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
    internal class EXPSystem : IComponentSystem
    {
        private ConcurrentBag<LevelComponent> LevelEXPComponents;
        private long LastTickCount = 0;

        public EXPSystem()
        {
            LevelEXPComponents = new ConcurrentBag<LevelComponent>();
        }

        public void AddComponent(LevelComponent HealthPointComponent)
        {
            LevelEXPComponents.Add(HealthPointComponent);
        }

        public void RemoveComponent(LevelComponent? Component, int Count)
        {
            if (Component == null)
            {
                return;
            }
            if (Count > 5)
            {
                LogManager.GetSingletone.WriteLog("Level EXP 컴포넌트 제거 실패");
                return;
            }
            if (!LevelEXPComponents.TryTake(out Component))
            {
                LogManager.GetSingletone.WriteLog("Level EXP 컴포넌트 제거 실패 잠시후 재시도");
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
                Parallel.ForEach(LevelEXPComponents, (Component) =>
                {
                    Component.UpdateEXPInfoToDB();
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
