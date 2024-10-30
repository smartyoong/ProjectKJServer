using CoreUtility.GlobalVariable;
using CoreUtility.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;
using System.Xml.Linq;

namespace GameServer.Resource
{
    public class Node
    {
        private bool CanMove;
        private int NodeID;
        private List<int> ConnectedNodeID;
        private float X;
        private float Y;

        public Node(int NodeID, float x, float y)
        {
            this.NodeID = NodeID;
            CanMove = true;
            ConnectedNodeID = new List<int>();
            X = x;
            Y = y;
        }

        public void AddConnectedNode(int NodeID)
        {
            ConnectedNodeID.Add(NodeID);
        }

        public void RemoveConnectedNode(int NodeID)
        {
            ConnectedNodeID.Remove(NodeID);
        }

        public void SetCanMove(bool CanMove)
        {
            this.CanMove = CanMove;
        }

        public int GetNodeID()
        {
            return NodeID;
        }

        public List<int> GetConnectedNodes()
        {
            return ConnectedNodeID;
        }

        public float GetX()
        {
            return X;
        }

        public float GetY()
        {
            return Y;
        }
    }


    public class Graph
    {
        private List<Node> Nodes;
        public Graph()
        {
            Nodes = new List<Node>();
        }

        public void AddNode(Node Node)
        {
            Nodes.Add(Node);
        }

        public void RemoveNode(Node Node)
        {
            Nodes.Remove(Node);
        }

        public List<Node> GetNodes()
        {
            return Nodes;
        }
    }

    internal class MapGraph
    {
        private Graph NodeGraph;
        public MapGraph()
        {
            NodeGraph = new Graph();
        }

        public Graph MakeGraph(MapData Data, float NodeSize)
        {
            // 이 메서드는 오래걸릴것이지만,, 어차피 서버 오픈 전에 하는거니까 상관 없다.
            // 타일 기반으로 노드를 생성

            float MaxX = Data.MapBoundX;
            float MaxY = Data.MapBoundY;
            float MinX = 0;
            float MinY = 0;

            int NodeId = 0;
            Dictionary<(float, float), Node> NodeDict = new Dictionary<(float, float), Node>();

            //// 디버깅용
            //// 언리얼의 X축은 서버상의 Y축, 언리얼의 Y축은 서버상의 X축이다.
            //// 사실 범위 체크같은거 할때는 문제 없지만, (이미 언리얼 기준축으로 변환을 다 시켜놔서)
            //// 디버그 프린트할때 그림이 이상하게 나오면 축을 의심하면된다.
            //string[,] map = new string[(int)MaxX+1, (int)MaxY+1];
            //for (int i = (int)MinX; i <= (int)MaxX; i+= (int)NodeSize)
            //{
            //    for (int j = (int)MinY; j <= (int)MaxY; j+=(int)NodeSize)
            //    {
            //        map[i, j] = "-     ";
            //    }
            //}

            // 노드 생성 꼭짓점에는 부딪히지 않도록 일부 보정한다.
            for (float x = MinX + NodeSize; x <= MaxX - NodeSize; x += NodeSize)
            {
                for (float y = MinY + NodeSize; y <= MaxY - NodeSize; y += NodeSize)
                {
                    bool IsOverlapping = false;
                    foreach (var Obstacles in Data.Obstacles)
                    {
                        float ObstaclesMinX = Obstacles.Points.Min(x => x.X);
                        float ObstaclesMaxX = Obstacles.Points.Max(x => x.X);
                        float ObstaclesMinY = Obstacles.Points.Min(x => x.Y);
                        float ObstaclesMaxY = Obstacles.Points.Max(x => x.Y);

                        // 장애물이 겹쳐있는 노드는 생성하지 않는다.
                        // 노드의 경계 계산
                        float NodeMinX = x - NodeSize / 2;
                        float NodeMaxX = x + NodeSize / 2;
                        float NodeMinY = y - NodeSize / 2;
                        float NodeMaxY = y + NodeSize / 2;

                        // 장애물과 노드가 겹치는지 확인 노드 경계 꼭짓점이 1개라도 장애물 안에 있으면 노드 생성이 실패한다.
                        if (NodeMaxX >= ObstaclesMinX && NodeMinX <= ObstaclesMaxX && NodeMaxY >= ObstaclesMinY && NodeMinY <= ObstaclesMaxY)
                        {
                            IsOverlapping = true;
                            // 디버깅용
                            //LogManager.GetSingletone.WriteLog($"Node ({x}, {y}) is overlapping with {Obstacles.MeshName} obstacles. Node is not created.");
                            break;
                        }
                    }

                    if(IsOverlapping)
                    {
                        continue;
                    }

                    Node NewNode = new Node(NodeId,x,y);
                    NodeDict[(x, y)] = NewNode;
                    NodeGraph.AddNode(NewNode);
                    NodeId++;

                    // 디버깅용
                    //map[(int)x, (int)y] = "*     ";
                }
            }

            // 디버깅용
            //for (int j = (int)MaxX; j >= (int)MinX; j--)
            //{
            //    string DebugString = string.Empty;
            //    for (int i = (int)MinY; i <= (int)MaxY; i += (int)NodeSize)
            //    {
            //        DebugString += map[j, i];
            //    }
            //    if(DebugString != string.Empty)
            //        LogManager.GetSingletone.WriteLog(DebugString);
            //}

            // 노드 연결
            foreach (var kvp in NodeDict)
            {
                var (x, y) = kvp.Key;
                Node CurrentNode = kvp.Value;

                // 오른쪽 노드 연결
                if (NodeDict.ContainsKey((x + NodeSize, y)))
                {
                    CurrentNode.AddConnectedNode(NodeDict[(x + NodeSize, y)].GetNodeID());
                }

                // 위쪽 노드 연결
                if (NodeDict.ContainsKey((x, y + NodeSize)))
                {
                    CurrentNode.AddConnectedNode(NodeDict[(x, y + NodeSize)].GetNodeID());
                }

                // 왼쪽 노드 연결
                if (NodeDict.ContainsKey((x - NodeSize, y)))
                {
                    CurrentNode.AddConnectedNode(NodeDict[(x - NodeSize, y)].GetNodeID());
                }

                // 아래쪽 노드 연결
                if (NodeDict.ContainsKey((x, y - NodeSize)))
                {
                    CurrentNode.AddConnectedNode(NodeDict[(x, y - NodeSize)].GetNodeID());
                }

                // 오른쪽 위 대각선 노드 연결
                if (NodeDict.ContainsKey((x + NodeSize, y + NodeSize)))
                {
                    CurrentNode.AddConnectedNode(NodeDict[(x + NodeSize, y + NodeSize)].GetNodeID());
                }

                // 오른쪽 아래 대각선 노드 연결
                if (NodeDict.ContainsKey((x + NodeSize, y - NodeSize)))
                {
                    CurrentNode.AddConnectedNode(NodeDict[(x + NodeSize, y - NodeSize)].GetNodeID());
                }

                // 왼쪽 위 대각선 노드 연결
                if (NodeDict.ContainsKey((x - NodeSize, y + NodeSize)))
                {
                    CurrentNode.AddConnectedNode(NodeDict[(x - NodeSize, y + NodeSize)].GetNodeID());
                }

                // 왼쪽 아래 대각선 노드 연결
                if (NodeDict.ContainsKey((x - NodeSize, y - NodeSize)))
                {
                    CurrentNode.AddConnectedNode(NodeDict[(x - NodeSize, y - NodeSize)].GetNodeID());
                }

                // 디버깅용
                //var ConnectedNodes = CurrentNode.GetConnectedNodes();
                //foreach (var ConnectedNodeId in ConnectedNodes)
                //{
                //    var ConnectedNode = NodeGraph.GetNodes().FirstOrDefault(n => n.GetNodeID() == ConnectedNodeId);
                //    if (ConnectedNode != null)
                //    {
                //        LogManager.GetSingletone.WriteLog($"Node {CurrentNode.GetNodeID()} : ({CurrentNode.GetX()}, {CurrentNode.GetY()}) is connected to Node ({ConnectedNode.GetX()}, {ConnectedNode.GetY()})");
                //    }
                //}

            }

            return NodeGraph;
        }
    }
}
