using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Devices.Core;
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
    public record struct LoginRequestPacket(string AccountID, string Password);

    [Serializable]
    public record struct RegistAccountRequestPacket(string AccountID, string Password);

    [Serializable]
    public record struct RegistAccountResponsePacket(bool IsSuccess, int ErrorCode);

    [Serializable]
    public record struct LoginResponsePacket(bool IsSuccess, int ErrorCode);
}
