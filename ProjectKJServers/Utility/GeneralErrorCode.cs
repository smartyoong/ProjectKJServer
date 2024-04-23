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
    }

    public struct ErrorPacket(GeneralErrorCode ErrorCode)
    {
        public GeneralErrorCode ErrorCode { get; } = ErrorCode;
    }
}
