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
        List<IParallelAction> Behaviors { get; set; }
        bool RunBehaviors(BlackBoard? Board);
        void Terminate();
    }

    public interface IParallelAction
    {
        bool Run(BlackBoard? Board, CancellationToken CancleToken);
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


    public class LimitDecorator : IBehavior, IDecorator
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

    public class UntilFailDecorator : IBehavior, IDecorator
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

    public class InverterDecorator : IBehavior, IDecorator
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
    public class SemaphoreDecorator : IBehavior, IDecorator
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

    public class WaitAction : IBehavior, IAction
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
    // 애초에 사용을 할까나?
    public class ParallelBehavior : IBehavior, IParallel
    {
        public List<IParallelAction> Behaviors { get; set; }
        private List<Task> RunningAction;
        private CancellationTokenSource CancleTokenSource;
        public ParallelBehavior()
        {
            Behaviors = new List<IParallelAction>();
            CancleTokenSource = new CancellationTokenSource();
            RunningAction = new List<Task>();
        }
        public bool Run(BlackBoard? Board)
        {
            return RunBehaviors(Board);
        }
        public bool RunBehaviors(BlackBoard? Board)
        {
            bool Result = true;
            CancleTokenSource = new CancellationTokenSource();
            RunningAction.Clear();
            foreach (var Behavior in Behaviors)
            {
                RunningAction.Add(Task.Run(() =>
                {
                    if (!Behavior.Run(Board, CancleTokenSource.Token))
                    {
                        Interlocked.Exchange(ref Result, false);
                        Terminate();
                    }
                }));
            }

            if(CancleTokenSource.Token.IsCancellationRequested)
            {
                return false;
            }

            Task.WaitAll(RunningAction.ToArray(),CancleTokenSource.Token);
            return Result;
        }
        public void Terminate()
        {
            CancleTokenSource.Cancel();
        }
    }

    // 아래의 클래스들은 디버그용이다.
    public class LogAction : IBehavior, IAction
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

    public class RandomCondition : IBehavior, ICondition
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
        // 인터럽트는 원자적으로 다른 행동노드에게 조건 변수 값을 변경 시키는건데 
        // 구현은 가능하겠는데 이게 노드간 건너뛰는거라서 일단 구현을 배제하자 구조적 정통성이 깨진다.
}
