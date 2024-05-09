using System.Net.Sockets;

namespace KYCInterface
{
    public interface IPacketProcess
    {
        protected virtual void PushToPipeLine(Memory<byte> Data, Socket Sock)
        {

        }
    }
}
