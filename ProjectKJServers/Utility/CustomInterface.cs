
namespace KYCInterface
{
    public interface IPacketProcess
    {
        public void GetRecvPacket();

        public void PushToSendQueue();

        public void PopFromSendQueue();

        public void ProcessPacket();
    }

    public interface IPacketProcessor<E> where E : Enum
    {
        public dynamic MakePacketStruct(E ID, params dynamic[] PacketParams);

        public void PushToPacketPipeline(byte[] Packet);
    }
}
