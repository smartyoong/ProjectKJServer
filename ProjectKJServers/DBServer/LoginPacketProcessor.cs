using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using Utility;

namespace DBServer
{
    internal class LoginPacketProcessor : IPacketProcessor<LoginPacketList>
    {
        private Channel<byte[]> PacketChannel = Channel.CreateUnbounded<byte[]>();

        LoginPacketProcessor()
        {
            Task.Run(() => ProcessPacket(1));
        }
        public dynamic MakePacketStruct(LoginPacketList ID, params dynamic[] PacketParams)
        {
            return 1;
        }

        public void ProcessPacket(object Packet)
        {

        }
    }
}
