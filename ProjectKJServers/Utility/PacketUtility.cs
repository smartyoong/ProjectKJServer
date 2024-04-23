using System.Runtime.InteropServices;
using System.Text.Json;

namespace KYCPacket
{
    public static class PacketUtils
    {
        public static int GetSizeFromPacket(ref byte[] Packet)
        {
            // 사이즈를 가져오는건 Acceptor 코어 내에서만 사용할거다
            return BitConverter.ToInt32(Packet, 0);
        }

        public static S? DeserializePacket<S>(ref Memory<byte> DataBuffer) where S : struct
        {
            return JsonSerializer.Deserialize<S>(DataBuffer.Span.ToArray());
        }

        public static byte[] SerializePacket<S>(S Data) where S : struct
        {
            return JsonSerializer.SerializeToUtf8Bytes(Data);
        }

        public static int IDToInt<E>(E ID) where E : Enum
        {
            if (!(ID is IConvertible))
            {
                throw new ArgumentException("ID 값은 반드시 int와 호환되어야합니다.");
            }
            return Convert.ToInt32(ID);
        }

        public static byte[] MakePacketFromStruct<S>(int ID, S Packet) where S : struct
        {
            // 사이즈를 구한다.
            int PacketSize = Marshal.SizeOf(Packet);
            int TotalSize = (PacketSize + (sizeof(int) * 2));

            // 각 데이터를 직렬화한다.
            var SizeBuffer = BitConverter.GetBytes(TotalSize);
            var IDBuffer = BitConverter.GetBytes(ID);
            var PacketBuffer = SerializePacket(Packet);

            // 복사 비용을 줄이기 위해 Memory를 사용한다.
            Memory<byte> ReturnMemory = new Memory<byte>(new byte[TotalSize]);
            Span<byte> ReturnSpan = ReturnMemory.Span;


            // Span을 이용해 Memory에 직접 참조하여 데이터를 복사한다.
            // 만약 여기서 에러가 나온다면, 사이즈를 제대로 계산했는지 확인하길 바란다
            int CurrentSpanIndex = 0;
            for (int i = CurrentSpanIndex; i < SizeBuffer.Length; i++)
            {
                ReturnSpan[i] = SizeBuffer[i];
            }

            CurrentSpanIndex += SizeBuffer.Length;
            for (int i = CurrentSpanIndex; i < IDBuffer.Length; i++)
            {
                ReturnSpan[i] = IDBuffer[i];
            }

            CurrentSpanIndex += IDBuffer.Length;
            for (int i = CurrentSpanIndex; i < PacketBuffer.Length; i++)
            {
                ReturnSpan[i] = PacketBuffer[i];
            }
            return ReturnSpan.ToArray();
        }

        public static Memory<byte> ByteToMemory(ref byte[] Packet)
        {
            return Packet.AsMemory();
        }

        public static EResult GetIDFromPacket<EResult>(ref Memory<byte> Packet) where EResult : Enum
        {
            // ID를 가져온다.
            int ID = BitConverter.ToInt32(Packet.Span);
            // 검증한다.
            bool IsEnumDefined = Enum.IsDefined(typeof(EResult), ID);
            if (!IsEnumDefined)
            {
                throw new ArgumentException("ID 값이 Enum에 정의되어 있지 않습니다.");
            }
            // 나머지 데이터만 남겨둔다
            Packet = Packet.Slice(sizeof(int));
            return (EResult)Enum.ToObject(typeof(EResult), ID);
        }

        public static SResult? GetPacketStruct<SResult>(ref Memory<byte> Packet) where SResult : struct
        {
            return DeserializePacket<SResult>(ref Packet);
        }
    }
}
