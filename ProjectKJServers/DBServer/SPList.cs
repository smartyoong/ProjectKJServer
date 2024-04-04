using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBServer
{
    enum SP_ERROR
    {
        CONNECTION_ERROR = -1,
        SQL_QUERY_ERROR = -2,
        NONE = 0
    }
    enum SP
    {
        SP_INVALID = 0,
        SP_ID_UNIQUE_CHECK = 1,
        SP_LOGIN = 2,
        SP_REGIST_ACCOUNT = 3
    }
}
