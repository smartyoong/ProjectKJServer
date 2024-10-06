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
        private float LookAhead = 400f;

        public SteeringHandle? GetSteeringHandle(float Ratio, Kinematic Character, Kinematic Target, float MaxSpeed,
            float MaxAccelerate, float MaxRotate, float MaxAngular, float TargetRadius, float SlowRadius, float TimeToTarget)
        {
            Vector3 Ray = Character.Velocity;
            Ray = Vector3.Normalize(Ray);
            Ray *= LookAhead;

            //일단 충돌 구현이 안되어있어서 null로 둔다.
            return null;

            // 언리얼의 Tarce 시스템을 가져와야겠다.
            //Collision = dectector.getCollision(Character.Position, Ray);
            //if (Collision == null)
            //    return null;
            //Target= Collision.Position + Collision.Normal * AvoidDistance;
            //ChaseMethod chase = new ChaseMethod();
            //return chase.GetSteeringHandle(Ratio, Character, Target, MaxSpeed, MaxAccelerate, MaxRotate, MaxAngular, TargetRadius, SlowRadius, TimeToTarget);
        }
    }
}
