﻿namespace GameServer
{
    public enum GameLoginPacketListID
    {
        REQUEST_USER_INFO_SUMMARY = 0,
        RESPONSE_USER_INFO_SUMMARY = 1
    }

    public enum GameDBPacketListID
    {
        REQUEST_DB_TEST = 0,
        RESPONSE_DB_TEST = 1
    }

    public enum GamePacketListID
    {
        REQUEST_GAME_TEST = 0,
        RESPONSE_GAME_TEST = 1
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
    public struct RequestUserInfoSummaryPacket(string AccountID, string NickName)
    {
        public string AccountID { get; set; } = AccountID;
        public string NickName { get; set; } = NickName;
    }

    [Serializable]
    public struct ResponseUserInfoSummaryPacket(string AccountID, string NickName, int Level, int Exp)
    {
        public string AccountID { get; set; } = AccountID;
        public string NickName { get; set; } = NickName;
        public int Level { get; set; } = Level;
        public int Exp { get; set; } = Exp;
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

    /// 클라이언트
    /// 
    [Serializable]
    public struct RequestGameTestPacket(string AccountID, string NickName)
    {
        public string AccountID { get; set; } = AccountID;
        public string NickName { get; set; } = NickName;
    }

    [Serializable]
    public struct ResponseGameTestPacket(string AccountID, string NickName, int Level, bool Exp)
    {
        public string AccountID { get; set; } = AccountID;
        public string NickName { get; set; } = NickName;
        public int Level { get; set; } = Level;
        public bool Exp { get; set; } = Exp;
    }
}