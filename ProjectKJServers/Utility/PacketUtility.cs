using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;

namespace PacketUtility
{
    public static class PacketUtils
    {
        public static int GetSizeFromPacket(byte[] Packet)
        {
            return BitConverter.ToInt32(Packet, 0);
        }

        public static byte[] AddPacketHeader(byte[] Original)
        {
            int PacketSize = Original.Length;
            byte[] PacketSizeBuffer = BitConverter.GetBytes(PacketSize);
            byte[] Packet = new byte[PacketSize + PacketSizeBuffer.Length];
            Buffer.BlockCopy(PacketSizeBuffer, 0, Packet, 0, PacketSizeBuffer.Length);
            Buffer.BlockCopy(Original, 0, Packet, PacketSizeBuffer.Length, PacketSize);
            return Packet;
        }

        public static T? DeserializePacket<T>(byte[] DataBuffer)
        {
            return JsonSerializer.Deserialize<T>(DataBuffer);
        }

        public static byte[] SerializePacket<T>(T Data)
        {
            return JsonSerializer.SerializeToUtf8Bytes(Data);
        }

        public static byte[] MakePacket<E,T>(E ID, T Packet)
        {
            if (!(ID is IConvertible))
            {
                throw new ArgumentException("ID 값은 반드시 int와 호환되어야합니다.");
            }
            var IDBuffer = BitConverter.GetBytes(Convert.ToInt32(ID));
            var PacketBuffer = SerializePacket(Packet);
            byte[] ReturnBuffer = new byte[IDBuffer.Length + PacketBuffer.Length];
            Buffer.BlockCopy(IDBuffer, 0, ReturnBuffer, 0, IDBuffer.Length);
            Buffer.BlockCopy(PacketBuffer, 0, ReturnBuffer, IDBuffer.Length, PacketBuffer.Length);
            return AddPacketHeader(ReturnBuffer);
        }

        public static (E ID, T Packet) UnPackPacket<E, T>(byte[] Packet)
        {
            int ID = BitConverter.ToInt32(Packet, 0);
            bool IsEnumDefined = Enum.IsDefined(typeof(E), ID);
            if (!IsEnumDefined)
            {
                throw new ArgumentException("ID 값이 Enum에 정의되어 있지 않습니다.");
            }
            var PacketBuffer = new byte[Packet.Length - sizeof(int)];
            Buffer.BlockCopy(Packet, sizeof(int), PacketBuffer, 0, PacketBuffer.Length);
            T Data;
            return ((E)Enum.ToObject(typeof(E),ID), Data);
        }
    }
}
