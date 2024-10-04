using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using CoreUtility.Utility;

namespace GameServer.Component
{
    internal class WanderMethod : Behaviors
    {
        public SteeringHandle? GetSteeringHandle(float Ratio, Kinematic Character, Kinematic Target, float MaxSpeed,
            float MaxAccelerate, float MaxRotate, float MaxAngular, float TargetRadius, float SlowRadius, float TimeToTarget)
        {
            float WanderOffset = 300f;
            float WanderRadius = 50f;
            float WanderRate = 30f;
            float WanderOrientation = 0f;

            WanderOrientation += (float)(ConvertMathUtility.RandomBinomial() * WanderRate);
            float TargetOrientation = WanderOrientation + Character.Orientation;
            Vector3 TargetPosition = Character.Position + (WanderOffset * new Vector3((float)Math.Cos(Character.Orientation), (float)Math.Sin(Character.Orientation), 0));
            TargetPosition += (WanderRadius * new Vector3((float)Math.Cos(TargetOrientation), (float)Math.Sin(TargetOrientation), 0));
            SteeringHandle Result = new SteeringHandle();
            Result.Linear = MaxAccelerate * new Vector3((float)Math.Cos(Character.Orientation), (float)Math.Sin(Character.Orientation), 0);

            LockOnMethod LockOn = new LockOnMethod();
            var Angular = LockOn.GetSteeringHandle(Ratio, Character, Target, MaxSpeed, MaxAccelerate, MaxRotate, MaxAngular, TargetRadius, SlowRadius, TimeToTarget);
            if(Angular != null)
            {
                Result.Angular = Angular.Value.Angular;
            }
            else
            {
                Result.Angular = 0;
            }
            return Result;
        }
    }
}
