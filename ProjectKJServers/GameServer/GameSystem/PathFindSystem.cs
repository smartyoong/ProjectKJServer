using CoreUtility.Utility;
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
        public List<Node>? FindPath(in Graph NodeGraph, Node StartNode, Node GoalNode, IHuristic Method);
    }

    struct NodeRecord
    {
        public Node Node;
        public Connection? Connection;
        public float CostSoFar;
        public float EstimatedTotalCost;
    }

    public class AStarPathFindSystem : IPathFind
    {
        public List<Node>? FindPath(in Graph NodeGraph, Node StartNode, Node GoalNode, IHuristic Method)
        {
            NodeRecord StartRecord = new NodeRecord();
            StartRecord.Node = StartNode;
            StartRecord.Connection = null;
            StartRecord.CostSoFar = 0;
            StartRecord.EstimatedTotalCost = Method.Calculate(StartNode, GoalNode);

            PriorityQueue<NodeRecord, float> OpenList = new PriorityQueue<NodeRecord, float>();
            Dictionary<Node, NodeRecord> OpenListHash = new Dictionary<Node, NodeRecord>();
            Dictionary<Node, NodeRecord> CloseList = new Dictionary<Node, NodeRecord>();
            List<Node> Path = new List<Node>();

            OpenList.Enqueue(StartRecord, StartRecord.EstimatedTotalCost);
            OpenListHash.Add(StartRecord.Node, StartRecord);

            while (OpenList.Count > 0)
            {
                NodeRecord CurrentRecord = OpenList.Dequeue();
                OpenListHash.Remove(CurrentRecord.Node);

                if (CurrentRecord.Node == GoalNode)
                {
                    while (CurrentRecord.Node != StartNode)
                    {
                        Path.Add(CurrentRecord.Node);
                        if(CurrentRecord.Connection == null)
                        {
                            LogManager.GetSingletone.WriteLog("ASterPathFindSystem::FindPath Connection is null");
                            break;
                        }
                        Connection CurrentConnection = CurrentRecord.Connection;
                        CurrentRecord = CloseList[CurrentConnection.GetFromNode()];
                    }
                    break;
                }

                List<Connection> Connections = NodeGraph.GetConnections(CurrentRecord.Node);

                foreach (Connection Connect in Connections)
                {
                    Node NextNode = Connect.GetToNode();
                    NodeRecord NextRecord = new NodeRecord();
                    float NextNodeCost = CurrentRecord.CostSoFar + Connect.GetCost();
                    NextRecord.Node = NextNode;
                    NextRecord.Connection = Connect;
                    NextRecord.CostSoFar = NextNodeCost;
                    NextRecord.EstimatedTotalCost = 0;
                    // 이미 방문했었는가?
                    if (CloseList.ContainsKey(NextNode))
                    {
                        NodeRecord OldNextRecord = CloseList[NextNode];
                        // 기존에 방문했던 노드 비용이 더 적으면 스킵
                        if (OldNextRecord.CostSoFar <= NextNodeCost)
                        {
                            continue;
                        }

                        // 재방문 필요
                        CloseList.Remove(NextNode);

                        // 기존에 측정했던 휴리스틱을 재활용해서 비용 절감
                        NextRecord.EstimatedTotalCost = OldNextRecord.EstimatedTotalCost - OldNextRecord.CostSoFar;
                    }
                    // 방문할 리스트에 이미 존재면서 비용 갱신이 필요한지 체크
                    else if (OpenListHash.ContainsKey(NextNode))
                    {
                        NodeRecord OldNextRecord = OpenListHash[NextNode];
                        // 기존에 방문했던 노드 비용이 더 적으면 스킵
                        if (OldNextRecord.CostSoFar <= NextNodeCost)
                        {
                            continue;
                        }

                        // 휴리스틱 재사용 및 재방문 필요
                        NextRecord.EstimatedTotalCost = OldNextRecord.EstimatedTotalCost - OldNextRecord.CostSoFar;

                        // 열린리스트에서 제거해서 재방문하도록 마킹
                        OpenListHash.Remove(NextNode);
                        NodeRecord Temp;
                        float OldPriority;
                        OpenList.Remove(OldNextRecord,out Temp, out OldPriority);
#if DEBUG
                        LogManager.GetSingletone.WriteLog($"ASterPathFindSystem에서 열린 리스트 재갱신에 의한 임시 노드 삭제 {Temp} {OldPriority}");  
#endif
                    }
                    else
                    {
                        // 첫방문은 어쩔수 없이 휴리스틱 계산
                        NextRecord.EstimatedTotalCost = Method.Calculate(NextNode, GoalNode);
                    }

                    //방문 예정 목록에 없을 경우에만 추가한다.
                    if (!OpenListHash.ContainsKey(NextNode))
                    {
                        OpenList.Enqueue(NextRecord, NextRecord.EstimatedTotalCost);
                        OpenListHash.Add(NextRecord.Node, NextRecord);
                    }
                }

                OpenListHash.Remove(CurrentRecord.Node);
                CloseList.Add(CurrentRecord.Node, CurrentRecord);
            }

            if(Path.Count > 0)
            {
                Path.Add(StartNode);
                Path.Reverse();
                return Path;
            }
            else
                return null;
        }
    }
}
