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
}
