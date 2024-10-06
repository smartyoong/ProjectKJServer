using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using Windows.Foundation.Numerics;
using GameServer.Component;

namespace GameServer.Mehtod
{
    internal class RunAwayMethod : Behaviors
    {
        // 타겟의 반대방향으로 이동합니다.
        // 해당 타겟이 추적중일때 타겟의 반대방향으로 계속 도망칠때 사용합니다.
        public SteeringHandle? GetSteeringHandle(float Ratio, Kinematic Character, Kinematic Target, float MaxSpeed, float MaxAccelerate, float MaxRotate, float MaxAngular,
            float TargetRadius, float SlowRadius, float TimeToTarget)
        {
            SteeringHandle Result = new SteeringHandle(Vector3.Zero, 0);
            Result.Linear = Character.Position - Target.Position;
            Result.Linear = Vector3.Normalize(Result.Linear) * MaxAccelerate;
            Result.Linear *= Ratio;
            return Result;
        }
    }
}
