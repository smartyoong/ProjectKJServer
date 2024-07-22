using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreUtility.GlobalVariable
{
    public record Vector3
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }

    public record Obstacle(Vector3 Location, Vector3 Scale, Vector3 MeshSize, string MeshName)
    {
        public Vector3 Location { get; set; } = Location;
        public Vector3 Scale { get; set; } = Scale;
        public Vector3 MeshSize { get; set; } = MeshSize;
        public string MeshName { get; set; } = MeshName;
    }

    public record MapDataForResourceLoader(int MapID, string MapName, List<Obstacle> Obstacles)
    {
        public int MapID { get; set; } = MapID;
        public string MapName { get; set; } = MapName;
        public List<Obstacle> Obstacles { get; set; } = Obstacles;
    }
    public record MapData(int MapID, string MapName, List<Obstacle> Obstacles, List<MapPortalData> Portals, float MapBoundX, float MapBoundY, float MapBoundZ)
    {
        public int MapID { get; set; } = MapID;
        public string MapName { get; set; } = MapName;
        public List<Obstacle> Obstacles { get; set; } = Obstacles;
        public List<MapPortalData> Portals { get; set; } = Portals;
        public float MapBoundX { get; set; } = MapBoundX;
        public float MapBoundY { get; set; } = MapBoundY;
        public float MapBoundZ { get; set; } = MapBoundZ;
    }

    public record Portal(Vector3 Location, Vector3 Scale, Vector3 BoxSize, int LinkMapID)
    {
        public Vector3 Location { get; init; } = Location;
        public Vector3 Scale { get; init; } = Scale;
        public Vector3 BoxSize { get; init; } = BoxSize;
        public int LinkMapID { get; init; } = LinkMapID;
    }

    public record MapPortalData(int MapID, string MapName, List<Portal> Portals)
    {
        public int MapID { get; set; } = MapID;
        public string MapName { get; set; } = MapName;
        public List<Portal> Portals { get; set; } = Portals;
    }

    public record CharacterPresetData(int PresetID, string PrestName, int Gender, string BlueprintName, string PlayerCharacterName)
    {
        public int PresetID { get; set; } = PresetID;
        public string PresetName { get; set; } = PrestName;
        public int Gender { get; set; } = Gender;
        public string BlueprintName { get; set; } = BlueprintName;
        public string PlayerCharacterName { get; set; } = PlayerCharacterName;
    }

}
