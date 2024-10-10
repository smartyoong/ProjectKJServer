using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using GameServer.Component;

namespace GameServer.Mehtod
{
    internal class FollowPathMethod : Behaviors
    {
        private PathComponent Component;
        public FollowPathMethod(ref PathComponent Path)
        {
            Component = Path;
        }

        public SteeringHandle? GetSteeringHandle(float Ratio, Kinematic Character, Kinematic Target, float MaxSpeed,
            float MaxAccelerate, float MaxRotate, float MaxAngular, float TargetRadius, float SlowRadius, float TimeToTarget)
        {
            Kinematic TargetPoint = Target;
            int CurrentIndex = Component.GetCurrentIndex();
            TargetPoint.Position = Component.GetPosition(CurrentIndex);

            if (Component.Arrived(Character.Position))
            {
                CurrentIndex = Component.GetNextIndex();
                TargetPoint.Position = Component.GetPosition(CurrentIndex);
            }

            EqualVelocityChaseMethod Chase = new EqualVelocityChaseMethod();
            return Chase.GetSteeringHandle(Ratio, Character, TargetPoint, MaxSpeed, MaxAccelerate, MaxRotate, MaxAngular, TargetRadius, SlowRadius, TimeToTarget);
        }
    }
}
