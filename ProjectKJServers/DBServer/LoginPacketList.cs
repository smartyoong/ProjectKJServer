using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBServer
{
    enum LoginPacketList
    {
        LoginRequest = 0,
        RegistAccountRequest = 1,
        RegistAccountResponse = 2,
        LoginResponse = 3,
        LoginRequestError = 4,
        RegistAccountRequestError = 5
    }
}
