using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace GameServer.Component
{
    internal class EqualVelocityRunAwayMethod : Behaviors
    {
        public SteeringHandle? GetSteeringHandle(float Ratio, Kinematic Character, Kinematic Target, float MaxSpeed,
            float MaxAccelerate, float MaxRotate, float MaxAngular, float TargetRadius, float SlowRadius, float TimeToTarget)
        {
            SteeringHandle Result = new SteeringHandle();
            Result.Linear = Character.Position - Target.Position;
            Result.Linear = Vector3.Normalize(Result.Linear);
            Result.Linear *= MaxSpeed;
            Result.Angular = 0;
            return Result;
        }
    }
}
