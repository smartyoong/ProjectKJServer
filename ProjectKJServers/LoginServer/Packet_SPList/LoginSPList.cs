using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoginServer.Packet_SPList
{
    enum LOGIN_SP
    {
        SP_INVALID = 0,
        SP_ID_UNIQUE_CHECK = 1,
        SP_LOGIN = 2,
        SP_REGIST_ACCOUNT = 3,
        SP_CREATE_NICKNAME = 4
    }

    public interface ISQLPacket
    {
        int ClientID { get; set; }
    }
    public record SQLLoginRequest(string AccountID, string Password, int ClientID) : ISQLPacket
    {
        public string AccountID { get; set; } = AccountID;
        public string Password { get; set; } = Password;
        public int ClientID { get; set; } = ClientID;
    }

    public class SQLIDUniqueCheckRequest(string AccountID, int ClientID) : ISQLPacket
    {
        public string AccountID { get; set; } = AccountID;
        public int ClientID { get; set; } = ClientID;
    }

    public class SQLRegistAccountRequest(string AccountID, string Password, string IPAddr, int ClientID) : ISQLPacket
    {
        public string AccountID { get; set; } = AccountID;
        public string Password { get; set; } = Password;
        public string IPAddr { get; set; } = IPAddr;
        public int ClientID { get; set; } = ClientID;
    }

    public class SQLCreateNickNameRequest(string AccountID, string NickName, int ClientID) : ISQLPacket
    {
        public string AccountID { get; set; } = AccountID;
        public string NickName { get; set; } = NickName;
        public int ClientID { get; set; } = ClientID;
    }
}
