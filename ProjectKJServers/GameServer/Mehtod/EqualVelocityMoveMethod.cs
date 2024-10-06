using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using GameServer.Component;

namespace GameServer.Mehtod
{
    internal class EqualVelocityMoveMethod : Behaviors
    {
        public SteeringHandle? GetSteeringHandle(float Ratio, Kinematic Character, Kinematic Target, float MaxSpeed, float MaxAccelerate, float MaxRotate, float MaxAngular,
            float TargetRadius, float SlowRadius, float TimeToTarget)
        {
            SteeringHandle Result = new SteeringHandle();
            Result.Linear = Target.Position - Character.Position;
            if (Result.Linear.Length() < TargetRadius)
            {
                return null;
            }
            Result.Linear = Result.Linear / TimeToTarget;
            if (Result.Linear.Length() > MaxSpeed)
            {
                Result.Linear = Vector3.Normalize(Result.Linear) * MaxSpeed;
            }
            Result.Angular = 0;
            return Result;
        }
    }
}
