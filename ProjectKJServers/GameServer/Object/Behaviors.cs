using Microsoft.VisualBasic.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreUtility.Utility;

namespace GameServer.Object
{
    public class BlackBoard
    {
        private Dictionary<string, object> DataDictionary;
        public BlackBoard()
        {
            DataDictionary = new Dictionary<string, object>();
        }

        public void AddData(string Key, object Value)
        {
            DataDictionary.Add(Key, Value);
        }

        public object? GetData(string Key)
        {
            if(DataDictionary.ContainsKey(Key))
            {
                return DataDictionary[Key];
            }
            return null;
        }
    }
    public interface IBehavior
    {
        bool Run(BlackBoard? Board);
    }

    public interface ICondition
    {
        bool Check(BlackBoard? Board);
    }

    public interface IAction // 시간이 필요한 행동은 행동이 진행되는 동안 다시 Root로 못가게하자
    {
        bool Execute(BlackBoard? Board);
    }

    public interface ISelector
    {
        List<IBehavior> Behaviors { get; set; }
        bool SelectBehavior(BlackBoard? Board);
    }

    public interface ISequence
    {
        List<IBehavior> Behaviors { get; set; }
        bool RunBehaviors(BlackBoard? Board);
    }
    public interface IDecorator
    {
        IBehavior Behavior { get; set; }
        bool RunBehavior(BlackBoard? Board);
    }
    public interface IParallel // 이걸 실제로 사용할 일이 있는지 모르겠네?
    {
        //Task Run을 사용하자
        List<IBehavior> Behaviors { get; set; }
        List<IBehavior> RunningBehavior { get; set; }
        bool RunBehaviors();
        bool RunChildren(CancellationToken CancleToken);
        void Terminate(CancellationToken CancleToken);
    }

    public class RootBehavior
    {
        private IBehavior Behavior;
        private int IsRunning = 0;
        public int IsRunningNow()
        {
            return IsRunning;
        }
        public RootBehavior(IBehavior Behavior)
        {
            this.Behavior = Behavior;
        }

        public async Task Run(BlackBoard? Board)
        {
            if (Interlocked.CompareExchange(ref IsRunning, 1, 0) == 0)
            {
                try
                {
                    await Task.Run(() => Behavior.Run(Board));
                }
                catch (Exception e)
                {
                    LogManager.GetSingletone.WriteLog(e);
                }
                finally
                {
                    Interlocked.Exchange(ref IsRunning, 0);
                }
            }
        }
    }

    public class SequenceBehavior : IBehavior, ISequence
    {
        public List<IBehavior> Behaviors { get; set; }
        public SequenceBehavior()
        {
            Behaviors = new List<IBehavior>();
        }
        public bool Run(BlackBoard? Board)
        {
            return RunBehaviors(Board);
        }
        public bool RunBehaviors(BlackBoard? Board)
        {
            foreach (var Behavior in Behaviors)
            {
                if (!Behavior.Run(Board))
                {
                    return false;
                }
            }
            return true;
        }
    }

    public class SelectorBehavior : IBehavior, ISelector
    {
        public List<IBehavior> Behaviors { get; set; }
        public SelectorBehavior()
        {
            Behaviors = new List<IBehavior>();
        }
        public bool Run(BlackBoard? Board)
        {
            return SelectBehavior(Board);
        }
        public bool SelectBehavior(BlackBoard? Board)
        {
            foreach (var Behavior in Behaviors)
            {
                if (Behavior.Run(Board))
                {
                    return true;
                }
            }
            return false;
        }
    }

    // 단일 노드를 실행하는 노드
    public class RandomSelectorBehavior : IBehavior, ISelector
    {
        public List<IBehavior> Behaviors { get; set; }
        private readonly Random Random = new Random();
        public RandomSelectorBehavior()
        {
            Behaviors = new List<IBehavior>();
        }
        public bool Run(BlackBoard? Board)
        {
            return SelectBehavior(Board);
        }
        public bool SelectBehavior(BlackBoard? Board)
        {
            while(true)
            {
                int Index = RandomSelector();
                if (Behaviors[Index].Run(Board))
                {
                    return true;
                }
            }
        }
        private int RandomSelector()
        {
            return Random.Next(0, Behaviors.Count);
        }
    }

    // 무작위 순서로 순회하는 셀렉터
    public class NoDeterministicSelector : IBehavior, ISelector
    {
        public List<IBehavior> Behaviors { get; set; }
        public NoDeterministicSelector()
        {
            Behaviors = new List<IBehavior>();
        }
        public bool Run(BlackBoard? Board)
        {
            ConvertMathUtility.DurstenfeldShuffle(Behaviors);
            return SelectBehavior(Board);
        }

        public bool SelectBehavior(BlackBoard? Board)
        {
            foreach (var Behavior in Behaviors)
            {
                if (Behavior.Run(Board))
                {
                    return true;
                }
            }
            return false;
        }
    }
    // 무작위 순서로 순회하는 시퀀스
    public class NoDeterministicSequence : IBehavior, ISequence
    {
        public List<IBehavior> Behaviors { get; set; }
        public NoDeterministicSequence()
        {
            Behaviors = new List<IBehavior>();
        }

        public bool Run(BlackBoard? Board)
        {
            ConvertMathUtility.DurstenfeldShuffle(Behaviors);
            return RunBehaviors(Board);
        }

        public bool RunBehaviors(BlackBoard? Board)
        {
            foreach (var Behavior in Behaviors)
            {
                if (!Behavior.Run(Board))
                {
                    return false;
                }
            }
            return true;
        }
    }

    // 내일 데코레이터랑 병렬 노드 구현하자
}
