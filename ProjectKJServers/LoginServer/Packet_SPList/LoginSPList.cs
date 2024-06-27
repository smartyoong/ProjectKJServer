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
        SP_REGIST_ACCOUNT = 3
    }
}
