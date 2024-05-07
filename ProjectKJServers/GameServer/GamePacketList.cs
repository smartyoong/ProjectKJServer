using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    internal class GamePacketList
    {
        public enum GameLoginPacketListID
        {
            REQUEST_USER_INFO_SUMMARY = 0,
            RESPONSE_USER_INFO_SUMMARY = 1
        }

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
    }
}
