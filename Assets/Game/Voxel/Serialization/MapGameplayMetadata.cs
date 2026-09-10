using System;

[Serializable]
public class MapGameplayMetadata
{
    public string defaultGameMode =
        "ctf";

    public string[] supportedGameModes =
    {
        "ctf"
    };

    public MapTeamMetadata[] teams =
        Array.Empty<MapTeamMetadata>();
}