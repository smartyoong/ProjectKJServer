﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Object
{
    public interface IGOAPAction
    {
        public float GetGoalChange(GOAPGoal goal);
        public IAction GetAction();

        public event Action<float>? GoalChange;
    }
    public struct GOAPGoal
    {
        public string Name;
        public float Value;
        public float GetDiscontentment(float NewValue)
        {
            return NewValue * NewValue;
        }

        public void AddValue(float Value)
        {
            this.Value += Value;
        }
    }

    public class TestGOAPAction : IGOAPAction
    {
        public event Action<float>? GoalChange;
        public float GetGoalChange(GOAPGoal Goal)
        {
            switch (Goal.Name)
            {
                case "Test":
                    return -1;
                default:
                    return 0;
            }
        }

        public IAction GetAction()
        {
            return new WaitAction(10000,1,TimeSpan.FromSeconds(5));
        }

    }
}
