using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using CoreUtility.Utility;

namespace GameServer.Component
{
    class MoveMethod : Behaviors
    {
        public SteeringHandle? GetSteeringHandle(float Ratio, Kinematic Character, Kinematic Target, float MaxSpeed, float MaxAccelerate, float MaxRotate, float MaxAngular,
            float TargetRadius, float SlowRadius, float TimeToTarget)
        {
            SteeringHandle Result = new SteeringHandle(Vector3.Zero, 0);

            Vector3 Direction = Target.Position - Character.Position;
            float Distance = Direction.Length();

            if (Distance < TargetRadius)
            {
                return null;
            }

            float TargetSpeed = MaxSpeed;

            if (Distance < SlowRadius)
            {
                TargetSpeed = MaxSpeed * Distance / SlowRadius;
            }

            Vector3 TargetVelocity = Vector3.Normalize(Direction) * TargetSpeed;

            Result.Linear = TargetVelocity - Character.Velocity;
            Result.Linear /= TimeToTarget;

            if (Result.Linear.Length() > MaxAccelerate)
            {
                Result.Linear = Vector3.Normalize(Result.Linear) * MaxAccelerate;
            }

            Result.Angular = 0;
            Result.Linear *= Ratio;
            return Result;
        }
    }
}
