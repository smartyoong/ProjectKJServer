using CoreUtility.GlobalVariable;
using GameServer.Component;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                if (Component.UpdateCheck())
                {
                    UpdatePosition(Component);
                }
            });
        }

        private void UpdatePosition(UniformVelocityMovementComponent Component)
        {
            // 주의!!! 멀티스레드로 돌아갑니다

        }
    }
}
