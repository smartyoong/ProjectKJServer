using System.Net.Sockets;

namespace LoginServer
{
    public enum LoginPacketListID
    {
        LOGIN_REQUEST = 0,
        LOGIN_RESPONESE = 1,
        REGIST_ACCOUNT_REQUEST = 2,
        REGIST_ACCOUNT_RESPONESE = 3
    }

    public enum LoginGamePacketListID
    {
        REQUEST_USER_INFO_SUMMARY = 0,
        RESPONSE_USER_INFO_SUMMARY = 1
    }

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
    public struct RegistAccountResponsePacket(bool IsSuccess, int ErrorCode)
    {
        public bool IsSuccess { get; set; } = IsSuccess;
        public int ErrorCode { get; set; } = ErrorCode;
    }

    /// <summary>
    /// ////////////////////////////////////////////////////////////////////////////////////////
    /// </summary>
    /// <param name="AccountID"></param>

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
