using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using GameServer.Component;

namespace GameServer.Mehtod
{
    internal class LookAtToMoveMethod : Behaviors
    {
        public SteeringHandle? GetSteeringHandle(float Ratio, Kinematic Character, Kinematic Target, float MaxSpeed,
            float MaxAccelerate, float MaxRotate, float MaxAngular, float TargetRadius, float SlowRadius, float TimeToTarget)
        {
            Vector3 Velocity = Character.Velocity;
            if (Velocity.Length() == 0)
                return null;
            Kinematic TargetKinematic = Target;
            TargetKinematic.Orientation = (float)Math.Atan2(Velocity.Y, Velocity.X);
            AlignMethod Align = new AlignMethod();
            return Align.GetSteeringHandle(Ratio, Character, TargetKinematic, MaxSpeed, MaxAccelerate, MaxRotate, MaxAngular, TargetRadius, SlowRadius, TimeToTarget);
        }
    }
}
