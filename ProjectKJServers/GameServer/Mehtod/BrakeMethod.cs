using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using GameServer.Component;

namespace GameServer.Mehtod
{
    internal class BrakeMethod : Behaviors
    {
        public SteeringHandle? GetSteeringHandle(float Ratio, Kinematic Character, Kinematic Target, float MaxSpeed, float MaxAccelerate, float MaxRotate, float MaxAngular, float TargetRadius, float SlowRadius, float TimeToTarget/*delta time으로 변수 재활용*/)
        {
            // 현재 속도 방향의 반대 방향으로 브레이크 가속도 적용
            Vector3 BrakeDirection = -Vector3.Normalize(Character.Velocity);
            Vector3 BrakeForce = BrakeDirection * MaxAccelerate;

            // 속도가 매우 작아졌을 때 객체를 완전히 멈추도록 설정
            const float VelocityThreshold = 0.01f; // 속도가 매우 작다고 간주할 임계값

            // 속도가 0을 넘어 반대 방향으로 가는 것을 방지 (null을 받으면 정지)
            float Diff = Character.Velocity.Length() - BrakeForce.Length() * TimeToTarget; // 여기서 TimeToTarget은 DeltaTime
            if (Diff < VelocityThreshold || Vector3.Dot(Character.Velocity, BrakeDirection) > 0)
            {
                return null;
            }

            return new SteeringHandle(BrakeForce, 0);
        }
    }
}
