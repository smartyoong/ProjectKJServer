using GameServer.Object;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Component
{
    internal class BehaviorTree
    {
        BlackBoard Board;
        RootBehavior Root;
        public BehaviorTree()
        {
            Board = new BlackBoard();
            Root = new RootBehavior();
        }
    }
}
