using CoreUtility.GlobalVariable;
using CoreUtility.Utility;
using GameServer.Component;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.GameSystem
{
    internal class HealthSystem : IComponentSystem
    {
        private ConcurrentBag<HealthPointComponent> HealthPointComponents;
        private ConcurrentBag<MagicPointComponent> MagicPointComponents;
        private long LastTickCount = 0;

        public HealthSystem()
        {
            HealthPointComponents = new ConcurrentBag<HealthPointComponent>();
            MagicPointComponents = new ConcurrentBag<MagicPointComponent>();
        }

        public void AddHealthPointComponent(HealthPointComponent HealthPointComponent)
        {
            HealthPointComponents.Add(HealthPointComponent);
        }

        public void AddMagicPointComponent(MagicPointComponent MagicPointComponent)
        {
            MagicPointComponents.Add(MagicPointComponent);
        }

        public void RemoveComponent(HealthPointComponent? Component, int Count)
        {
            if (Component == null)
            {
                return;
            }
            if (Count > 5)
            {
                LogManager.GetSingletone.WriteLog("HP 컴포넌트 제거 실패");
                return;
            }
            if (!HealthPointComponents.TryTake(out Component))
            {
                LogManager.GetSingletone.WriteLog("HP 컴포넌트 제거 실패 잠시후 재시도");
                Task.Delay(TimeSpan.FromSeconds(1));
                RemoveComponent(Component, Count++);
            }
        }

        public void RemoveComponent(MagicPointComponent? Component, int Count)
        {
            if (Component == null)
            {
                return;
            }
            if (Count > 5)
            {
                LogManager.GetSingletone.WriteLog("MP 컴포넌트 제거 실패");
                return;
            }
            if (!MagicPointComponents.TryTake(out Component))
            {
                LogManager.GetSingletone.WriteLog("MP 컴포넌트 제거 실패 잠시후 재시도");
                Task.Delay(TimeSpan.FromSeconds(1));
                RemoveComponent(Component, Count++);
            }
        }
        // 2분에 한번씩 주기적으로 HP와 MP를 DB에 갱신한다.
        public void Update()
        {
            try
            {
                long CurrentTickCount = Environment.TickCount64;
                if (CurrentTickCount - LastTickCount < TimeSpan.FromMinutes(2).Ticks)
                {
                    return;
                }
                float DeltaTime = (CurrentTickCount - LastTickCount);
                Parallel.ForEach(HealthPointComponents, (Component) =>
                {
                    Component.UpdateHPInfoToDB();
                });
                Parallel.ForEach(MagicPointComponents, (Component) =>
                {
                    Component.UpdateMPInfoToDB();
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
