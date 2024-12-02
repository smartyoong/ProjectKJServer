﻿using System;
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

        public GOAPComponent(ActionManager ActionManager)
        {
            Goals = new List<GOAPGoal>();
            Actions = new List<IGOAPAction>();
            Manager = ActionManager;
        }

        // 나중에 액션 매니저를 만들어서 액션 큐에 아무것도 없을때, 액션 계획을 실행시키도록 하자
        // Update 메서드에서 ActionManager에게 액션을 추가하도록하자
        public void Update()
        {
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
