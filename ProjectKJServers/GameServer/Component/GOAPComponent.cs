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
        private List<GOAPGoal> Goals;
        private List<IGOAPAction> Actions;
        private ActionManager Manager;

        public List<GOAPGoal> GoalList { get { return Goals; } }
        public List<IGOAPAction> ActionList { get { return Actions; } }

        public GOAPComponent(ActionManager ActionManager, List<GOAPGoal> UserGoal, List<IGOAPAction> UserActions)
        {
            Goals = UserGoal;
            Actions = UserActions;
            Manager = ActionManager;
        }

        // 나중에 액션 매니저를 만들어서 액션 큐에 아무것도 없을때, 액션 계획을 실행시키도록 하자
        // Update 메서드에서 ActionManager에게 액션을 추가하도록하자
        public void Update()
        {
            //만약에 목표치가 0이면, 액션 계획을 실행시킬 필요가 없다.
            if (Goals.Aggregate(0f, (acc, Goal) => acc + Goal.Value) <= 0f)
            {
                return;
            }

            // 아 근데 여기서 액션을 지정하고 실제로 액션일 실행될때, Goal의 Value를 변경시켜야하는데,,
            // 그럼 액션 매니저에서 감소를 시켜야하는건가? 아니면 여기서 감소를 시켜야하는건가?
            // 가장 깔끔한건 Action 매니저에서 감소를 시키는게 좋을것 같다.
            // 그러면 Action Manager가 Goal을 알아야하나? 아니면 GOAPComponent가 Action Manager에게 알려줘야하나? 아니면 GoalAction이 참조를해야하나?
            // 아니면 GOAPGoal에 이벤트를 추가해서 Goal이 변경될때마다 알려주는걸로 할까?
            // 일단 조합 계획까지 다 구현하고 생각해보자
            // 또한 Goal의 Value도 올려줘야하는 코드가 필요하고, Goal이 0미만으로 안떨어지게 예외처리도 필요하다..
            IGOAPAction? Action = ChooseAction();
            if (Action != null)
            {
                Manager.AddActionToSchedule(Action.GetAction());
            }
        }

        // 타이밍, 조합 계획 적용하자

        private IGOAPAction? ChooseAction()
        {
            IGOAPAction? BestAction = null;
            float BestScore = float.MaxValue;
            foreach (var Action in Actions)
            {
                float Score = 0;
                Score = CalcDiscontentment(Action);
                if (Score < BestScore)
                {
                    BestScore = Score;
                    BestAction = Action;
                }
            }
            // 대충 이런식으로 하자 이걸 Pawn이 미리 세팅해두자
            // 그리고 Action Execute에서 Event를 Invoke하자
            //if (BestAction != null)
            //{
                //BestAction.GoalChange += Goals[0].AddValue;
            //}

            return BestAction;
        }

        private float CalcDiscontentment(IGOAPAction CompAction)
        {
            float Current = 0;
            foreach (var Goal in Goals)
            {
                float NewValue = Goal.Value + CompAction.GetGoalChange(Goal);
                Current += Goal.GetDiscontentment(NewValue);
            }
            return Current;
        }
    }
}
