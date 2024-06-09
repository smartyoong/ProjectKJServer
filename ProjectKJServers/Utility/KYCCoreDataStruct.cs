using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KYCCoreDataStruct
{
    public struct Vector3
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }

    public struct Obstacle(Vector3 Location, Vector3 Scale, Vector3 MeshSize, string MeshName)
    {
        public Vector3 Location { get; set; } = Location;
        public Vector3 Scale { get; set; } = Scale;
        public Vector3 MeshSize { get; set; } = MeshSize;
        public string MeshName { get; set; } = MeshName;
    }

    public struct MapData(int MapID, string MapName, List<Obstacle> Obstacles, List<MapPortalData> Portals)
    {
        public int MapID { get; set; } = MapID;
        public string MapName { get; set; } = MapName;
        public List<Obstacle> Obstacles { get; set; } = Obstacles;
        public List<MapPortalData> Portals { get; set; } = Portals;
    }

    public struct MapPortalData(int MapID, int LinkToMapID, string MapName, Vector3 Location, Vector3 Scale, Vector3 Bound)
    {
        public int MapID { get; set; } = MapID;
        public int LinkToMapID { get; set; } = LinkToMapID;
        public string MapName { get; set; } = MapName;
        public Vector3 Location { get; set; } = Location;
        public Vector3 Scale { get; set; } = Scale;
        public Vector3 Bound { get; set; } = Bound;
    }

}
