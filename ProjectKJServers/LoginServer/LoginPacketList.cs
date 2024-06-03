using System.Net.Sockets;

namespace LoginServer
{
    public enum LoginPacketListID
    {
        LOGIN_REQUEST = 0,
        LOGIN_RESPONESE = 1,
        REGIST_ACCOUNT_REQUEST = 2,
        REGIST_ACCOUNT_RESPONESE = 3,
        ID_UNIQUE_CHECK_REQUEST = 4,
        ID_UNIQUE_CHECK_RESPONESE = 5
    }

    public enum LoginGamePacketListID
    {
        REQUEST_LOGIN_TEST = 0,
        RESPONSE_LOGIN_TEST = 1
    }

    // 래핑 클래스들은 한번 생성되고 불변으로 매개변수 전달용으로만 사용할 것이기에 Record가 적합
    public record GameServerSendPipeLineWrapper<E>(E ID, dynamic Packet) where E : Enum
    {
        public E PacketID { get; set; } = ID;
        public dynamic Packet { get; set; } = Packet;
    }

    public record ClientRecvMemoryPipeLineWrapper(Memory<byte> Data, int ClientID)
    { 
        public Memory<byte> MemoryData { get; set; } = Data;
        public int ClientID { get; set; } = ClientID;
    }

    public record ClientRecvPacketPipeLineWrapper(dynamic Packet, int ClientID)
    {
        public dynamic Packet { get; set; } = Packet;
        public int ClientID { get; set; } = ClientID;
    }

    public record ClientSendPacketPipeLineWrapper<E>(E ID,dynamic Packet, int ClientID) where E : Enum
    {
        public E PacketID { get; set; } = ID;
        public dynamic Packet { get; set; } = Packet;
        public int ClientID { get; set; } = ClientID;
    }

    public record ClientSendMemoryPipeLineWrapper(Memory<byte> Data, int ClientID)
    {
        public Memory<byte> MemoryData { get; set; } = Data;
        public int ClientID { get; set; } = ClientID;
    }

    // 패킷은 데이터가 크지않고 내부 값이 변경될 수 있기 때문에 Struct가 적합
    [Serializable]
    public struct LoginRequestPacket(string AccountID, string Password)
    {
        public string AccountID { get; set; } = AccountID;
        public string Password { get; set; } = Password;
    }

    [Serializable]
    public struct LoginResponsePacket(string NickName, int ErrorCode)
    {
        public string NickName { get; set; } = NickName;
        public int ErrorCode { get; set; } = ErrorCode;
    }

    [Serializable]
    public struct RegistAccountRequestPacket(string AccountID, string Password)
    {
        public string AccountID { get; set; } = AccountID;
        public string Password { get; set; } = Password;
    }

    [Serializable]
    public struct RegistAccountResponsePacket(int ErrorCode)
    {
        public int ErrorCode { get; set; } = ErrorCode;
    }

    [Serializable]
    public struct IDUniqueCheckRequestPacket(string AccountID)
    {
        public string AccountID { get; set; } = AccountID;
    }

    [Serializable]
    public struct IDUniqueCheckResponsePacket(bool IsSuccess)
    {
        public bool IsUnique { get; set; } = IsSuccess;
    }

    /// <summary>
    /// ////////////////////////////////////////////////////////////////////////////////////////
    /// </summary>
    /// <param name="AccountID"></param>

    [Serializable]
    public struct RequestGameTestPacket(string AccountID, string NickName)
    {
        public string AccountID { get; set; } = AccountID;
        public string NickName { get; set; } = NickName;
    }

    [Serializable]
    public struct ResponseGameTestPacket(string AccountID, string NickName, int Level, int Exp)
    {
        public string AccountID { get; set; } = AccountID;
        public string NickName { get; set; } = NickName;
        public int Level { get; set; } = Level;
        public int Exp { get; set; } = Exp;
    }
}
