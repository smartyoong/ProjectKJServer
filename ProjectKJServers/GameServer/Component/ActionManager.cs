using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameServer.Object;

namespace GameServer.Component
{
    internal class ActionManager
    {
        private List<IAction> ActionList;
        private List<IAction> ActiveActionList;

        public ActionManager()
        {
            ActionList = new List<IAction>();
            ActiveActionList = new List<IAction>();
        }

        public void AddActionToSchedule(IAction Action)
        {
            ActionList.Add(Action);
        }

        public void Run()
        {
            Execute();
        }
        // 이 함수 뭔가 빠진거 같은데? 계속 동적 Update를 시키는 함수가 아닌거 같은데
        // 상관 없겠다. 이미 실행중인 함수가 있으면, 준비리스트에서 대기중이겠네
        // 우선 순위가 낮다면, 만료되어서 삭제될 수도 있겠네 실행 못하고
        private void Execute()
        {
            int CurrentTime = Environment.TickCount;
            int PriorityCutOff = ActiveActionList.Any() ? ActiveActionList.Max(a => a.Priority) : int.MinValue;

            ActionList.RemoveAll(action => action.ExpriationTime < CurrentTime);

            // 실행 준비 리스트를 우선순위로 정렬
            ActionList.Sort((a, b) => b.Priority.CompareTo(a.Priority));

            foreach (var action in ActionList)
            {
                if (action.Priority <= PriorityCutOff)
                    break;

                if (action.Interrupt())
                {
                    ActionList.Remove(action);
                    // 전부 날리는게 맞을까?
                    ActiveActionList.Clear();
                    ActiveActionList.Add(action);
                    PriorityCutOff = action.Priority;
                }
                else
                {
                    // 만약 1개도 리스트에 없다면 아래의 변수가 true여서 실행 액션에 추가될것이다.
                    bool CanAddToActive = true;
                    foreach (var activeAction in ActiveActionList)
                    {
                        if (!activeAction.CanDoBoth(action))
                        {
                            CanAddToActive = false;
                            break;
                        }
                    }

                    if (CanAddToActive)
                    {
                        ActiveActionList.Add(action);
                        ActionList.Remove(action);
                        PriorityCutOff = action.Priority;
                    }
                }
            }

            // 우선 순위를 기반으로 정렬
            ActiveActionList.Sort((a, b) => a.Priority.CompareTo(b.Priority));

            foreach (var action in ActiveActionList)
            {
                if (action.IsComplete())
                {
                    ActiveActionList.Remove(action);
                }
                else
                {
                    action.Execute();
                }
            }
        }
    }
}
