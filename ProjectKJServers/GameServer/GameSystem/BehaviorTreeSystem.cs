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
    internal class BehaviorTreeSystem : IComponentSystem
    {
        ConcurrentBag<BehaviorTreeComponent> Components;
        long LastTickCount = 0;
        public BehaviorTreeSystem()
        {
            Components = new ConcurrentBag<BehaviorTreeComponent>();
        }

        public void AddComponent(BehaviorTreeComponent Component)
        {
            Components.Add(Component);
        }

        public void RemoveComponent(BehaviorTreeComponent? Component, int Count)
        {
            if (Component == null)
            {
                return;
            }
            if (Count > 5)
            {
                LogManager.GetSingletone.WriteLog("행동트리 컴포넌트 제거 실패");
                return;
            }
            if (!Components.TryTake(out Component))
            {
                LogManager.GetSingletone.WriteLog("행동트리 컴포넌트 제거 실패 잠시후 재시도");
                Task.Delay(TimeSpan.FromSeconds(1));
                RemoveComponent(Component, Count++);
            }
        }

        public void Update()
        {
            try
            {
                long CurrentTickCount = Environment.TickCount64;
                //병렬로 행동트리를 실행시킨다
                if (CurrentTickCount - LastTickCount < GameEngine.UPDATE_INTERVAL_20PERSEC)
                {
                    return;
                }
                float DeltaTime = (CurrentTickCount - LastTickCount);
                Parallel.ForEach(Components, (Component) =>
                {
                    if(Component.IsRunningNow())
                    {
                        return;
                    }
                    //행동트리를 실행시킨다
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
