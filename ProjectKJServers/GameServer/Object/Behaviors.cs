using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    public class RootBehavior : IBehavior, ISequence
    {
        public List<IBehavior> Behaviors { get; set; }
        public RootBehavior()
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
}
