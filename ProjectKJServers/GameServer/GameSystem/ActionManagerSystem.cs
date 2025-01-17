﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreUtility.GlobalVariable;
using GameServer.Component;
using CoreUtility.Utility;

namespace GameServer.GameSystem
{
    internal class ActionManagerSystem : IComponentSystem
    {
        ConcurrentBag<ActionManager> Components;
        long LastTickCount = 0;
        public ActionManagerSystem()
        {
            Components = new ConcurrentBag<ActionManager>();
        }

        public void AddComponent(ActionManager Component)
        {
            Components.Add(Component);
        }

        public void RemoveComponent(ActionManager? Component, int Count)
        {
            if (Component == null)
            {
                return;
            }
            if (Count > 5)
            {
                LogManager.GetSingletone.WriteLog("액션 매니저 컴포넌트 제거 실패");
                return;
            }
            if (!Components.TryTake(out Component))
            {
                LogManager.GetSingletone.WriteLog("액션 매니저 컴포넌트 제거 실패 잠시후 재시도");
                Task.Delay(TimeSpan.FromSeconds(1));
                RemoveComponent(Component, Count++);
            }
        }

        public void Update()
        {
            try
            {
                long CurrentTickCount = Environment.TickCount64;
                if (CurrentTickCount - LastTickCount < GameEngine.UPDATE_INTERVAL_20PERSEC)
                {
                    return;
                }
                float DeltaTime = (CurrentTickCount - LastTickCount);
                Parallel.ForEach(Components, (Component) =>
                {
                    Component.Run();
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
