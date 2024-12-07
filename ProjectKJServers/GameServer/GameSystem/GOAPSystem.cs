using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameServer.Component;
using CoreUtility.Utility;
using CoreUtility.GlobalVariable;

namespace GameServer.GameSystem
{
    internal class GOAPSystem : IComponentSystem
    {
        private ConcurrentBag<GOAPComponent> Components;
        private long LastTickCount = 0;
        public GOAPSystem()
        {
            Components = new ConcurrentBag<GOAPComponent>();
        }

        public void AddComponent(GOAPComponent Component)
        {
            Components.Add(Component);
        }

        public void RemoveComponent(GOAPComponent? Component, int Count)
        {
            if (Component == null)
            {
                return;
            }
            if (Count > 5)
            {
                LogManager.GetSingletone.WriteLog("GOAP 컴포넌트 제거 실패");
                return;
            }
            if (!Components.TryTake(out Component))
            {
                LogManager.GetSingletone.WriteLog("GOAP 컴포넌트 제거 실패 잠시후 재시도");
                Task.Delay(TimeSpan.FromSeconds(1));
                RemoveComponent(Component, Count++);
            }
        }

        public void Update()
        {
            try
            {
                long CurrentTickCount = Environment.TickCount64;
                //병렬로 GOAP를 실행시킨다
                if (CurrentTickCount - LastTickCount < GameEngine.UPDATE_INTERVAL_20PERSEC)
                {
                    return;
                }
                float DeltaTime = (CurrentTickCount - LastTickCount);
                Parallel.ForEach(Components, (Component) =>
                {
                    // 업데이트 조건을 추가할까? 그냥해도 될것 같긴하다
                    Component.Update();
                });
                LastTickCount = CurrentTickCount;
            }
            catch (Exception e)
            {
                LogManager.GetSingletone.WriteLog(e);
            }
        }
    }
}
