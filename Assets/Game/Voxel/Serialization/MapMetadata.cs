using System;

[Serializable]
public class MapMetadata
{
    public int formatVersion = 1;

    public string name;
    public string author;
    public string description;

    public string terrainFile =
        "terrain.vxm";

    public int sizeX;
    public int sizeY;
    public int sizeZ;

    public string createdUtc;
    public string modifiedUtc;

    public MapGameplayMetadata gameplay =
        new MapGameplayMetadata();
}