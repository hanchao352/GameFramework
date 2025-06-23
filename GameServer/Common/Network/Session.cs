using System.Net.Sockets;
using System.Threading.Tasks;

namespace Common.Network
{
    public class Session
    {
        public Socket Socket { get; }
        public int ServerId { get; set; }
        public byte[] ReceiveBuffer { get; } = new byte[8192];
        public int ReceiveOffset { get; set; }

        public Session(Socket socket) { Socket = socket; }

        public ValueTask<int> SendAsync(ReadOnlyMemory<byte> data)
            => Socket.SendAsync(data, SocketFlags.None);
    }
}
