using System;
using System.Collections.Generic;

public static class JagexNativeGameplayConverter
{
    private const string CommonMode =
        "nor";

    public static MapGameplayMetadata Convert(
        JagexGameplayData source)
    {
        if (source == null)
        {
            throw new ArgumentNullException(
                nameof(source)
            );
        }

        MapGameplayMetadata gameplay =
            new MapGameplayMetadata();

        gameplay.supportedGameModes =
            CopySupportedModes(
                source.SupportedGameModes
            );

        gameplay.defaultGameMode =
            SelectDefaultGameMode(
                gameplay.supportedGameModes
            );

        /*
         * Jagex utiliza zonas específicas
         * por modo.
         *
         * Não tentamos reduzir isso para os
         * campos simples do PySnip.
         */
        gameplay.teams =
            CreateCompatibilityTeams();

        gameplay.modes =
            CreateModes(
                source
            );

        gameplay.commonEntities =
            CreateCommonEntities(
                source
            );

        return gameplay;
    }

    // ============================================================
    // DEFAULT MODE
    // ============================================================

    private static string SelectDefaultGameMode(
        string[] supportedModes)
    {
        if (supportedModes == null ||
            supportedModes.Length == 0)
        {
            return "tdm";
        }

        /*
         * Se houver CTF, preferimos CTF
         * como default temporário.
         *
         * Depois poderemos preservar um
         * default explicitamente definido
         * pelo UGC, caso seja recuperado.
         */
        foreach (string mode
                 in supportedModes)
        {
            if (string.Equals(
                    mode,
                    "ctf",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "ctf";
            }
        }

        return supportedModes[0];
    }

    private static string[] CopySupportedModes(
        string[] source)
    {
        if (source == null ||
            source.Length == 0)
        {
            return Array.Empty<string>();
        }

        string[] result =
            new string[source.Length];

        Array.Copy(
            source,
            result,
            source.Length
        );

        return result;
    }

    // ============================================================
    // COMPATIBILITY TEAMS
    // ============================================================

    private static MapTeamMetadata[]
        CreateCompatibilityTeams()
    {
        /*
         * Esses times existem para manter
         * compatibilidade com sistemas antigos
         * que ainda esperam gameplay.teams.
         *
         * As posições reais de mapas Jagex
         * ficam em gameplay.modes[].zones.
         */

        MapTeamMetadata blue =
            new MapTeamMetadata();

        blue.id =
            "blue";

        blue.displayName =
            "Blue";

        blue.spawnPolicy =
            MapLocationPolicy.Disabled;

        blue.spawnPoints =
            Array.Empty<MapCoordinate>();

        blue.basePolicy =
            MapLocationPolicy.Disabled;

        blue.hasBase =
            false;

        blue.flagPolicy =
            MapLocationPolicy.Disabled;

        blue.hasFlag =
            false;

        MapTeamMetadata green =
            new MapTeamMetadata();

        green.id =
            "green";

        green.displayName =
            "Green";

        green.spawnPolicy =
            MapLocationPolicy.Disabled;

        green.spawnPoints =
            Array.Empty<MapCoordinate>();

        green.basePolicy =
            MapLocationPolicy.Disabled;

        green.hasBase =
            false;

        green.flagPolicy =
            MapLocationPolicy.Disabled;

        green.hasFlag =
            false;

        return new MapTeamMetadata[]
        {
            blue,
            green
        };
    }

    // ============================================================
    // MODES
    // ============================================================

    private static MapGameModeMetadata[] CreateModes(
        JagexGameplayData source)
    {
        if (source.SupportedGameModes == null ||
            source.SupportedGameModes.Length == 0)
        {
            return Array.Empty<MapGameModeMetadata>();
        }

        List<MapGameModeMetadata> result =
            new List<MapGameModeMetadata>();

        foreach (string mode
                 in source.SupportedGameModes)
        {
            if (string.IsNullOrWhiteSpace(
                    mode))
            {
                continue;
            }

            string normalizedMode =
                mode
                    .Trim()
                    .ToLowerInvariant();

            if (normalizedMode ==
                CommonMode)
            {
                continue;
            }

            MapGameModeMetadata nativeMode =
                new MapGameModeMetadata();

            nativeMode.id =
                normalizedMode;

            nativeMode.zones =
                ConvertZonesForMode(
                    source,
                    normalizedMode
                );

            nativeMode.entities =
                ConvertPointsForMode(
                    source,
                    normalizedMode
                );

            result.Add(
                nativeMode
            );
        }

        return result.ToArray();
    }

    // ============================================================
    // ZONES
    // ============================================================

    private static MapZoneMetadata[]
        ConvertZonesForMode(
            JagexGameplayData source,
            string mode)
    {
        List<MapZoneMetadata> result =
            new List<MapZoneMetadata>();

        foreach (JagexGameplayZone zone
                 in source.Zones)
        {
            if (zone == null)
            {
                continue;
            }

            if (!string.Equals(
                    zone.mode,
                    mode,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            MapZoneMetadata nativeZone =
                new MapZoneMetadata();

            nativeZone.kind =
                ConvertZoneKind(
                    zone.kind
                );

            nativeZone.team =
                ConvertTeam(
                    zone.team
                );

            nativeZone.size =
                ConvertZoneSize(
                    zone.size
                );

            nativeZone.sourceItem =
                zone.item;

            nativeZone.center =
                zone.center;

            result.Add(
                nativeZone
            );
        }

        return result.ToArray();
    }

    private static string ConvertZoneKind(
        JagexZoneKind kind)
    {
        switch (kind)
        {
            case JagexZoneKind.Spawn:
                return "spawn";

            case JagexZoneKind.Base:
                return "base";

            default:
                return "unknown";
        }
    }

    private static string ConvertTeam(
        JagexTeam team)
    {
        switch (team)
        {
            case JagexTeam.Blue:
                return "blue";

            case JagexTeam.Green:
                return "green";

            default:
                return "neutral";
        }
    }

    private static string ConvertZoneSize(
        JagexZoneSize size)
    {
        switch (size)
        {
            case JagexZoneSize.Small:
                return "small";

            case JagexZoneSize.Medium:
                return "medium";

            case JagexZoneSize.Large:
                return "large";

            default:
                return "unknown";
        }
    }

    // ============================================================
    // MODE-SPECIFIC POINT ENTITIES
    // ============================================================

    private static MapPointEntityMetadata[]
        ConvertPointsForMode(
            JagexGameplayData source,
            string mode)
    {
        List<MapPointEntityMetadata> result =
            new List<MapPointEntityMetadata>();

        foreach (JagexGameplayPointEntity point
                 in source.PointEntities)
        {
            if (point == null)
            {
                continue;
            }

            if (!string.Equals(
                    point.mode,
                    mode,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            result.Add(
                ConvertPoint(
                    point
                )
            );
        }

        return result.ToArray();
    }

    // ============================================================
    // COMMON ENTITIES
    // ============================================================

    private static MapPointEntityMetadata[]
        CreateCommonEntities(
            JagexGameplayData source)
    {
        List<MapPointEntityMetadata> result =
            new List<MapPointEntityMetadata>();

        foreach (JagexGameplayPointEntity point
                 in source.PointEntities)
        {
            if (point == null)
            {
                continue;
            }

            if (!string.Equals(
                    point.mode,
                    CommonMode,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            result.Add(
                ConvertPoint(
                    point
                )
            );
        }

        return result.ToArray();
    }

    private static MapPointEntityMetadata ConvertPoint(
        JagexGameplayPointEntity point)
    {
        MapPointEntityMetadata result =
            new MapPointEntityMetadata();

        result.kind =
            point.kind;

        result.sourceItem =
            point.item;

        result.position =
            point.position;

        return result;
    }
}