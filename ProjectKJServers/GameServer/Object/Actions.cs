using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Object
{
    public interface IAction
    {
        public bool IsRunning { get; }
        public int ExpriationTime { get; }
        public int Priority { get; }
        public bool Interrupt();
        public bool CanDoBoth(IAction Other);
        public bool IsComplete();
        public void Execute();
    }

    public interface IActionCombination
    {
        public List<IAction> Actions { get; }
        public bool Interrupt();
        public bool CanDoBoth(IAction Other);
        public bool IsComplete();

        void Execute();
    }

    public interface IActionSequence
    {
        public List<IAction> Actions { get; }
        public int ActiveIndex { get; }
        public bool Interrupt();
        public bool CanDoBoth(IAction Other);
        public bool IsComplete();
        public void Execute();
    }
    // 액션 조합
    public class ActionCombination : IActionCombination
    {
        private List<IAction> ActionList;
        public List<IAction> Actions { get { return ActionList; } }
        public ActionCombination()
        {
            ActionList = new List<IAction>();
        }

        public bool Interrupt()
        {
            foreach (var Action in ActionList)
            {
                if (Action.Interrupt())
                {
                    return true;
                }
            }
            return false;
        }

        public bool CanDoBoth(IAction Other)
        {
            foreach (var Action in ActionList)
            {
                if (!Action.CanDoBoth(Other))
                {
                    return false;
                }
            }
            return true;
        }

        public bool IsComplete()
        {
            foreach (var Action in ActionList)
            {
                if (!Action.IsComplete())
                {
                    return false;
                }
            }
            return true;
        }

        public void Execute()
        {
            foreach (var Action in ActionList)
            {
                Action.Execute();
            }
        }
    }

    // 연쇄 액션
    public class ActionSequence : IActionSequence
    {
        private List<IAction> ActionList;
        private int CurrentIndex;
        public List<IAction> Actions { get { return ActionList; } }
        public int ActiveIndex { get { return CurrentIndex; } }
        public ActionSequence()
        {
            ActionList = new List<IAction>();
            CurrentIndex = 0;
        }
        
        // 첫번째 서브 액션을 인터럽트 가능하면 인터럽트가 가능한거다.
        public bool Interrupt()
        {
            if (ActionList.Count == 0)
                return true;
            return ActionList[0].Interrupt();
        }

        public bool CanDoBoth(IAction Other)
        {
            foreach (var Action in ActionList)
            {
                if (!Action.CanDoBoth(Other))
                {
                    return false;
                }
            }
            return true;
        }

        public bool IsComplete()
        {
            if (ActionList.Count == 0)
                return true;
            return CurrentIndex >= ActionList.Count;
        }

        public void Execute()
        {
            if (ActionList.Count == 0)
                return;
            ActionList[CurrentIndex].Execute();
            CurrentIndex++;
        }
    }
}
