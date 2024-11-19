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
            if (DataDictionary.ContainsKey(Key))
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

    public interface IBTCondition
    {
        bool Check(BlackBoard? Board);
    }

    public interface IBTAction // 시간이 필요한 행동은 행동이 진행되는 동안 다시 Root로 못가게하자
    {
        bool Execute(BlackBoard? Board);
    }

    public interface IBTSelector
    {
        List<IBehavior> Behaviors { get; set; }
        bool SelectBehavior(BlackBoard? Board);
    }

    public interface IBTSequence
    {
        List<IBehavior> Behaviors { get; set; }
        bool RunBehaviors(BlackBoard? Board);
    }
    public interface IBTDecorator
    {
        IBehavior Behavior { get; set; }
        bool RunBehavior(BlackBoard? Board);
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

        public void Run(BlackBoard? Board)
        {
            if (Interlocked.CompareExchange(ref IsRunning, 1, 0) == 0)
            {
                try
                {
                    Behavior.Run(Board);
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

    public class SequenceBehavior : IBehavior, IBTSequence
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

    public class SelectorBehavior : IBehavior, IBTSelector
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
    public class RandomSelectorBehavior : IBehavior, IBTSelector
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
            while (true)
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
    public class NoDeterministicSelector : IBehavior, IBTSelector
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
    public class NoDeterministicSequence : IBehavior, IBTSequence
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


    public class LimitDecorator : IBehavior, IBTDecorator
    {
        public IBehavior Behavior { get; set; }
        private int LimitCount;
        private int Count = 0;
        public LimitDecorator(IBehavior Behavior, int LimitCount)
        {
            this.Behavior = Behavior;
            this.LimitCount = LimitCount;
        }
        public bool Run(BlackBoard? Board)
        {
            return RunBehavior(Board);
        }

        public bool RunBehavior(BlackBoard? Board)
        {
            if (Count < LimitCount)
            {
                Count++;
                return Behavior.Run(Board);
            }
            return false;
        }
    }

    public class UntilFailDecorator : IBehavior, IBTDecorator
    {
        public IBehavior Behavior { get; set; }
        public UntilFailDecorator(IBehavior Behavior)
        {
            this.Behavior = Behavior;
        }
        public bool Run(BlackBoard? Board)
        {
            return RunBehavior(Board);
        }
        public bool RunBehavior(BlackBoard? Board)
        {
            while (Behavior.Run(Board))
            {
                continue;
            }
            return true;
        }
    }

    public class InverterDecorator : IBehavior, IBTDecorator
    {
        public IBehavior Behavior { get; set; }
        public InverterDecorator(IBehavior Behavior)
        {
            this.Behavior = Behavior;
        }
        public bool Run(BlackBoard? Board)
        {
            return RunBehavior(Board);
        }
        public bool RunBehavior(BlackBoard? Board)
        {
            return !Behavior.Run(Board);
        }
    }

    // 이걸 쓸일이 있을까?
    public class SemaphoreDecorator : IBehavior, IBTDecorator
    {
        public IBehavior Behavior { get; set; }
        private Semaphore Sema;
        public SemaphoreDecorator(IBehavior Behavior, int MaxConCurrentCount)
        {
            this.Behavior = Behavior;
            Sema = new Semaphore(MaxConCurrentCount, MaxConCurrentCount);
        }
        public bool Run(BlackBoard? Board)
        {
            return RunBehavior(Board);
        }
        public bool RunBehavior(BlackBoard? Board)
        {
            if (Sema.WaitOne(TimeSpan.FromSeconds(1)))
            {
                try
                {
                    return Behavior.Run(Board);
                }
                finally
                {
                    Sema.Release();
                }
            }
            return false;
        }
    }

    public class WaitAction : IBehavior, IBTAction
    {
        private float WaitTime;
        public WaitAction(float WaitTime)
        {
            this.WaitTime = WaitTime;
        }

        public bool Run(BlackBoard? Board)
        {
            return Execute(Board);
        }

        public bool Execute(BlackBoard? Board)
        {
            Task.Delay(TimeSpan.FromSeconds(WaitTime)).Wait();
            return true;
        }
    }

    // 아래의 클래스들은 디버그용이다.
    public class LogAction : IBehavior, IBTAction
    {
        private string LogMessage;
        public LogAction(string LogMessage)
        {
            this.LogMessage = LogMessage;
        }
        public bool Run(BlackBoard? Board)
        {
            return Execute(Board);
        }

        public bool Execute(BlackBoard? Board)
        {
            LogManager.GetSingletone.WriteLog(LogMessage);
            return true;
        }
    }

    public class RandomCondition : IBehavior, IBTCondition
    {

        private float Probability;
        private Random Random;
        public RandomCondition(float Probability)
        {
            this.Probability = Probability;
            Random = new Random();
        }
        public bool Check(BlackBoard? Board)
        {
            return Random.NextDouble() < Probability;
        }
        public bool Run(BlackBoard? Board)
        {
            return Check(Board);
        }
    }
}
