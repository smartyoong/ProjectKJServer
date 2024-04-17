using Utility;

namespace DBServer
{
    public enum LoginPacketList
    {
        LoginRequest = 0,
        LoginResponse = 1,
        RegistAccountRequest = 2,
        RegistAccountResponse = 3
    }

    public struct ErrorPacket(GeneralErrorCode ErrorCode)
    {
        public GeneralErrorCode ErrorCode { get; } = ErrorCode;
    }

    [Serializable]
    public struct LoginRequestPacket(string AccountID, string Password)
    {
        public string AccountID { get; } = AccountID;
        public string Password { get; } = Password;
    }

    [Serializable]
    public struct RegistAccountRequestPacket(string AccountID, string Password)
    {
        public string AccountID { get; } = AccountID;
        public string Password { get; } = Password;
    }

    [Serializable]
    public struct RegistAccountResponsePacket(bool IsSuccess, int ErrorCode)
    {
        public bool IsSuccess { get; } = IsSuccess;
        public int ErrorCode { get; } = ErrorCode;
    }

    [Serializable]
    public struct LoginResponsePacket(bool IsSuccess, int ErrorCode)
    {
        public bool IsSuccess { get; } = IsSuccess;
        public int ErrorCode { get; } = ErrorCode;
    }
}
