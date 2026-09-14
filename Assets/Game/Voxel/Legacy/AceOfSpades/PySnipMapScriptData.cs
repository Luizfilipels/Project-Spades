using System;
using System.Collections.Generic;

public sealed class PySnipMapScriptData
{
    public string Name { get; set; } =
        string.Empty;

    public string Version { get; set; } =
        string.Empty;

    public string Author { get; set; } =
        string.Empty;

    public string Description { get; set; } =
        string.Empty;

    public Dictionary<string, string>
        Extensions { get; } =
            new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase
            );

    public List<MapCoordinate>
        BlueSpawns { get; } =
            new List<MapCoordinate>();

    public List<MapCoordinate>
        GreenSpawns { get; } =
            new List<MapCoordinate>();

    public bool HasBlueBase { get; set; }

    public MapCoordinate BlueBase { get; set; }

    public bool HasGreenBase { get; set; }

    public MapCoordinate GreenBase { get; set; }

    public bool HasBlueFlag { get; set; }

    public MapCoordinate BlueFlag { get; set; }

    public bool HasGreenFlag { get; set; }

    public MapCoordinate GreenFlag { get; set; }

    // ------------------------------------------------------------
    // LOCATION POLICIES
    // ------------------------------------------------------------

    public bool HasCustomBlueSpawns =>
        BlueSpawns.Count > 0;

    public bool HasCustomGreenSpawns =>
        GreenSpawns.Count > 0;

    public bool UsesDefaultBlueSpawnPolicy =>
        !HasCustomBlueSpawns;

    public bool UsesDefaultGreenSpawnPolicy =>
        !HasCustomGreenSpawns;

    public bool UsesDefaultBlueBasePolicy =>
        !HasBlueBase;

    public bool UsesDefaultGreenBasePolicy =>
        !HasGreenBase;

    public bool UsesDefaultBlueFlagPolicy =>
        !HasBlueFlag;

    public bool UsesDefaultGreenFlagPolicy =>
        !HasGreenFlag;
}