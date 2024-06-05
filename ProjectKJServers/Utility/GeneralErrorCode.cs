using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KYCException
{

    // 에러 코드 전용 패킷의 에러 코드는 반드시 Enum 값을 사용해야한다.
    // 만약 일반 Int로 에러코드를 사용한다면, dynamic에서 에러코드 패킷으로 인식한다.
    public enum GeneralErrorCode
    {
        ERR_PACKET_IS_NOT_ASSIGNED = 0
      , ERR_PACKET_IS_NULL = 1
      , ERR_HASH_CODE_IS_NOT_REGIST = 2
      , ERR_HASH_CODE_NICKNAME_DUPLICATED = 3
      , ERR_AUTH_SUCCESS = 4
      , ERR_AUTH_FAIL = 5
      , ERR_AUTH_RETRY = 6
      , ERR_SQL_RETURN_ERROR = 999999
    }

    public struct ErrorPacket(GeneralErrorCode ErrorCode)
    {
        public GeneralErrorCode ErrorCode { get; } = ErrorCode;
    }

    enum SP_ERROR
    {
        CONNECTION_ERROR = -1,
        SQL_QUERY_ERROR = -2,
        NONE = 0
    }
}
