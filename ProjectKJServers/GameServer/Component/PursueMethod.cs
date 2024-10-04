using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Component
{
    class PursueMethod : Behaviors
    {
        public SteeringHandle? GetSteeringHandle(float Ratio, Kinematic Character, Kinematic Target, float MaxSpeed,
            float MaxAccelerate, float MaxRotate, float MaxAngular, float TargetRadius, float SlowRadius, float TimeToTarget)
        {
            const float MaxPrediction = 30f;
            Vector3 Direction = Target.Position - Character.Position;
            float Distance = Direction.Length();
            float Speed = Character.Velocity.Length();
            float Prediction;
            if (Speed <= Distance / MaxPrediction)
            {
                Prediction = MaxPrediction;
            }
            else
            {
                Prediction = Distance / Speed;
            }
            Kinematic FutureTarget = new Kinematic();
            FutureTarget.Position += Target.Velocity * Prediction;
            ChaseMethod Chase = new ChaseMethod();
            return Chase.GetSteeringHandle(Ratio, Character, FutureTarget, MaxSpeed, MaxAccelerate, MaxRotate, MaxAngular, TargetRadius, SlowRadius, TimeToTarget);
        }
    }
}
