using System.Buffers.Binary;
using System.Globalization;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;

namespace KYCPacket
{
    public static class PacketUtils
    {
        public static int GetSizeFromPacket(Memory<byte> Packet)
        {
            // 사이즈를 가져오는건 Acceptor 코어 내에서만 사용할거다
            return BinaryPrimitives.ReadInt32LittleEndian(Packet.Span);
        }

        public static S? DeserializePacket<S>(ref Memory<byte> DataBuffer) where S : struct
        {
            return JsonSerializer.Deserialize<S>(DataBuffer.Span.ToArray(), new JsonSerializerOptions { WriteIndented = true });
        }

        public static Memory<byte> SerializePacket<S>(S Data) where S : struct
        {
            return JsonSerializer.SerializeToUtf8Bytes(Data, new JsonSerializerOptions { WriteIndented = true }).AsMemory();
        }

        public static Memory<byte> MakePacket<E,S>(E ID, S Packet) where S : struct where E : Enum, IConvertible
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            // 사이즈를 구한다.
            var MemoryPacket = SerializePacket(Packet);
            int PacketSize = MemoryPacket.Length;
            int TotalSize = (PacketSize + (sizeof(int) * 2));

            // 각 데이터를 직렬화한다.
            writer.Write(TotalSize);
            writer.Write(ID.ToInt32(CultureInfo.CurrentCulture));
            writer.Write(MemoryPacket.Span);

            return new Memory<byte>(ms.GetBuffer(), 0, (int)ms.Length);
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

        // 거의 쓸일이 있으려나
        public static (EResult, SResult) GetPacket<EResult,SResult>(ref Memory<byte> MemoryData) where EResult : Enum, IConvertible where SResult : struct
        {
            // ID를 가져온다.
            int IDValue = BitConverter.ToInt32(MemoryData.Span);
            // 검증한다.
            bool IsEnumDefined = Enum.IsDefined(typeof(EResult), IDValue);
            if (!IsEnumDefined)
            {
                throw new ArgumentException("ID 값이 Enum에 정의되어 있지 않습니다.");
            }
            // enum 제약 조건이 없기에 어쩔 수 없는 박싱, 언박싱
            EResult ID = (EResult)Enum.ToObject(typeof(EResult), IDValue);
            // 나머지 데이터만 남겨둔다
            MemoryData = MemoryData.Slice(sizeof(int));
            // 데이터를 가져온다.
            SResult? Result = DeserializePacket<SResult>(ref MemoryData);
            if (Result == null)
            {
                throw new ArgumentException("패킷 데이터를 가져오는데 실패했습니다.");
            }
            return (ID, Result.Value);
        }
    }
}
