using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using CoreUtility.Utility;
using GameServer.Component;

namespace GameServer.Mehtod
{
    internal class AlignMethod : Behaviors
    {
        public SteeringHandle? GetSteeringHandle(float Ratio, Kinematic Character, Kinematic Target, float MaxSpeed, float MaxAccelerate, float MaxRotate, float MaxAngular, float TargetRadius, float SlowRadius, float TimeToTarget)
        {
            SteeringHandle Result = new SteeringHandle(Vector3.Zero, 0);
            float Rotation = Target.Orientation - Character.Orientation;
            Rotation = ConvertMathUtility.MapToRange(Rotation);
            float RotationSize = Math.Abs(Rotation);
            //회전을 강제로 멈추도록 요청한다.
            if (RotationSize < 0.1f)
            {
                return null;
            }

            float GoalRotation = 0;
            if (RotationSize > 5f)
            {
                GoalRotation = MaxRotate;
            }
            else
            {
                GoalRotation = MaxRotate * RotationSize / 5f;
            }

            GoalRotation *= Rotation / RotationSize;

            Result.Angular = GoalRotation - Character.Rotation;
            Result.Angular /= TimeToTarget;

            float AngularAcceleration = Math.Abs(Result.Angular);
            if (AngularAcceleration > MaxAngular)
            {
                Result.Angular /= AngularAcceleration;
                Result.Angular *= MaxAngular;
            }
            Result.Linear = Vector3.Zero;
            return Result;
        }
    }
}
