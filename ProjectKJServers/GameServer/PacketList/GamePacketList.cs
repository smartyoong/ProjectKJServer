namespace GameServer.PacketList
{
    public enum GameLoginPacketListID
    {
        SEND_USER_HASH_INFO = 1,
        RESPONSE_USER_HASH_INFO = 2,
        REQUEST_KICK_USER = 3
    }

    public enum GameDBPacketListID
    {
        REQUEST_DB_TEST = 0,
        RESPONSE_DB_TEST = 1,
        REQUEST_CHAR_BASE_INFO = 2,
        RESPONSE_CHAR_BASE_INFO = 3,
        RESPONSE_NEED_TO_MAKE_CHARACTER = 4
    }

    public enum GamePacketListID
    {
        REQUEST_GAME_TEST = 0,
        RESPONSE_GAME_TEST = 1,
        REQUEST_HASH_AUTH_CHECK = 2,
        RESPONSE_HASH_AUTH_CHECK = 3,
        KICK_CLIENT = 4,
        REQUEST_CHAR_BASE_INFO = 5,
        RESPONSE_NEED_TO_MAKE_CHARACTER = 6,
        RESPONSE_CHAR_BASE_INFO = 7,
        REQUEST_CREATE_CHARACTER = 8
    }

    // 래핑 클래스들은 한번 생성되고 불변으로 매개변수 전달용으로만 사용할 것이기에 Record가 적합
    public record DBServerSendPipeLineWrapper<E>(E ID, dynamic Packet) where E : Enum
    {
        public E PacketID { get; set; } = ID;
        public dynamic Packet { get; set; } = Packet;
    }
    public record LoginServerSendPipeLineWrapper<E>(E ID, dynamic Packet) where E : Enum
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

    public record ClientSendPacketPipeLineWrapper<E>(E ID, dynamic Packet, int ClientID) where E : Enum
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

    /// <summary>
    /// 로그인 서버
    /// </summary>
    /// <param name="AccountID"></param>
    /// <param name="NickName"></param>

    [Serializable]
    public struct ResponseLoginTestPacket(string AccountID, string NickName, int Level, int Exp)
    {
        public string AccountID { get; set; } = AccountID;
        public string NickName { get; set; } = NickName;
        public int Level { get; set; } = Level;
        public int Exp { get; set; } = Exp;
    }

    [Serializable]
    public struct SendUserHashInfoPacket(string Account, string HashValue, int ClientID, string IPAddr)
    {
        public string AccountID { get; set; } = Account;
        public string HashCode { get; set; } = HashValue;
        public int ClientLoginID { get; set; } = ClientID;
        public string IPAddr { get; set; } = IPAddr;

        public int TimeToLive = 0;
    }

    [Serializable]
    public struct ResponseUserHashInfoPacket(int ClientID, string NickName, int ErrCode, int TTL)
    {
        public int ClientLoginID { get; set; } = ClientID;
        public string NickName { get; set; } = NickName;
        public int ErrorCode { get; set; } = ErrCode;
        public int TimeToLive { get; set; } = TTL;
    }
    [Serializable]
    public struct RequestKickUserPacket(string IPAddr, string AccountID)
    {
        public string IPAddr { get; set; } = IPAddr;
        public string AccountID { get; set; } = AccountID;
    }

    /// 디비 서버
    /// 
    [Serializable]
    public struct RequestDBTestPacket(string AccountID, string NickName)
    {
        public string AccountID { get; set; } = AccountID;
        public string NickName { get; set; } = NickName;
    }

    [Serializable]
    public struct ResponseDBTestPacket(string AccountID, string NickName, int Level, int Exp)
    {
        public string AccountID { get; set; } = AccountID;
        public string NickName { get; set; } = NickName;
        public int Level { get; set; } = Level;
        public int Exp { get; set; } = Exp;
    }

    [Serializable]
    public struct RequestDBCharBaseInfoPacket(string AccountID)
    {
        public string AccountID { get; set; } = AccountID;
    }
    [Serializable]
    public struct ResponseDBNeedToMakeCharacterPacket(string AccountID)
    {
        public string AccountID { get; set; } = AccountID;
    }

    /// 클라이언트
    /// 
    [Serializable]
    public struct RequestHashAuthCheckPacket(string AccountID, string HashCode)
    {
        public string AccountID { get; set; } = AccountID;
        public string HashCode { get; set; } = HashCode;
    }

    [Serializable]
    public struct ResponseHashAuthCheckPacket(string AccountID, int ErrorCode)
    {
        public string AccountID { get; set; } = AccountID;
        public int ErrorCode { get; set; } = ErrorCode;
    }

    [Serializable]
    public struct SendKickClientPacket(int Reason)
    {
        public int Reason { get; set; } = Reason;
    }

    [Serializable]
    public struct RequestCharBaseInfoPacket(string AccountID, string HashCode)
    {
        public string AccountID { get; set; } = AccountID;
        public string HashCode { get; set; } = HashCode;
    }

    [Serializable]
    public struct ResponseNeedToMakeCharcterPacket(int ErrorCode)
    {
        public int ErrorCode { get; set; } = ErrorCode;
    }

    [Serializable]
    public struct ResponseCharBaseInfoPacket(int Level, int Exp, int Job, int JobLevel, int LastMapID, int LastMapX, int LastMapY)
    {
        public int Level { get; set; } = Level;
        public int Exp { get; set; } = Exp;
        public int Job { get; set; } = Job;
        public int JobLevel { get; set; } = JobLevel;
        public int LastMapID { get; set; } = LastMapID;
        public int LastMapX { get; set; } = LastMapX;
        public int LastMapY { get; set; } = LastMapY;

    }

    [Serializable]
    public struct RequestCreateCharacter(string AccountID, string HashCode, string NickName, int Gender, int PresetID)
    {
        public string AccountID { get; set; } = AccountID;
        public string HashCode { get; set; } = HashCode;
        public string NickName { get; set; } = NickName;
        public int Gender { get; set; } = Gender;
        public int PresetID { get; set; } = PresetID;
    }
}
