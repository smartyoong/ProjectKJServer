using GameServer.Object;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Component
{
    internal class BehaviorTreeComponent
    {
        BlackBoard Board;
        RootBehavior RootBehavior;
        public BehaviorTreeComponent(IBehavior StartNode)
        {
            Board = new BlackBoard();
            RootBehavior = new RootBehavior(StartNode);
        }
        public bool IsRunningNow()
        {
            return RootBehavior.IsRunningNow()  == 0 ? false : true;
        }
        public void Run()
        {
            RootBehavior.Run(Board);
        }
    }
}
