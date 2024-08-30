using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBServer.Packet_SPList
{
    enum DB_SP
    {
        SP_INVALID = 0,
        SP_TEST = 1,
        SP_READ_CHARACTER = 2,
        SP_CREATE_CHARACTER = 3
    }

    public interface IGameSQLPacket
    {
        public string AccountID { get; set; }
    }

    public record GameSQLReadCharacterPacket(string AccountID, string NickName) : IGameSQLPacket
    {
        public string AccountID { get; set; } = AccountID;
        public string NickName { get; set; } = NickName;
    }

    public record GameSQLCreateCharacterPacket(string AccountID, int Gender, int PresetID) : IGameSQLPacket
    {
        public string AccountID { get; set; } = AccountID;
        public int Gender { get; set; } = Gender;
        public int PresetID { get; set; } = PresetID;
    }
}
