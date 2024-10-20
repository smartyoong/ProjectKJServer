using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using GameServer.Component;
using CoreUtility.Utility;

namespace GameServer.GameSystem
{
    internal class ArcKinematicSystem
    {
        private ConcurrentBag<ArcKinematicComponent> ArcKinematicComponents;
        private long LastTickCount = Environment.TickCount64;
        public ArcKinematicSystem()
        {
            ArcKinematicComponents = new ConcurrentBag<ArcKinematicComponent>();
        }

        public void AddComponent(ArcKinematicComponent ArcKinematicComponent)
        {
            ArcKinematicComponents.Add(ArcKinematicComponent);
        }

        public void RemoveComponent(ArcKinematicComponent? ArcKinematicComponent, int Count)
        {
            if (ArcKinematicComponent == null)
            {
                return;
            }
            if (Count > 5)
            {
                LogManager.GetSingletone.WriteLog("ArcKinematicComponent 제거 실패");
                return;
            }
            if (!ArcKinematicComponents.TryTake(out ArcKinematicComponent))
            {
                LogManager.GetSingletone.WriteLog("ArcKinematicComponent 제거 실패 잠시후 재시도");
                Task.Delay(TimeSpan.FromSeconds(1));
                RemoveComponent(ArcKinematicComponent, Count++);
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
                Parallel.ForEach(ArcKinematicComponents, (ArcKinematicComponent) =>
                {
                    ArcKinematicComponent.Update(DeltaTime);
                });
                LastTickCount = CurrentTickCount;
            }
            catch (Exception e)
            {
                LogManager.GetSingletone.WriteLog(e.ToString());
            }
        }
    }
}
