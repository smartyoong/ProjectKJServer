using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using GameServer.Component;
using Windows.Storage;

namespace GameServer.Mehtod
{
    internal class ObstacleAvoidanceMethod : Behaviors
    {
        // 벽으로의 최소 거리 캐릭터 보다 커야한다.
        private float AvoidDistance = 200f;
        Vector3 CollisionPosition;
        Vector3 CollisionNormal;
        public ObstacleAvoidanceMethod(Vector3 Position, Vector3 Normal)
        {
            CollisionPosition = Position;
            CollisionNormal = Normal;
        }

        public SteeringHandle? GetSteeringHandle(float Ratio, Kinematic Character, Kinematic Target, float MaxSpeed,
            float MaxAccelerate, float MaxRotate, float MaxAngular, float TargetRadius, float SlowRadius, float TimeToTarget)
        {
            Target.Position = CollisionPosition + CollisionNormal * AvoidDistance;
            ChaseMethod chase = new ChaseMethod();
            return chase.GetSteeringHandle(Ratio, Character, Target, MaxSpeed, MaxAccelerate, MaxRotate, MaxAngular, TargetRadius, SlowRadius, TimeToTarget);
        }
    }
}
