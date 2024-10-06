using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace GameServer.Component
{
    public class PathComponent
    {
        int Total = 1;
        private Vector3[] Positions;
        int Index = 0;
        public PathComponent(int Total)
        {
            this.Total = Total;
            Positions = new Vector3[Total];
        }
        public int GetCurrentIndex()
        {
            return Index;
        }
        public Vector3 GetPosition(int Index)
        {
            return Positions[Index];
        }

        public bool Arrived(Vector3 Position)
        {
            return Vector3.Distance(Position, Positions[Index]) < 5f;
        }

        public int GetNextIndex()
        {
            Index = (Index + 1) % Total;
            return Index;
        }

        public void AddPosition(Vector3 Position, int Pos)
        {
            Positions[Pos] = Position;
        }
    }
}
