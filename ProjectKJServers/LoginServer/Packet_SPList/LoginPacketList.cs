﻿using System.Net.Sockets;

namespace LoginServer.Packet_SPList
{
    interface ClientRecvPacket
    {
    }

    interface ClientSendPacket
    {
    }

    interface GameSendPacket
    {
    }

    interface GameRecvPacket
    {
    }

    public enum LoginPacketListID
    {
        LOGIN_REQUEST = 0,
        LOGIN_RESPONESE = 1,
        REGIST_ACCOUNT_REQUEST = 2,
        REGIST_ACCOUNT_RESPONESE = 3,
        ID_UNIQUE_CHECK_REQUEST = 4,
        ID_UNIQUE_CHECK_RESPONESE = 5,
        CREATE_NICKNAME_REQUEST = 6,
        CREATE_NICKNAME_RESPONESE = 7
    }

    public enum LoginGamePacketListID
    {
        SEND_USER_HASH_INFO = 1,
        RESPONSE_USER_HASH_INFO = 2,
        REQUEST_KICK_USER = 3
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

    // 패킷은 데이터가 크지않고 내부 값이 변경될 수 있기 때문에 Struct가 적합
    [Serializable]
    public struct LoginRequestPacket(string AccountID, string Password) : ClientRecvPacket
    {
        public string AccountID { get; set; } = AccountID;
        public string Password { get; set; } = Password;
    }

    [Serializable]
    public struct LoginResponsePacket(string NickName, string HashCode, int ErrorCode) : ClientSendPacket
    {
        public string NickName { get; set; } = NickName;
        public string HashValue { get; set; } = HashCode;
        public int ErrorCode { get; set; } = ErrorCode;
    }

    [Serializable]
    public struct RegistAccountRequestPacket(string AccountID, string Password) : ClientRecvPacket
    {
        public string AccountID { get; set; } = AccountID;
        public string Password { get; set; } = Password;
    }

    [Serializable]
    public struct RegistAccountResponsePacket(int ErrorCode) : ClientSendPacket
    {
        public int ErrorCode { get; set; } = ErrorCode;
    }

    [Serializable]
    public struct IDUniqueCheckRequestPacket(string AccountID) : ClientRecvPacket
    {
        public string AccountID { get; set; } = AccountID;
    }

    [Serializable]
    public struct IDUniqueCheckResponsePacket(bool IsSuccess) : ClientSendPacket
    {
        public bool IsUnique { get; set; } = IsSuccess;
    }
    //한글을 지원하기 위한 특수화
    [Serializable]
    public struct CreateNickNameRequestPacket(string AccountID, string NickName) : ClientRecvPacket
    {
        public string AccountID { get; set; } = AccountID;
        public string NickName { get; set; } = NickName; // Base64 인코딩된 문자열
    }
    // Encoding.UTF8.GetString(packet.NickName) 이거 사용해서 byte[]를 string으로 변환
    //Encoding.UTF8.GetBytes((string)Parameters[1].Value) 이거 사용해서 string을 byte[]로 변환
    [Serializable]
    public struct CreateNickNameResponsePacket(string NickName, int ErrorCode) : ClientSendPacket
    {
        public string NickName { get; set; } = NickName;
        public int ErrorCode { get; set; } = ErrorCode;
    }


    /// <summary>
    /// ////////////////////////////////////////////////////////////////////////////////////////
    /// </summary>
    /// <param name="AccountID"></param>

    [Serializable]
    public struct SendUserHashInfoPacket(string AccountID, string HashValue, int ClientID, string IPAddr) : GameSendPacket
    {
        public string AccountID { get; set; } = AccountID;
        public string HashCode { get; set; } = HashValue;
        public int ClientLoginID { get; set; } = ClientID;
        public string IPAddr { get; set; } = IPAddr;
        public int TimeToLive = 0;
    }

    [Serializable]
    public struct ResponseUserHashInfoPacket(int ClientID, string NickName, int ErrCode, int TTL) : GameRecvPacket
    {
        public int ClientLoginID { get; set; } = ClientID;
        public string NickName { get; set; } = NickName;
        public int ErrorCode { get; set; } = ErrCode;
        public int TimeToLive { get; set; } = TTL;
    }

    [Serializable]
    public struct RequestKickUserPacket(string IPAddr, string AccountID) : GameRecvPacket
    {
        public string IPAddr { get; set; } = IPAddr;
        public string AccountID { get; set; } = AccountID;
    }
}
