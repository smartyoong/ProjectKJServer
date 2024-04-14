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
    public record LoginRequestPacket(string AccountID, string Password);

    [Serializable]
    public record RegistAccountRequestPacket(string AccountID, string Password);

    [Serializable]
    public record RegistAccountResponsePacket(bool IsSuccess, int ErrorCode);

    [Serializable]
    public record LoginResponsePacket(bool IsSuccess, int ErrorCode);

    public class LoginServerMapper : IPackertMapper<LoginPacketList>
    {
        public void GetPacketClassByID(LoginPacketList ID)
        {
            switch (ID)
            {
                case LoginPacketList.LoginRequest:
                    break;
                case LoginPacketList.RegistAccountRequest:
                    break;
                case LoginPacketList.RegistAccountResponse:
                    break;
                case LoginPacketList.LoginResponse:
                    break;
            }
        }
    }
}
