using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace GameServer.Component
{
    internal class VelocityMatchMethod : Behaviors
    {
        public SteeringHandle? GetSteeringHandle(float Ratio, Kinematic Character, Kinematic Target, float MaxSpeed, float MaxAccelerate, float MaxRotate, float MaxAngular, float TargetRadius, float SlowRadius, float TimeToTarget)
        {
            SteeringHandle Result = new SteeringHandle(Vector3.Zero,0);
            Result.Linear = Target.Velocity - Character.Velocity;
            Result.Linear /= TimeToTarget;
            if (Result.Linear.Length() > MaxAccelerate)
            {
                Result.Linear = Vector3.Normalize(Result.Linear);
                Result.Linear *= MaxAccelerate;
            }
            Result.Angular = 0;
            return Result;
        }
    }
}
