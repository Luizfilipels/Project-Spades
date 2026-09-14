using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class LegacyMapNativeConverter
{
    private const string NativeMapsFolder =
        "Maps";

    private const string TerrainFileName =
        "terrain.vxm";

    private const string MetadataFileName =
        "map.json";

    // ============================================================
    // PYSNIP
    // ============================================================

    public static string ConvertPySnipMap(
        LegacyMapEntry source)
    {
        ValidateSourceVxl(
            source
        );

        if (source.HasUgc)
        {
            throw new InvalidOperationException(
                "O mapa possui UGC e foi " +
                "identificado como Jagex/Retail."
            );
        }

        if (!source.HasTxt)
        {
            throw new InvalidOperationException(
                "Conversão PySnip requer " +
                "VXL + TXT."
            );
        }

        PySnipMapScriptData scriptData =
            PySnipMapScriptImporter.Load(
                source.TxtPath
            );

        VoxelMapData terrain =
            VxlReader.Load(
                source.VxlPath
            );

        MapGameplayMetadata gameplay =
            PySnipGameplayConverter.Convert(
                scriptData
            );

        string mapName =
            scriptData.Name;

        if (string.IsNullOrWhiteSpace(
                mapName))
        {
            mapName =
                source.Name;
        }

        return WriteNativeMap(
            mapName,
            scriptData.Author,
            scriptData.Description,
            terrain,
            gameplay
        );
    }

    // ============================================================
    // JAGEX / RETAIL
    // ============================================================

    public static string ConvertJagexMap(
        LegacyMapEntry source)
    {
        ValidateSourceVxl(
            source
        );

        if (!source.HasUgc)
        {
            throw new InvalidOperationException(
                "Conversão Jagex requer " +
                "VXL + UGC."
            );
        }

        // --------------------------------------------------------
        // 1. UGC
        // --------------------------------------------------------

        JagexUgcData ugc =
            JagexUgcImporter.Load(
                source.UgcPath
            );

        // --------------------------------------------------------
        // 2. VXL RETAIL
        // --------------------------------------------------------

        VoxelMapData terrain =
            VxlReader.Load(
                source.VxlPath
            );

        // --------------------------------------------------------
        // 3. UGC -> INTERMEDIATE GAMEPLAY
        // --------------------------------------------------------

        JagexGameplayData jagexGameplay =
            JagexGameplayConverter.Convert(
                ugc,
                source.VxlPath
            );

        // --------------------------------------------------------
        // 4. INTERMEDIATE -> NATIVE GAMEPLAY
        // --------------------------------------------------------

        MapGameplayMetadata gameplay =
            JagexNativeGameplayConverter.Convert(
                jagexGameplay
            );

        // --------------------------------------------------------
        // 5. MAP METADATA
        // --------------------------------------------------------

        string mapName =
            ugc.title;

        if (string.IsNullOrWhiteSpace(
                mapName))
        {
            mapName =
                source.Name;
        }

        string mapAuthor =
            ugc.author;

        string mapDescription =
            ugc.description;

        // --------------------------------------------------------
        // 6. WRITE
        // --------------------------------------------------------

        return WriteNativeMap(
            mapName,
            mapAuthor,
            mapDescription,
            terrain,
            gameplay
        );
    }

    // ============================================================
    // WRITE NATIVE MAP
    // ============================================================

    private static string WriteNativeMap(
        string mapName,
        string author,
        string description,
        VoxelMapData terrain,
        MapGameplayMetadata gameplay)
    {
        if (terrain == null)
        {
            throw new ArgumentNullException(
                nameof(terrain)
            );
        }

        if (gameplay == null)
        {
            throw new ArgumentNullException(
                nameof(gameplay)
            );
        }

        if (string.IsNullOrWhiteSpace(
                mapName))
        {
            mapName =
                "Imported Map";
        }

        string folderName =
            SanitizeFolderName(
                mapName
            );

        string mapsDirectory =
            Path.Combine(
                Application.persistentDataPath,
                NativeMapsFolder
            );

        Directory.CreateDirectory(
            mapsDirectory
        );

        string mapDirectory =
            Path.Combine(
                mapsDirectory,
                folderName
            );

        Directory.CreateDirectory(
            mapDirectory
        );

        string terrainPath =
            Path.Combine(
                mapDirectory,
                TerrainFileName
            );

        string metadataPath =
            Path.Combine(
                mapDirectory,
                MetadataFileName
            );

        string nowUtc =
            DateTime.UtcNow.ToString(
                "o"
            );

        string createdUtc =
            GetExistingCreatedUtc(
                metadataPath
            );

        if (string.IsNullOrWhiteSpace(
                createdUtc))
        {
            createdUtc =
                nowUtc;
        }

        MapMetadata metadata =
            new MapMetadata();

        metadata.formatVersion =
            1;

        metadata.name =
            mapName;

        metadata.author =
            author ?? string.Empty;

        metadata.description =
            description ?? string.Empty;

        metadata.terrainFile =
            TerrainFileName;

        metadata.sizeX =
            terrain.SizeX;

        metadata.sizeY =
            terrain.SizeY;

        metadata.sizeZ =
            terrain.SizeZ;

        metadata.createdUtc =
            createdUtc;

        metadata.modifiedUtc =
            nowUtc;

        metadata.gameplay =
            gameplay;

        ValidateMetadata(
            metadata,
            terrain
        );

        VxmSerializer.Save(
            terrainPath,
            terrain
        );

        string json =
            JsonUtility.ToJson(
                metadata,
                true
            );

        File.WriteAllText(
            metadataPath,
            json
        );

        return mapDirectory;
    }

    // ============================================================
    // VALIDATION
    // ============================================================

    private static void ValidateSourceVxl(
        LegacyMapEntry source)
    {
        if (source == null)
        {
            throw new ArgumentNullException(
                nameof(source)
            );
        }

        if (string.IsNullOrWhiteSpace(
                source.VxlPath))
        {
            throw new InvalidOperationException(
                "Caminho VXL vazio."
            );
        }

        if (!File.Exists(
                source.VxlPath))
        {
            throw new FileNotFoundException(
                "Arquivo VXL não encontrado.",
                source.VxlPath
            );
        }
    }

    private static void ValidateMetadata(
        MapMetadata metadata,
        VoxelMapData terrain)
    {
        if (metadata == null)
        {
            throw new InvalidDataException(
                "Metadata nativo nulo."
            );
        }

        if (metadata.sizeX !=
                terrain.SizeX ||
            metadata.sizeY !=
                terrain.SizeY ||
            metadata.sizeZ !=
                terrain.SizeZ)
        {
            throw new InvalidDataException(
                "Dimensões do metadata não " +
                "correspondem ao terreno."
            );
        }

        ValidateGameplay(
            metadata.gameplay,
            terrain
        );
    }

    private static void ValidateGameplay(
        MapGameplayMetadata gameplay,
        VoxelMapData terrain)
    {
        if (gameplay == null)
        {
            throw new InvalidDataException(
                "Gameplay metadata nulo."
            );
        }

        ValidateTeams(
            gameplay.teams,
            terrain
        );

        ValidateModes(
            gameplay.modes,
            terrain
        );

        ValidatePointEntities(
            gameplay.commonEntities,
            terrain,
            "common"
        );
    }

    // ============================================================
    // TEAM VALIDATION
    // ============================================================

    private static void ValidateTeams(
        MapTeamMetadata[] teams,
        VoxelMapData terrain)
    {
        if (teams == null)
        {
            throw new InvalidDataException(
                "Lista de times nula."
            );
        }

        HashSet<string> ids =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase
            );

        foreach (MapTeamMetadata team
                 in teams)
        {
            if (team == null)
            {
                throw new InvalidDataException(
                    "Time nulo."
                );
            }

            if (string.IsNullOrWhiteSpace(
                    team.id))
            {
                throw new InvalidDataException(
                    "Time sem ID."
                );
            }

            if (!ids.Add(
                    team.id))
            {
                throw new InvalidDataException(
                    "Time duplicado: " +
                    team.id
                );
            }

            if (team.spawnPolicy ==
                MapLocationPolicy.MapDefined)
            {
                if (team.spawnPoints == null ||
                    team.spawnPoints.Length == 0)
                {
                    throw new InvalidDataException(
                        "Time " +
                        team.id +
                        " usa spawn MapDefined " +
                        "sem spawnPoints."
                    );
                }

                foreach (MapCoordinate spawn
                         in team.spawnPoints)
                {
                    ValidateCoordinate(
                        spawn,
                        terrain,
                        team.id +
                        " spawn"
                    );
                }
            }

            if (team.basePolicy ==
                    MapLocationPolicy.MapDefined &&
                team.hasBase)
            {
                ValidateCoordinate(
                    team.basePosition,
                    terrain,
                    team.id +
                    " base"
                );
            }

            if (team.flagPolicy ==
                    MapLocationPolicy.MapDefined &&
                team.hasFlag)
            {
                ValidateCoordinate(
                    team.flagPosition,
                    terrain,
                    team.id +
                    " flag"
                );
            }
        }
    }

    // ============================================================
    // MODE VALIDATION
    // ============================================================

    private static void ValidateModes(
        MapGameModeMetadata[] modes,
        VoxelMapData terrain)
    {
        if (modes == null)
        {
            return;
        }

        HashSet<string> ids =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase
            );

        foreach (MapGameModeMetadata mode
                 in modes)
        {
            if (mode == null)
            {
                throw new InvalidDataException(
                    "Modo nulo."
                );
            }

            if (string.IsNullOrWhiteSpace(
                    mode.id))
            {
                throw new InvalidDataException(
                    "Modo sem ID."
                );
            }

            if (!ids.Add(
                    mode.id))
            {
                throw new InvalidDataException(
                    "Modo duplicado: " +
                    mode.id
                );
            }

            ValidateZones(
                mode.zones,
                terrain,
                mode.id
            );

            ValidatePointEntities(
                mode.entities,
                terrain,
                mode.id
            );
        }
    }

    private static void ValidateZones(
        MapZoneMetadata[] zones,
        VoxelMapData terrain,
        string mode)
    {
        if (zones == null)
        {
            return;
        }

        foreach (MapZoneMetadata zone
                 in zones)
        {
            if (zone == null)
            {
                throw new InvalidDataException(
                    "Zona nula no modo " +
                    mode
                );
            }

            ValidateCoordinate(
                zone.center,
                terrain,
                mode +
                " zone " +
                zone.sourceItem
            );
        }
    }

    private static void ValidatePointEntities(
        MapPointEntityMetadata[] entities,
        VoxelMapData terrain,
        string context)
    {
        if (entities == null)
        {
            return;
        }

        foreach (MapPointEntityMetadata entity
                 in entities)
        {
            if (entity == null)
            {
                throw new InvalidDataException(
                    "Point entity nula em " +
                    context
                );
            }

            ValidateCoordinate(
                entity.position,
                terrain,
                context +
                " entity " +
                entity.sourceItem
            );
        }
    }

    private static void ValidateCoordinate(
        MapCoordinate coordinate,
        VoxelMapData terrain,
        string description)
    {
        if (coordinate.x < 0 ||
            coordinate.x >= terrain.SizeX ||
            coordinate.y < 0 ||
            coordinate.y >= terrain.SizeY ||
            coordinate.z < 0 ||
            coordinate.z >= terrain.SizeZ)
        {
            throw new InvalidDataException(
                description +
                " fora dos limites: (" +
                coordinate.x +
                ", " +
                coordinate.y +
                ", " +
                coordinate.z +
                ")."
            );
        }
    }

    // ============================================================
    // EXISTING METADATA
    // ============================================================

    private static string GetExistingCreatedUtc(
        string metadataPath)
    {
        if (!File.Exists(
                metadataPath))
        {
            return string.Empty;
        }

        try
        {
            string json =
                File.ReadAllText(
                    metadataPath
                );

            MapMetadata existing =
                JsonUtility.FromJson<MapMetadata>(
                    json
                );

            if (existing == null)
            {
                return string.Empty;
            }

            return existing.createdUtc;
        }
        catch
        {
            return string.Empty;
        }
    }

    // ============================================================
    // FOLDER
    // ============================================================

    private static string SanitizeFolderName(
        string value)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            return "Imported Map";
        }

        char[] invalid =
            Path.GetInvalidFileNameChars();

        char[] chars =
            value.Trim().ToCharArray();

        for (int i = 0;
             i < chars.Length;
             i++)
        {
            bool replace =
                chars[i] == '/' ||
                chars[i] == '\\';

            if (!replace)
            {
                for (int j = 0;
                     j < invalid.Length;
                     j++)
                {
                    if (chars[i] ==
                        invalid[j])
                    {
                        replace =
                            true;

                        break;
                    }
                }
            }

            if (replace)
            {
                chars[i] =
                    '_';
            }
        }

        string result =
            new string(chars).Trim();

        if (string.IsNullOrWhiteSpace(
                result))
        {
            return "Imported Map";
        }

        return result;
    }
}