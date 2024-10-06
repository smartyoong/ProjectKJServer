using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using Windows.ApplicationModel.DataTransfer;
using GameServer.Component;

namespace GameServer.Mehtod
{
    //도착지점에 완벽하게 멈추지 않고,
    //왔다 갔다 하거나 빙빙 돕니다.
    //추적을 사용할때 용이합니다 (해당지점에 전속력으로 돌진합니다.)
    class ChaseMethod : Behaviors
    {
        public SteeringHandle? GetSteeringHandle(float Ratio, Kinematic Character, Kinematic Target, float MaxSpeed, float MaxAccelerate, float MaxRotate, float MaxAngular,
            float TargetRadius, float SlowRadius, float TimeToTarget)
        {
            SteeringHandle Result = new SteeringHandle(Vector3.Zero, 0);
            Result.Linear = Target.Position - Character.Position;
            Result.Linear = Vector3.Normalize(Result.Linear) * MaxAccelerate;
            Result.Linear *= Ratio;
            return Result;
        }
    }
}
