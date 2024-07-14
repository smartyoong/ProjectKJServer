using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBServer.Packet_SPList
{
    enum DBPacketListID
    {
        REQUEST_DB_TEST = 0,
        RESPONSE_DB_TEST = 1,
        REQUEST_CHAR_BASE_INFO = 2,
        RESPONSE_CHAR_BASE_INFO = 3,
        RESPONSE_NEED_TO_MAKE_CHARACTER = 4,
        REQUEST_CREATE_CHARACTER = 5,
        RESPONSE_CREATE_CHARACTER = 6
    }
    // 래핑 클래스들은 한번 생성되고 불변으로 매개변수 전달용으로만 사용할 것이기에 Record가 적합
    public record GameServerSendPipeLineWrapper<E>(E ID, dynamic Packet) where E : Enum
    {
        public E PacketID { get; set; } = ID;
        public dynamic Packet { get; set; } = Packet;
    }
    // AccountID는 반드시 필요함 안그러면 클라한테 응답 못보냄!
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
    public struct ResponseDBCharBaseInfoPacket(string AccountID, int Gender, int PresetNumber, int Job, int JobLevel, int MapID, int X, int Y, int Level, int EXP)
    {
        string AccountID { get; set; } = AccountID;
        int Gender { get; set; } = Gender;
        int PresetNumber { get; set; } = PresetNumber;
        int Job { get; set; } = Job;
        int JobLevel { get; set; } = JobLevel;
        int MapID { get; set; } = MapID;
        int X { get; set; } = X;
        int Y { get; set; } = Y;
        int Level { get; set; } = Level;
        int EXP { get; set; } = EXP;

    }
    [Serializable]
    public struct ResponseDBNeedToMakeCharacterPacket(string AccountID)
    {
        public string AccountID { get; set; } = AccountID;
    }

    [Serializable]
    public struct RequestDBCreateCharacterPacket(string AccountID, int Gender, int PresetID)
    {
        public string AccountID { get; set; } = AccountID;
        public int Gender { get; set; } = Gender;
        public int PresetID { get; set; } = PresetID;
    }

    [Serializable]
    public struct ResponseDBCreateCharacterPacket(string AccountID, int ErrorCode)
    {
        public string AccountID { get; set; } = AccountID;
        public int ErrorCode { get; set; } = ErrorCode;
    }

}
