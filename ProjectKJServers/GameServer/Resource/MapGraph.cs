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
        private float X;
        private float Y;

        public Node(float x, float y)
        {
            X = x;
            Y = y;
        }

        public float GetX() => X;

        public float GetY() => Y;
    }

    public class Connection
    {
        private Node FromNode;
        private Node ToNode;
        private float Cost;

        public Connection(Node FromNode, Node ToNode, float Cost)
        {
            this.FromNode = FromNode;
            this.ToNode = ToNode;
            this.Cost = Cost;
        }

        public Node GetFromNode() => FromNode;
        public Node GetToNode() => ToNode;
        public float GetCost() => Cost;
    }


    public class Graph
    {
        private Dictionary<Node, List<Connection>> Connections;
        public Graph()
        {
            Connections = new Dictionary<Node, List<Connection>>();
        }

        public void AddConnection(Connection Connect)
        {
            if(Connections.ContainsKey(Connect.GetFromNode()))
            {
                Connections[Connect.GetFromNode()].Add(Connect);
            }
            else
            {
                Connections[Connect.GetFromNode()] = new List<Connection> { Connect };
            }
        }

        public void RemoveNode(Connection Connect)
        {
            if (Connections.ContainsKey(Connect.GetFromNode()))
            {
                Connections[Connect.GetFromNode()].Remove(Connect);
            }
        }

        public List<Connection> GetConnections(Node FromNode)
        {
            return Connections[FromNode];
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
            Dictionary<(float, float), Node> NodeDict = new Dictionary<(float, float), Node>();

#if DEBUG
            // 디버깅용
            // 언리얼의 X축은 서버상의 Y축, 언리얼의 Y축은 서버상의 X축이다.
            // 사실 범위 체크같은거 할때는 문제 없지만, (이미 언리얼 기준축으로 변환을 다 시켜놔서)
            // 디버그 프린트할때 그림이 이상하게 나오면 축을 의심하면된다.
            string[,] map = new string[(int)MaxX + 1, (int)MaxY + 1];
            for (int i = (int)MinX; i <= (int)MaxX; i += (int)NodeSize)
            {
                for (int j = (int)MinY; j <= (int)MaxY; j += (int)NodeSize)
                {
                    map[i, j] = "-     ";
                }
            }
#endif

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
#if DEBUG
                            LogManager.GetSingletone.WriteLog($"Node ({x}, {y}) is overlapping with {Obstacles.MeshName} obstacles. Node is not created.");
#endif
                            break;
                        }
                    }

                    if(IsOverlapping)
                    {
                        continue;
                    }

                    Node NewNode = new Node(x,y);
                    NodeDict[(x, y)] = NewNode;

#if DEBUG
                    map[(int)x, (int)y] = "*     ";
#endif
                }
            }

#if DEBUG
            for (int j = (int)MaxX; j >= (int)MinX; j--)
            {
                string DebugString = string.Empty;
                for (int i = (int)MinY; i <= (int)MaxY; i += (int)NodeSize)
                {
                    DebugString += map[j, i];
                }
                if (DebugString != string.Empty)
                    LogManager.GetSingletone.WriteLog(DebugString);
            }
#endif

            // 노드 연결
            foreach (var kvp in NodeDict)
            {
                var (x, y) = kvp.Key;
                Node CurrentNode = kvp.Value;

                // 오른쪽 노드 연결
                if (NodeDict.ContainsKey((x + NodeSize, y)))
                {
                    Connection Connect  = new Connection(CurrentNode, NodeDict[(x + NodeSize, y)], 1f);
                    NodeGraph.AddConnection(Connect);
                }

                // 위쪽 노드 연결
                if (NodeDict.ContainsKey((x, y + NodeSize)))
                {
                    Connection Connect = new Connection(CurrentNode, NodeDict[(x, y + NodeSize)], 1f);
                    NodeGraph.AddConnection(Connect);
                }

                // 왼쪽 노드 연결
                if (NodeDict.ContainsKey((x - NodeSize, y)))
                {
                    Connection Connect = new Connection(CurrentNode, NodeDict[(x - NodeSize, y)], 1f);
                    NodeGraph.AddConnection(Connect);
                }

                // 아래쪽 노드 연결
                if (NodeDict.ContainsKey((x, y - NodeSize)))
                {
                    Connection Connect = new Connection(CurrentNode, NodeDict[(x, y - NodeSize)], 1f);
                    NodeGraph.AddConnection(Connect);
                }

                // 오른쪽 위 대각선 노드 연결 대각선은 피타고라스 정리에 의해 루트2로 가중치를 둔다
                if (NodeDict.ContainsKey((x + NodeSize, y + NodeSize)))
                {
                    Connection Connect = new Connection(CurrentNode, NodeDict[(x + NodeSize, y + NodeSize)], 1.414f);
                    NodeGraph.AddConnection(Connect);
                }

                // 오른쪽 아래 대각선 노드 연결
                if (NodeDict.ContainsKey((x + NodeSize, y - NodeSize)))
                {
                    Connection Connect = new Connection(CurrentNode, NodeDict[(x + NodeSize, y - NodeSize)], 1.414f);
                    NodeGraph.AddConnection(Connect);
                }

                // 왼쪽 위 대각선 노드 연결
                if (NodeDict.ContainsKey((x - NodeSize, y + NodeSize)))
                {
                    Connection Connect = new Connection(CurrentNode, NodeDict[(x - NodeSize, y + NodeSize)], 1.414f);
                    NodeGraph.AddConnection(Connect);
                }

                // 왼쪽 아래 대각선 노드 연결
                if (NodeDict.ContainsKey((x - NodeSize, y - NodeSize)))
                {
                    Connection Connect = new Connection(CurrentNode, NodeDict[(x - NodeSize, y - NodeSize)], 1.414f);
                    NodeGraph.AddConnection(Connect);
                }
#if DEBUG
                var ConnectedNodes = NodeGraph.GetConnections(CurrentNode);
                foreach (var ConnectedNode in ConnectedNodes)
                {
                    if (ConnectedNode != null)
                    {
                        LogManager.GetSingletone.WriteLog($"Node ({CurrentNode.GetX()}, {CurrentNode.GetY()}) is connected to Node ({ConnectedNode.GetToNode().GetX()}, {ConnectedNode.GetToNode().GetY()})");
                    }
                }
#endif

            }

            return NodeGraph;
        }
    }
}
