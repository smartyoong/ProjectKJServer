using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using CoreUtility.Utility;

namespace GameServer.Component
{
    class MoveMethod : Behaviors
    {
        public SteeringHandle? GetSteeringHandle(float Ratio, Kinematic Character, Kinematic Target, float MaxSpeed, float MaxAccelerate, float MaxRotate, float MaxAngular,
            float TargetRadius, float SlowRadius, float TimeToTarget)
        {
            SteeringHandle Result = new SteeringHandle(Vector3.Zero, 0);

            Vector3 Direction = Target.Position - Character.Position;
            float Distance = Direction.Length();

            // 현재 속도의 제곱 크기
            //float CurrentSpeedSqr = Character.Velocity.LengthSquared();

            // 브레이크 거리를 계산하여 TargetRadius를 동적으로 설정
            //float DynamicTargetRadius = CurrentSpeedSqr / (2 * MaxAccelerate);

            if (Distance < TargetRadius)
            {
                // null을 반환 받으면 브레이크를 시작한다 (현재 속력의 반대 방향으로 최대 가속도를 적용)
                return null;
            }

            float TargetSpeed = MaxSpeed;

            //float DynamicSlowRadius = DynamicTargetRadius * 2; // 브레이크 지점에 비해서 넒은 지점을 감속 지점으로 지정

            if (Distance < SlowRadius)
            {
                TargetSpeed = MaxSpeed * Distance / SlowRadius;
            }

            Vector3 TargetVelocity = Vector3.Normalize(Direction) * TargetSpeed;

            Result.Linear = TargetVelocity - Character.Velocity;
            Result.Linear /= TimeToTarget;

            if (Result.Linear.Length() > MaxAccelerate)
            {
                Result.Linear = Vector3.Normalize(Result.Linear) * MaxAccelerate;
            }

            Result.Angular = 0;
            Result.Linear *= Ratio;
            return Result;
        }
    }
}
