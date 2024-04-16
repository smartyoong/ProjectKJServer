using Utility;

namespace DBServer
{
    public enum LoginPacketList
    {
        LoginRequest = 0,
        RegistAccountRequest = 1,
        RegistAccountResponse = 2,
        LoginResponse = 3
    }

    [Serializable]
    public struct LoginRequestPacket(string AccountID, string Password);

    [Serializable]
    public struct RegistAccountRequestPacket(string AccountID, string Password);

    [Serializable]
    public struct RegistAccountResponsePacket(bool IsSuccess, int ErrorCode);

    [Serializable]
    public struct LoginResponsePacket(bool IsSuccess, int ErrorCode);

}
