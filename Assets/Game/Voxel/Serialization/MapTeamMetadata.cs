using System;

[Serializable]
public class MapTeamMetadata
{
    public string id;

    public string displayName;

    public MapCoordinate[] spawnPoints =
        Array.Empty<MapCoordinate>();

    public bool hasBase;

    public MapCoordinate basePosition;

    public bool hasFlag;

    public MapCoordinate flagPosition;
}