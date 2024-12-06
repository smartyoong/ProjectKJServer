using System;
using System.Collections;
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
        public TimeSpan GetDurationTime();
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


    public class GOAP
    {
        private List<GOAPGoal> Goals;
        private List<IGOAPAction> Actions;
        public GOAP(List<GOAPGoal> Goals, List<IGOAPAction> Actions)
        {
            this.Goals = Goals;
            this.Actions = Actions;
        }

        public float CalculateDiscontentment()
        {
            return Goals.Aggregate(0f, (discontentment, goal) => discontentment + goal.GetDiscontentment(goal.Value));
        }

        public void ApplyAction(IGOAPAction Action)
        {
            foreach (var Goal in Goals)
            {
                Goal.AddValue(Action.GetGoalChange(Goal));
            }
        }
        // 재귀 형식으로 사용하면서, 현재의 함수 진행상황을 일시정지했다가 재개했다가 하는 형식이므로
        // 코루틴이 매우 적합하다.
        // C#의 코루틴은 IEnumerator를 통해 외부에서 관리하기때문에, 매개변수로 이 클래스를 넘겨도 문제가 없다.
        // 그러나 C++은 co_yield, co_return을 사용하여서 내부에서 관리하기 때문에 매개변수로 넘겨도 상태가 저장된다. co객체에서
        public IEnumerator<IGOAPAction> GetActionCoroutine()
        {
            for (int i = 0; i < Actions.Count; i++)
            {
                yield return Actions[i];
            }
        }
        public int GetMaxDepth()
        {
            return Actions.Count;
        }
    }


    public class TestGOAPAction : IGOAPAction
    {
        private TimeSpan DurationTime;
        public TestGOAPAction(TimeSpan DurationTime)
        {
            this.DurationTime = DurationTime;
        }
        public TimeSpan GetDurationTime()
        {
            return DurationTime;
        }
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
