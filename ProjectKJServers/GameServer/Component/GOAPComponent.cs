using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreUtility.Utility;
using GameServer.Object;

namespace GameServer.Component
{
    internal class GOAPComponent
    {
        private ActionManager Manager;
        private GOAP Goaps;
        private IGOAPAction? BestAction;
        private int MaxDepth = 0;
        private float BestDiscontentment = float.MaxValue;

        public GOAPComponent(ActionManager ActionManager, GOAP Goaps)
        {
            Manager = ActionManager;
            this.Goaps = Goaps;
            MaxDepth = Goaps.GetMaxDepth();
        }

        public void Update()
        {
            // 만약 해당 코드가 제대로 작동을 안한다면 액션 매니저를 빼도된다. 굳이 만든 느낌이 들긴해
            // 그리고 아래의 코드를 주석 해제한다.
            //if (BestAction != null && !BestAction.IsComplete)
                //return;

            List<IGOAPAction> Actions = new List<IGOAPAction>();
            PlanningAction(0,Actions,Goaps);
            // 다음번 업데이트를 위해서 최고값 초기화
            BestDiscontentment = float.MaxValue;

            if (BestAction != null)
            {
                Manager.AddActionToSchedule(BestAction.GetAction());
            }

            // 액션 매니저를 사용안한다면 아래의 코드를 주석해제하고 위의 코드를 주석처리한다.
            //if (BestAction != null)
            //{
            //    BestAction.Run();
            //    BestAction = null;
            //}
        }

        // 현재 Goal 상태를 기준으로 실행 가능한 Action 목록중에서 가장 좋은 것을 선택한다. DFS 기반
        private void PlanningAction(int CurrentDepth, List<IGOAPAction> Actions, GOAP CurrentGOAP)
        {
            if(CurrentDepth >= MaxDepth)
            {
                float CurrentDiscontentment = Goaps.CalculateDiscontentment();
                if(CurrentDiscontentment < BestDiscontentment)
                {
                    BestDiscontentment = CurrentDiscontentment;
                    // 현재 실행할 딱 1개의 Action만을 선택하면 되는거다.
                    BestAction = Actions[0];

                    // 대충 이런식으로 하자 이걸 Pawn이 미리 세팅해두자
                    // 그리고 Action Execute에서 Event를 Invoke하자
                    // pawn이 미리 세팅해둬야하는 event 연결 코드 예시
                    //if (BestAction != null)  
                    //{
                    //BestAction.GoalChange += Goals[0].AddValue;
                    //}
                }
                return;
            }

            // 코루틴 함수 현재 상태값을 저장하고 멈췄다가 실행했다가하는 가장 좋은 방법인듯
            IEnumerator<IGOAPAction> ActionEnumerator = CurrentGOAP.GetActionCoroutine();

            while (ActionEnumerator.MoveNext())
            {
                IGOAPAction Action = ActionEnumerator.Current;
                // Action을 실행하고 Goal을 변경한다.
                CurrentGOAP.ApplyAction(Action);
                Actions.Add(Action);
                PlanningAction(CurrentDepth + 1, Actions, CurrentGOAP);
                // Action을 실행하고 Goal을 변경한 상태에서 다음 Action을 실행하기 위해서는
                // 이전 Action을 취소해야한다.
                CurrentGOAP.RevertAction(Action);
                Actions.Remove(Action);
            }
        }
    }
}
