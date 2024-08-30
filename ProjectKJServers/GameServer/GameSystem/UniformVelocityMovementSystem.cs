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
        long LastTickCount = 0;
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
            long CurrentTickCount = Environment.TickCount64;
            //병렬로 위치 업데이트를 시킨다
            if (CurrentTickCount - LastTickCount < GameEngine.UPDATE_INTERVAL_20PERSEC)
            {
                return;
            }
            float DeltaTime = (CurrentTickCount - LastTickCount) / 1000;
            Parallel.ForEach(Components, (Component) =>
            {
                if (Component.IsMoving)
                {
                    UpdatePosition(Component,DeltaTime);
                }
            });

            LastTickCount = CurrentTickCount;
        }

        private void UpdatePosition(UniformVelocityMovementComponent Component, float DeltaTime)
        {
            // 주의!!! 멀티스레드로 돌아갑니다 이 메서드를 다른 곳에서 호출하지 마세요.
            System.Numerics.Vector3 CurrentPos = Component.Position;

            System.Numerics.Vector3 TargetPos = Component.TargetPosition;

            System.Numerics.Vector3 Direction = System.Numerics.Vector3.Normalize(TargetPos-CurrentPos);

            System.Numerics.Vector3 NewLocation = CurrentPos + Direction * Component.Speed * DeltaTime;

            // 목표 위치에 도달했는지 확인
            if (System.Numerics.Vector3.Distance(NewLocation, Component.TargetPosition) <= Component.Speed * DeltaTime)
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
