using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Devices.Core;

namespace DBServer
{
    enum LoginPacketList
    {
        LoginRequest = 0,
        RegistAccountRequest = 1,
        RegistAccountResponse = 2,
        LoginResponse = 3
    }

    [Serializable]
    public record LoginRequestPacket(string AccountID, string Password);

    [Serializable]
    public record RegistAccountRequestPacket(string AccountID, string Password);

    [Serializable]
    public record RegistAccountResponsePacket(bool IsSuccess, int ErrorCode);

    [Serializable]
    public record LoginResponsePacket(bool IsSuccess, int ErrorCode);
}
