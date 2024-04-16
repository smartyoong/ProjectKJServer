using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utility
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

        public void ProcessPacket(object Packet);
    }
}
