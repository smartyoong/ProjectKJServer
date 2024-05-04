using System.Net.Sockets;

namespace KYCInterface
{
    public interface IPacketProcess
    {
        public void GetRecvPacket();

        public void PushToSendQueue();

        public void PopFromSendQueue();

        public void ProcessPacket();
    }
}
