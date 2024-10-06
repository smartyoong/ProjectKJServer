using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using CoreUtility.Utility;
using GameServer.Component;

namespace GameServer.Mehtod
{
    internal class EqualVelocityWanderMethod : Behaviors
    {
        public SteeringHandle? GetSteeringHandle(float Ratio, Kinematic Character, Kinematic Target, float MaxSpeed,
            float MaxAccelerate, float MaxRotate, float MaxAngular, float TargetRadius, float SlowRadius, float TimeToTarget)
        {
            SteeringHandle Result = new SteeringHandle();
            float X = (float)Math.Cos(Character.Orientation);
            float Y = (float)Math.Sin(Character.Orientation);
            Result.Linear = new Vector3(X, Y, 0) * MaxSpeed;
            float WanderOrientation = (float)ConvertMathUtility.RandomBinomial() * MaxRotate;
            return Result;
        }
    }
}
