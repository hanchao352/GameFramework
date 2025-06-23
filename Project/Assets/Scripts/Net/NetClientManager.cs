using Google.Protobuf;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class NetClientManager : SingletonManager<NetClientManager>, IGeneric
{
    private const string _ip = "127.0.0.1";
    private const int _port = 5000;
    private const int InitialReceiveBufferSize = 8192;
    private const int MaxReceiveBufferSize = 1024 * 1024;
    private const int BackpressureThreshold = 100;

    private Socket _socket;
    private readonly ConcurrentQueue<(IMessage message, int protoId)> _sendQueue = new();
    private bool _sending;
    private int _receiveBacklog;
    private bool _backpressureApplied;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private CancellationTokenSource _cts;

    // 接收缓冲区管理
    private byte[] _receiveBuffer;
    private int _receiveDataLength;

    // SocketAsyncEventArgs
    private SocketAsyncEventArgs _receiveArgs;
    private readonly ConcurrentStack<SocketAsyncEventArgs> _sendArgsPool = new();

    public override void Initialize()
    {
        base.Initialize();
        // 不在这里租 buffer / 创建 SAEA，改到 ConnectAsync 里保证重连时也能重新创建
    }

    private void SetupReceiveArgs()
    {
        _receiveArgs = new SocketAsyncEventArgs();
        _receiveArgs.Completed += OnSocketOperationCompleted;
        _receiveArgs.SetBuffer(new byte[4096], 0, 4096);
    }

    public async void ConnectAsync()
    {
        // 每次连接前，先清理旧状态
        Cleanup();

        // 重建接收缓冲区和 SocketAsyncEventArgs
        _receiveBuffer = ArrayPool<byte>.Shared.Rent(InitialReceiveBufferSize);
        _receiveDataLength = 0;
        SetupReceiveArgs();

        try
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await _socket.ConnectAsync(_ip, _port);

            Debug.Log("Connected to server.");
            _cts = new CancellationTokenSource();

            StartReceiving();
            StartSending();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error connecting: {e.Message}");
            await Task.Delay(1000);
            Reconnect();
        }
    }

    private void StartReceiving()
    {
        try
        {
            if (!_socket.ReceiveAsync(_receiveArgs))
            {
                ProcessReceive(_receiveArgs);
            }
        }
        catch (ObjectDisposedException)
        {
            Debug.Log("Socket disposed during receive start");
        }
    }

    private void OnSocketOperationCompleted(object sender, SocketAsyncEventArgs e)
    {
        switch (e.LastOperation)
        {
            case SocketAsyncOperation.Receive:
                ProcessReceive(e);
                break;
            case SocketAsyncOperation.Send:
                ProcessSend(e);
                break;
        }
    }

    private void ProcessReceive(SocketAsyncEventArgs e)
    {
        if (e.BytesTransferred == 0 || e.SocketError != SocketError.Success)
        {
            HandleDisconnection();
            return;
        }

        EnsureReceiveCapacity(e.BytesTransferred);

        Buffer.BlockCopy(e.Buffer, e.Offset, _receiveBuffer, _receiveDataLength, e.BytesTransferred);
        _receiveDataLength += e.BytesTransferred;

        ProcessReceivedData();
        CheckBackpressure();

        if (!_backpressureApplied)
        {
            StartReceiving();
        }
    }

    private void EnsureReceiveCapacity(int newDataSize)
    {
        int required = _receiveDataLength + newDataSize;
        if (required > _receiveBuffer.Length)
        {
            if (required > MaxReceiveBufferSize)
            {
                Debug.LogError($"Receive buffer overflow ({required} > {MaxReceiveBufferSize})");
                HandleDisconnection();
                return;
            }

            int newSize = Math.Min(Math.Max(_receiveBuffer.Length * 2, required), MaxReceiveBufferSize);
            var newBuf = ArrayPool<byte>.Shared.Rent(newSize);
            Buffer.BlockCopy(_receiveBuffer, 0, newBuf, 0, _receiveDataLength);
            ArrayPool<byte>.Shared.Return(_receiveBuffer);
            _receiveBuffer = newBuf;
        }
    }

    private void ProcessReceivedData()
    {
        int offset = 0;
        while (offset + 8 <= _receiveDataLength)
        {
            int packageLength = BitConverter.ToInt32(_receiveBuffer, offset);
            int protoId = BitConverter.ToInt32(_receiveBuffer, offset + 4);

            if (packageLength < 8 || packageLength > MaxReceiveBufferSize)
            {
                Debug.LogError($"Invalid package length: {packageLength}");
                HandleDisconnection();
                return;
            }
            if (offset + packageLength > _receiveDataLength)
                break;

            Interlocked.Increment(ref _receiveBacklog);
            ProcessMessage(protoId, new ArraySegment<byte>(_receiveBuffer, offset + 8, packageLength - 8));
            offset += packageLength;
        }

        if (offset > 0)
        {
            int remaining = _receiveDataLength - offset;
            if (remaining > 0)
                Buffer.BlockCopy(_receiveBuffer, offset, _receiveBuffer, 0, remaining);
            _receiveDataLength = remaining;
        }
    }

    private async void ProcessMessage(int protoId, ArraySegment<byte> data)
    {
        try
        {
            Type msgType = ProtoManager.Instance.GetTypeByProtoId(protoId);
            IMessage message = (IMessage)ObjectCreator.CreateInstance(msgType);
            using (var cis = new CodedInputStream(data.Array, data.Offset, data.Count))
            {
                message.MergeFrom(cis);
            }
            await NetMessageHandleManager.Instance.InvokeCallback(protoId, message);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error processing message {protoId}: {ex.Message}");
        }
        finally
        {
            Interlocked.Decrement(ref _receiveBacklog);
            CheckBackpressure();
        }
    }

    private void CheckBackpressure()
    {
        lock (this)
        {
            bool apply = _receiveBacklog > BackpressureThreshold;
            bool release = _receiveBacklog <= BackpressureThreshold / 2;

            if (apply && !_backpressureApplied)
            {
                Debug.LogWarning($"Applying backpressure. Backlog: {_receiveBacklog}");
                _backpressureApplied = true;
            }
            else if (release && _backpressureApplied)
            {
                Debug.Log("Releasing backpressure");
                _backpressureApplied = false;
                StartReceiving();
            }
        }
    }

    public void SendMessageAsync(IMessage message, int protoId)
    {
        if (!IsConnected())
        {
            Debug.LogWarning("Attempted to send message while disconnected");
            return;
        }

        _sendQueue.Enqueue((message, protoId));
        if (!_sending)
        {
            StartSending();
        }
    }

    private async void StartSending()
    {
        if (_sending) return;
        _sending = true;

        try
        {
            while (_sendQueue.TryDequeue(out var item) && IsConnected())
            {
                var (message, protoId) = item;
                int headerSize = 8;
                int msgSize = message.CalculateSize();
                int total = headerSize + msgSize;
                byte[] buf = ArrayPool<byte>.Shared.Rent(total);

                try
                {
                    MemoryMarshal.Write(buf.AsSpan(0, 4), ref total);
                    MemoryMarshal.Write(buf.AsSpan(4, 4), ref protoId);

                    using (var ms = new MemoryStream(buf, 8, msgSize))
                    using (var cos = new CodedOutputStream(ms))
                    {
                        message.WriteTo(cos);
                    }

                    var args = GetSendArgs();
                    args.SetBuffer(buf, 0, total);
                    if (!_socket.SendAsync(args))
                        ProcessSend(args);

                    if (_sendQueue.Count > 50)
                        await Task.Yield();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Send error: {ex.Message}");
                    ArrayPool<byte>.Shared.Return(buf);
                }
            }
        }
        finally
        {
            _sending = false;
        }
    }

    private SocketAsyncEventArgs GetSendArgs()
    {
        if (_sendArgsPool.TryPop(out var args))
            return args;

        args = new SocketAsyncEventArgs();
        args.Completed += OnSocketOperationCompleted;
        return args;
    }

    private void ProcessSend(SocketAsyncEventArgs e)
    {
        if (e.SocketError != SocketError.Success)
            Debug.LogError($"Send failed: {e.SocketError}");

        if (e.Buffer != null)
            ArrayPool<byte>.Shared.Return(e.Buffer);
        _sendArgsPool.Push(e);

        if (_sendQueue.Count > 0)
            StartSending();
    }

    private void HandleDisconnection()
    {
        Debug.Log("Connection closed");
        Cleanup();
        Reconnect();
    }

    private async void Reconnect()
    {
        if (_cts == null || _cts.IsCancellationRequested)
            return;

        Debug.Log("Attempting reconnect in 3 seconds...");
        try
        {
            await Task.Delay(3000, _cts.Token);
            if (!_cts.IsCancellationRequested)
                ConnectAsync();
        }
        catch (TaskCanceledException) { /* ignore */ }
    }

    private void Cleanup()
    {
        try
        {
            if (_socket != null)
            {
                try
                {
                    if (_socket.Connected)
                        _socket.Shutdown(SocketShutdown.Both);
                }
                catch (Exception ex)
                {
                    Debug.Log($"[Cleanup] Socket shutdown safe-guard: {ex.Message}");
                }
                finally
                {
                    _socket.Close();
                }
                _socket = null;
            }

            if (_receiveBuffer != null)
            {
                ArrayPool<byte>.Shared.Return(_receiveBuffer);
                _receiveBuffer = null;
            }

            _receiveArgs?.Dispose();
            _receiveArgs = null;

            while (_sendArgsPool.TryPop(out var args))
                args.Dispose();

            _cts?.Cancel();
            _cts = null;

            _sendQueue.Clear();
            _receiveDataLength = 0;
            _receiveBacklog = 0;
        }
        catch (Exception e)
        {
            Debug.LogError($"Cleanup error: {e}");
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        Cleanup();
    }

    private void OnDisable()
    {
        // 停止播放/销毁时取消重连
        _cts?.Cancel();
    }

    public bool IsConnected() =>
        _socket != null && _socket.Connected;
}
