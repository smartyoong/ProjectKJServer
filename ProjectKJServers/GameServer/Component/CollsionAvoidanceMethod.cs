using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using Windows.UI.Composition;

namespace GameServer.Component
{
    internal class CollsionAvoidanceMethod : Behaviors
    {
        private List<Kinematic> TargetList;
        public CollsionAvoidanceMethod(List<Kinematic> TargetList)
        {
            this.TargetList = TargetList;
        }

        public SteeringHandle? GetSteeringHandle(float Ratio, Kinematic Character, Kinematic Target, float MaxSpeed,
            float MaxAccelerate, float MaxRotate, float MaxAngular, float TargetRadius, float SlowRadius, float TimeToTarget)
        {
            float ShortestTime = float.MaxValue;
            Kinematic? FirstTarget = null;
            float FirstMinSeparation = 0;
            float FirstDistance = 0;
            Vector3 FirstRelativePos = new Vector3(0, 0, 0);
            Vector3 FirstRelativeVel = new Vector3(0, 0, 0);

            foreach(Kinematic t in TargetList)
            {
                Vector3 RelativePos = t.Position - Character.Position;
                Vector3 RelativeVel = t.Velocity - Character.Velocity;
                float RelativeSpeed = RelativeVel.Length();
                float TimeToCollision = Vector3.Dot(RelativePos, RelativeVel) / (RelativeSpeed * RelativeSpeed);
                float Distance = RelativePos.Length();
                float MinSeparation = Distance - RelativeSpeed * ShortestTime;
                if (MinSeparation > 2 * TargetRadius)
                    continue;
                if (TimeToCollision > 0 && TimeToCollision < ShortestTime)
                {
                    ShortestTime = TimeToCollision;
                    FirstTarget = t;
                    FirstMinSeparation = MinSeparation;
                    FirstDistance = Distance;
                    FirstRelativePos = RelativePos;
                    FirstRelativeVel = RelativeVel;
                }
            }

            if (FirstTarget == null)
                return null;
            Vector3 Pos = new Vector3(0, 0, 0);
            // 현재 위치 기반
            if (FirstMinSeparation <= 0 || FirstDistance < 2 * TargetRadius)
            {
                Pos = FirstTarget.Value.Position - Character.Position;
            }
            else // 미래 위치 기반
            {
               Pos += FirstRelativePos + FirstRelativeVel * ShortestTime;
            }

            Pos = Vector3.Normalize(Pos);
            SteeringHandle Result = new SteeringHandle();
            Result.Linear = Pos * MaxAccelerate;
            Result.Angular = 0;

            return Result;
        }
    }
}
