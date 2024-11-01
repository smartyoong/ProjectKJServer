using GameServer.Resource;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.GameSystem
{
    internal class EuclideanHeuristic
    {
        public static float Calculate(Node CurrentNode, Node GoalNode)
        {
            float dx = CurrentNode.GetX() - GoalNode.GetX();
            float dy = CurrentNode.GetY() - GoalNode.GetY();
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }
    }
    internal class AstarPathFindSystem
    {
        public void AStarSearch(Node StartNode, Node GoalNode)
        {
            // 우선순위 큐를 사용하여 열린 목록을 관리
            var OpenList = new PriorityQueue<Node, float>();
            var ClosedList = new HashSet<Node>();

            StartNode.SetHeuristic(EuclideanHeuristic.Calculate(StartNode, GoalNode));
            OpenList.Enqueue(StartNode, StartNode.GetHeuristic());

            while (OpenList.Count > 0)
            {
                Node CurrentNode = OpenList.Dequeue();

                if (CurrentNode.GetNodeID() == GoalNode.GetNodeID())
                {
                    // 목표 노드에 도달
                    break;
                }

                ClosedList.Add(CurrentNode);

                foreach (var kvp in CurrentNode.GetConnectedNodes())
                {
                    Node NeighborNode = GetNodeById(kvp.Key); // 노드 ID로 노드를 가져오는 함수
                    if (ClosedList.Contains(NeighborNode))
                    {
                        continue;
                    }

                    float TentativeGScore = CurrentNode.GetHeuristic() + kvp.Value;

                    if (!OpenList.Contains(NeighborNode) || TentativeGScore < NeighborNode.GetHeuristic())
                    {
                        NeighborNode.SetHeuristic(TentativeGScore + EuclideanHeuristic.Calculate(NeighborNode, GoalNode));
                        OpenList.Enqueue(NeighborNode, NeighborNode.GetHeuristic());
                    }
                }
            }
        }
    }
}
