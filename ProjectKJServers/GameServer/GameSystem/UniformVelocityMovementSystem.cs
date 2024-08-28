using CoreUtility.GlobalVariable;
using GameServer.Component;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace GameServer.GameSystem
{
    internal class UniformVelocityMovementSystem : IComponentSystem
    {
        ConcurrentBag<UniformVelocityMovementComponent> Components;
        private object _lock = new object();
        public UniformVelocityMovementSystem()
        {
            Components = new ConcurrentBag<UniformVelocityMovementComponent>();
        }

        public void AddComponent(UniformVelocityMovementComponent Component)
        {
            Components.Add(Component);
        }

        public void Update()
        {
            //병렬로 위치 업데이트를 시킨다
            Parallel.ForEach(Components, (Component) =>
            {
                if (Component.UpdateCheck() && Component.IsMoving)
                {
                    UpdatePosition(Component);
                }
            });
        }

        private void UpdatePosition(UniformVelocityMovementComponent Component)
        {
            // 주의!!! 멀티스레드로 돌아갑니다 이 메서드를 다른 곳에서 호출하지 마세요.
            System.Numerics.Vector3 CurrentPos = Component.Position;

            System.Numerics.Vector3 TargetPos = Component.TargetPosition;

            System.Numerics.Vector3 Direction = System.Numerics.Vector3.Normalize(TargetPos-CurrentPos);

            System.Numerics.Vector3 NewLocation = CurrentPos + Direction * Component.Speed * GameEngine.FPS_60_UPDATE_INTERVAL; // 16.67ms에 해당하는 시간

            // 목표 위치에 도달했는지 확인
            if (Component.CheckArrive(NewLocation))
            {
                Component.SetArrived();
            }
            else
            {
                Component.UpdatePosition(NewLocation);
            }
            Console.WriteLine($"Current Location: {Component.Position}");
        }
    }
}
