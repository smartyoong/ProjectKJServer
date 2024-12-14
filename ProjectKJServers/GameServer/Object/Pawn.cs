using CoreUtility.GlobalVariable;
using GameServer.Component;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Object
{
    enum PawnType
    {
        Player = 0,
        Monster = 1,
        NPC = 2,
        Projectile = 3
    }

    interface Pawn
    {
        public KinematicComponent GetMovementComponent { get; }
        public CollisionComponent GetCollisionComponent { get; }
        public PawnType GetPawnType { get; }
        public PathComponent GetPathComponent { get; }
        public int GetCurrentMapID { get; }

        public string GetName { get; }
    }
}
