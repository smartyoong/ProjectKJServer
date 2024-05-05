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

    [Serializable]
    public struct LoginRequestPacket(string AccountID, string Password)
    {
        public readonly string AccountID { get; } = AccountID;
        public readonly string Password { get; } = Password;
    }

    [Serializable]
    public struct LoginResponsePacket(bool IsSuccess, int ErrorCode)
    {
        public readonly bool IsSuccess { get; } = IsSuccess;
        public readonly int ErrorCode { get; } = ErrorCode;
    }

    [Serializable]
    public struct RegistAccountRequestPacket(string AccountID, string Password)
    {
        public readonly string AccountID { get; } = AccountID;
        public readonly string Password { get; } = Password;
    }

    [Serializable]
    public struct RegistAccountResponsePacket(bool IsSuccess, int ErrorCode)
    {
        public readonly bool IsSuccess { get; } = IsSuccess;
        public readonly int ErrorCode { get; } = ErrorCode;
    }
}
