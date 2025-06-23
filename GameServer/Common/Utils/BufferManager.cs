using System.Net.Sockets;
using System.Collections.Generic;

namespace Common.Utils
{
    public class BufferManager
    {
        private readonly byte[] _buffer;
        private readonly Stack<int> _freeIndexes = new();
        private int _current;
        private readonly int _blockSize;

        public BufferManager(int totalBytes, int blockSize)
        {
            _buffer = new byte[totalBytes];
            _blockSize = blockSize;
        }

        public void SetBuffer(SocketAsyncEventArgs args)
        {
            if (_freeIndexes.Count > 0)
            {
                args.SetBuffer(_buffer, _freeIndexes.Pop(), _blockSize);
            }
            else
            {
                args.SetBuffer(_buffer, _current, _blockSize);
                _current += _blockSize;
            }
        }

        public void FreeBuffer(SocketAsyncEventArgs args)
        {
            _freeIndexes.Push(args.Offset);
            args.SetBuffer(null, 0, 0);
        }
    }
}
