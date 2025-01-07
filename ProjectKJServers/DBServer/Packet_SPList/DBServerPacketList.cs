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
        RESPONSE_CREATE_CHARACTER = 6,
        REQUEST_UPDATE_HEALTH_POINT = 7,
        REQUEST_UPDATE_MAGIC_POINT = 8,
        REQUEST_UPDATE_LEVEL_EXP = 9,
        REQUEST_UPDATE_JOB_LEVEL = 10,
        REQUEST_UPDATE_JOB = 11
    }
    // 래핑 클래스들은 한번 생성되고 불변으로 매개변수 전달용으로만 사용할 것이기에 Record가 적합
    public record GameServerSendPipeLineWrapper<E>(E ID, dynamic Packet) where E : Enum
    {
        public E PacketID { get; set; } = ID;
        public dynamic Packet { get; set; } = Packet;
    }
    // AccountID는 반드시 필요함 안그러면 클라한테 응답 못보냄!
    [Serializable]
    public struct RequestDBCharBaseInfoPacket(string AccountID, string NickName)
    {
        public string AccountID { get; set; } = AccountID;
        public string NickName { get; set; } = NickName;
    }
    [Serializable]
    public struct ResponseDBCharBaseInfoPacket(string AccountID, int Gender, int PresetNumber, int Job, int JobLevel, int MapID, int X, int Y, int Level, int EXP, string NickName, int HP, int MP)
    {
        public string AccountID { get; set; } = AccountID;
        public int Gender { get; set; } = Gender;
        public int PresetNumber { get; set; } = PresetNumber;
        public int Job { get; set; } = Job;
        public int JobLevel { get; set; } = JobLevel;
        public int MapID { get; set; } = MapID;
        public int X { get; set; } = X;
        public int Y { get; set; } = Y;
        public int Level { get; set; } = Level;
        public int EXP { get; set; } = EXP;
        public string NickName { get; set; } = NickName;
        public int HP { get; set; } = HP;
        public int MP { get; set; } = MP;
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

    [Serializable]
    public struct RequestDBUpdateHealthPointPacket(string AccountID, int CurrentHP)
    {
        public string AccountID { get; set; } = AccountID;
        public int CurrentHP { get; set; } = CurrentHP;
    }

    [Serializable]
    public struct RequestDBUpdateMagicPointPacket(string AccountID, int CurrentMP)
    {
        public string AccountID { get; set; } = AccountID;
        public int CurrentMP { get; set; } = CurrentMP;
    }

    [Serializable]
    public struct RequestDBUpdateLevelExpPacket(string AccountID, int Level, int CurrentEXP)
    {
        public string AccountID { get; set; } = AccountID;
        public int Level { get; set; } = Level;
        public int CurrentEXP { get; set; } = CurrentEXP;
    }

    [Serializable]
    public struct RequestDBUpdateJobLevelPacket(string AccountID, int Level)
    {
        public string AccountID { get; set; } = AccountID;
        public int Level { get; set; } = Level;
    }

    [Serializable]
    public struct RequestDBUpdateJobPacket(string AccountID, int Job)
    {
        public string AccountID { get; set; } = AccountID;
        public int Job { get; set; } = Job;
    }

}
