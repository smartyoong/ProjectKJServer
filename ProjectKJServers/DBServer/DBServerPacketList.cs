using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBServer
{
    enum DBPacketListID
    {
        REQUEST_DB_TEST = 0,
        RESPONSE_DB_TEST = 1,
        REQUEST_CHAR_BASE_INFO = 2,
        RESPONSE_CHAR_BASE_INFO = 3,
        RESPONSE_NEED_TO_MAKE_CHARACTER = 4
    }
    // 래핑 클래스들은 한번 생성되고 불변으로 매개변수 전달용으로만 사용할 것이기에 Record가 적합
    public record GameServerSendPipeLineWrapper<E>(E ID, dynamic Packet) where E : Enum
    {
        public E PacketID { get; set; } = ID;
        public dynamic Packet { get; set; } = Packet;
    }

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

}
