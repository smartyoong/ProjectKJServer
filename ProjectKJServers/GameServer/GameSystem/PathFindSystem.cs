using GameServer.Resource;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.GameSystem
{
    public interface IHuristic
    {
        public float Calculate(Node CurrentNode, Node GoalNode);
    }

    internal class EuclideanHuristic : IHuristic
    {
        public float Calculate(Node CurrentNode, Node GoalNode)
        {
            float dx = CurrentNode.GetX() - GoalNode.GetX();
            float dy = CurrentNode.GetY() - GoalNode.GetY();
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }
    }

    public interface IPathFind
    {
        public List<Node> FindPath(in Graph NodeGraph, Node StartNode, Node GoalNode, IHuristic Method);
    }

    class NodeRecord
    {

    }

    public class ASterPathFindSystem : IPathFind
    {
        public List<Node> FindPath(in Graph NodeGraph, Node StartNode, Node GoalNode, IHuristic Method)
        {
            List<Node> Result = new List<Node>();

            return Result;
        }
    }
}
