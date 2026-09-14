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

    public static string ConvertPySnipMap(
        LegacyMapEntry source)
    {
        if (source == null)
        {
            throw new ArgumentNullException(
                nameof(source)
            );
        }

        if (string.IsNullOrWhiteSpace(
                source.VxlPath) ||
            !File.Exists(source.VxlPath))
        {
            throw new FileNotFoundException(
                "Arquivo VXL não encontrado.",
                source.VxlPath
            );
        }

        /*
         * Importante:
         *
         * um mapa que possui .ugc pertence ao
         * pipeline Jagex/Retail.
         *
         * Alguns mapas Jagex também possuem
         * arquivos .txt, mas esses arquivos não
         * devem automaticamente ser tratados
         * como scripts PySnip.
         */
        if (source.HasUgc)
        {
            throw new InvalidOperationException(
                "Este mapa possui arquivo UGC e " +
                "foi identificado como Jagex/Retail. " +
                "A conversão Jagex será implementada " +
                "no próximo estágio."
            );
        }

        if (!source.HasTxt)
        {
            throw new InvalidOperationException(
                "Este mapa não possui TXT PySnip. " +
                "A conversão PySnip requer VXL + TXT."
            );
        }

        // ========================================================
        // 1. LER O SCRIPT PYSNIP
        // ========================================================

        PySnipMapScriptData scriptData =
            PySnipMapScriptImporter.Load(
                source.TxtPath
            );

        // ========================================================
        // 2. LER O TERRENO VXL
        // ========================================================

        VoxelMapData terrain =
            VxlReader.Load(
                source.VxlPath
            );

        // ========================================================
        // 3. CONVERTER GAMEPLAY
        // ========================================================

        MapGameplayMetadata gameplay =
            PySnipGameplayConverter.Convert(
                scriptData
            );

        // ========================================================
        // 4. DETERMINAR NOME DO MAPA
        // ========================================================

        string mapName =
            scriptData.Name;

        if (string.IsNullOrWhiteSpace(
                mapName))
        {
            mapName =
                source.Name;
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

        // ========================================================
        // 5. CRIAR DIRETÓRIO NATIVO
        // ========================================================

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

        // ========================================================
        // 6. DATAS
        // ========================================================

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

        // ========================================================
        // 7. CRIAR METADATA NATIVO
        // ========================================================

        MapMetadata metadata =
            new MapMetadata();

        metadata.formatVersion =
            1;

        metadata.name =
            mapName;

        metadata.author =
            scriptData.Author;

        metadata.description =
            scriptData.Description;

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

        // ========================================================
        // 8. VALIDAR ANTES DE GRAVAR
        // ========================================================

        ValidateMetadata(
            metadata,
            terrain
        );

        // ========================================================
        // 9. GRAVAR TERRAIN.VXM
        // ========================================================

        VxmSerializer.Save(
            terrainPath,
            terrain
        );

        // ========================================================
        // 10. GRAVAR MAP.JSON
        // ========================================================

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

        if (terrain == null)
        {
            throw new InvalidDataException(
                "Terrain nulo."
            );
        }

        if (string.IsNullOrWhiteSpace(
                metadata.name))
        {
            throw new InvalidDataException(
                "Nome do mapa vazio."
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
                "As dimensões do metadata " +
                "não correspondem ao terreno."
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

        if (gameplay.teams == null)
        {
            throw new InvalidDataException(
                "Lista de times nula."
            );
        }

        HashSet<string> teamIds =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase
            );

        foreach (MapTeamMetadata team
                 in gameplay.teams)
        {
            if (team == null)
            {
                throw new InvalidDataException(
                    "Time nulo encontrado."
                );
            }

            if (string.IsNullOrWhiteSpace(
                    team.id))
            {
                throw new InvalidDataException(
                    "Time sem ID."
                );
            }

            if (!teamIds.Add(
                    team.id))
            {
                throw new InvalidDataException(
                    "ID de time duplicado: " +
                    team.id
                );
            }

            ValidateSpawnPolicy(
                team,
                terrain
            );

            ValidateBasePolicy(
                team,
                terrain
            );

            ValidateFlagPolicy(
                team,
                terrain
            );
        }
    }

    private static void ValidateSpawnPolicy(
        MapTeamMetadata team,
        VoxelMapData terrain)
    {
        if (team.spawnPolicy ==
            MapLocationPolicy.MapDefined)
        {
            if (team.spawnPoints == null ||
                team.spawnPoints.Length == 0)
            {
                throw new InvalidDataException(
                    "O time " +
                    team.id +
                    " usa MapDefined para spawn, " +
                    "mas não possui spawnPoints."
                );
            }

            foreach (MapCoordinate coordinate
                     in team.spawnPoints)
            {
                ValidateCoordinate(
                    coordinate,
                    terrain,
                    team.id +
                    " spawn"
                );
            }

            return;
        }

        if (team.spawnPolicy ==
            MapLocationPolicy.ServerDefault)
        {
            /*
             * Nenhuma coordenada é necessária.
             * O servidor escolherá futuramente.
             */
            return;
        }

        if (team.spawnPolicy ==
            MapLocationPolicy.Disabled)
        {
            return;
        }

        throw new InvalidDataException(
            "Spawn policy desconhecida."
        );
    }

    private static void ValidateBasePolicy(
        MapTeamMetadata team,
        VoxelMapData terrain)
    {
        if (team.basePolicy ==
            MapLocationPolicy.MapDefined)
        {
            if (!team.hasBase)
            {
                throw new InvalidDataException(
                    "O time " +
                    team.id +
                    " possui base MapDefined, " +
                    "mas hasBase está false."
                );
            }

            ValidateCoordinate(
                team.basePosition,
                terrain,
                team.id +
                " base"
            );

            return;
        }

        if (team.basePolicy ==
            MapLocationPolicy.ServerDefault)
        {
            return;
        }

        if (team.basePolicy ==
            MapLocationPolicy.Disabled)
        {
            return;
        }

        throw new InvalidDataException(
            "Base policy desconhecida."
        );
    }

    private static void ValidateFlagPolicy(
        MapTeamMetadata team,
        VoxelMapData terrain)
    {
        if (team.flagPolicy ==
            MapLocationPolicy.MapDefined)
        {
            if (!team.hasFlag)
            {
                throw new InvalidDataException(
                    "O time " +
                    team.id +
                    " possui flag MapDefined, " +
                    "mas hasFlag está false."
                );
            }

            ValidateCoordinate(
                team.flagPosition,
                terrain,
                team.id +
                " flag"
            );

            return;
        }

        if (team.flagPolicy ==
            MapLocationPolicy.ServerDefault)
        {
            return;
        }

        if (team.flagPolicy ==
            MapLocationPolicy.Disabled)
        {
            return;
        }

        throw new InvalidDataException(
            "Flag policy desconhecida."
        );
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
                " possui coordenada fora " +
                "dos limites: (" +
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
                JsonUtility.FromJson<
                    MapMetadata>(json);

            if (existing == null)
            {
                return string.Empty;
            }

            return existing.createdUtc;
        }
        catch
        {
            /*
             * Um metadata antigo inválido não deve
             * impedir uma nova conversão.
             */
            return string.Empty;
        }
    }

    // ============================================================
    // FOLDER NAME
    // ============================================================

    private static string SanitizeFolderName(
        string value)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            return "Imported Map";
        }

        char[] invalidCharacters =
            Path.GetInvalidFileNameChars();

        char[] characters =
            value.Trim().ToCharArray();

        for (int i = 0;
             i < characters.Length;
             i++)
        {
            char character =
                characters[i];

            bool invalid =
                character == '/' ||
                character == '\\';

            if (!invalid)
            {
                for (int j = 0;
                     j < invalidCharacters.Length;
                     j++)
                {
                    if (character ==
                        invalidCharacters[j])
                    {
                        invalid =
                            true;

                        break;
                    }
                }
            }

            if (invalid)
            {
                characters[i] =
                    '_';
            }
        }

        string result =
            new string(
                characters
            ).Trim();

        if (string.IsNullOrWhiteSpace(
                result))
        {
            result =
                "Imported Map";
        }

        return result;
    }
}