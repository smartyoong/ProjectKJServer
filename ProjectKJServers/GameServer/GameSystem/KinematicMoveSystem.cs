using CoreUtility.GlobalVariable;
using GameServer.Component;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using CoreUtility.Utility;

namespace GameServer.GameSystem
{
    internal class KinematicMoveSystem : IComponentSystem
    {
        ConcurrentBag<KinematicComponent> Components;
        long LastTickCount = 0;
        public KinematicMoveSystem()
        {
            Components = new ConcurrentBag<KinematicComponent>();
        }

        public void AddComponent(KinematicComponent Component)
        {
            Components.Add(Component);
        }

        public void RemoveComponent(KinematicComponent? Component, int Count)
        {
            if(Component == null)
            {
                return;
            }
            if(Count > 5)
            {
                LogManager.GetSingletone.WriteLog("컴포넌트 제거 실패");
                return;
            }
            if (!Components.TryTake(out Component))
            {
                LogManager.GetSingletone.WriteLog("컴포넌트 제거 실패 잠시후 재시도");
                Task.Delay(TimeSpan.FromSeconds(1));
                RemoveComponent(Component,Count++);
            }
        }

        public void Update()
        {
            try
            {
                long CurrentTickCount = Environment.TickCount64;
                //병렬로 위치 업데이트를 시킨다
                if (CurrentTickCount - LastTickCount < GameEngine.UPDATE_INTERVAL_20PERSEC)
                {
                    return;
                }

                float DeltaTime = (CurrentTickCount - LastTickCount);
                Parallel.ForEach(Components, (Component) =>
                {
                    Component.Update(DeltaTime);
                });

                LastTickCount = CurrentTickCount;
            }
            catch (Exception e)
            {
                LogManager.GetSingletone.WriteLog(e.Message);
            }
        }
    }
}
