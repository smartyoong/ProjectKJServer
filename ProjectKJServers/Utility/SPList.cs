
namespace KYCSQL
{
    enum SP_ERROR
    {
        CONNECTION_ERROR = -1,
        SQL_QUERY_ERROR = -2,
        NONE = 0
    }
    enum LOGIN_SP
    {
        SP_INVALID = 0,
        SP_ID_UNIQUE_CHECK = 1,
        SP_LOGIN = 2,
        SP_REGIST_ACCOUNT = 3
    }
}
