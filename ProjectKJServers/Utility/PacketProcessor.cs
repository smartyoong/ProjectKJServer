using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace PacketProcessor
{
    public class PacketProcessor</*Enum 타입*/E,/*Enum을 매개변수로 받아서 record를 리턴하는 functor*/T>
    {
        private Channel<byte[]> PacketChannel;

    }
}
