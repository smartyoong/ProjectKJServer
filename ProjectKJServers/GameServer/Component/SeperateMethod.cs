using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Component
{
    internal class SeperateMethod : Behaviors
    {
        List<Kinematic> CharacterList;
        const float Threshold = 50f;
        const float DecayCoefficient = 3f;
        public SeperateMethod(List<Kinematic> List)
        {
            CharacterList = List;
        }

        public SteeringHandle? GetSteeringHandle(float Ratio, Kinematic Character, Kinematic Target, float MaxSpeed,
            float MaxAccelerate, float MaxRotate, float MaxAngular, float TargetRadius, float SlowRadius, float TimeToTarget)
        {
            SteeringHandle Result = new SteeringHandle();
            for(int i = 0; i < CharacterList.Count; i++)
            {
                Vector3 Direction = CharacterList[i].Position - Character.Position;
                float Distance = Direction.Length();
                if (Distance < Threshold)
                {
                    float Strength = Math.Min(DecayCoefficient / (Distance * Distance), MaxAccelerate);
                    Result.Linear += -Vector3.Normalize(Direction) * Strength;
                }
            }
            return Result;
        }
    }
}
