using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

public class VoxelMapManager : MonoBehaviour
{
    [Header("World")]
    [SerializeField]
    private VoxelWorld world;

    [Header("Map")]
    [SerializeField]
    private string mapFolderName =
        "TestMap";

    [SerializeField]
    private string mapName =
        "Test Map";

    [SerializeField]
    private string author =
        "Project Spades";

    [SerializeField]
    [TextArea]
    private string description =
        "Project Spades test map.";

    [Header("Gameplay")]
    [SerializeField]
    private string defaultGameMode =
        "ctf";

    [SerializeField]
    private string[] supportedGameModes =
    {
        "ctf"
    };

    [SerializeField]
    private MapTeamMetadata[] teams =
        Array.Empty<MapTeamMetadata>();

    [Header("Debug")]
    [SerializeField]
    private bool enableDebugHotkeys =
        true;

    private const string MapsFolder =
        "Maps";

    private const string MetadataFileName =
        "map.json";

    private const string DefaultTerrainFile =
        "terrain.vxm";

    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        if (world == null)
        {
            world =
                FindAnyObjectByType<VoxelWorld>();
        }
    }

    private void Update()
    {
        if (!enableDebugHotkeys ||
            Keyboard.current == null)
        {
            return;
        }

        /*
         * Mantemos os atalhos antigos.
         */
        if (Keyboard.current.f5Key
            .wasPressedThisFrame)
        {
            Debug.Log(
                "VoxelMapManager: F5 detectado."
            );

            SaveCurrentMap();
        }

        if (Keyboard.current.f9Key
            .wasPressedThisFrame)
        {
            Debug.Log(
                "VoxelMapManager: F9 detectado."
            );

            LoadCurrentMap();
        }

        /*
         * Atalhos novos usando teclado numérico.
         *
         * NumPad 1 = Load
         * NumPad 2 = Save
         */
        if (Keyboard.current.numpad1Key
            .wasPressedThisFrame)
        {
            Debug.Log(
                "VoxelMapManager: NumPad 1 detectado."
            );

            LoadCurrentMap();
        }

        if (Keyboard.current.numpad2Key
            .wasPressedThisFrame)
        {
            Debug.Log(
                "VoxelMapManager: NumPad 2 detectado."
            );

            SaveCurrentMap();
        }
    }

    // ============================================================
    // PATHS
    // ============================================================

    public static string GetMapsDirectory()
    {
        return Path.Combine(
            Application.persistentDataPath,
            MapsFolder
        );
    }

    private string GetCurrentMapDirectory()
    {
        string safeFolderName =
            SanitizeFolderName(
                mapFolderName
            );

        return Path.Combine(
            GetMapsDirectory(),
            safeFolderName
        );
    }

    // ============================================================
    // SAVE
    // ============================================================

    public bool SaveCurrentMap()
    {
        if (world == null)
        {
            Debug.LogError(
                "VoxelMapManager: " +
                "VoxelWorld não definido."
            );

            return false;
        }

        try
        {
            Debug.Log(
                "================================\n" +
                "SALVANDO MAPA NATIVO\n" +
                "Folder: " +
                mapFolderName +
                "\n" +
                "================================"
            );

            string mapDirectory =
                GetCurrentMapDirectory();

            Directory.CreateDirectory(
                mapDirectory
            );

            string metadataPath =
                Path.Combine(
                    mapDirectory,
                    MetadataFileName
                );

            string terrainPath =
                Path.Combine(
                    mapDirectory,
                    DefaultTerrainFile
                );

            VoxelMapData mapData =
                world.CaptureMapData();

            string nowUtc =
                DateTime.UtcNow.ToString(
                    "o"
                );

            string createdUtc =
                ReadExistingCreationDate(
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
                author;

            metadata.description =
                description;

            metadata.terrainFile =
                DefaultTerrainFile;

            metadata.sizeX =
                mapData.SizeX;

            metadata.sizeY =
                mapData.SizeY;

            metadata.sizeZ =
                mapData.SizeZ;

            metadata.createdUtc =
                createdUtc;

            metadata.modifiedUtc =
                nowUtc;

            metadata.gameplay =
                CreateGameplayMetadata();

            ValidateMetadata(
                metadata,
                mapData
            );

            VxmSerializer.Save(
                terrainPath,
                mapData
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

            Debug.Log(
                "Mapa nativo salvo com sucesso.\n" +
                "Destino: " +
                mapDirectory
            );

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "Erro ao salvar mapa nativo:\n" +
                exception
            );

            return false;
        }
    }

    private MapGameplayMetadata
        CreateGameplayMetadata()
    {
        MapGameplayMetadata gameplay =
            new MapGameplayMetadata();

        gameplay.defaultGameMode =
            defaultGameMode;

        gameplay.supportedGameModes =
            supportedGameModes != null
                ? supportedGameModes
                : Array.Empty<string>();

        gameplay.teams =
            teams != null
                ? teams
                : Array.Empty<MapTeamMetadata>();

        return gameplay;
    }

    // ============================================================
    // LOAD
    // ============================================================

    public bool LoadCurrentMap()
    {
        if (world == null)
        {
            Debug.LogError(
                "VoxelMapManager: " +
                "VoxelWorld não definido."
            );

            return false;
        }

        try
        {
            string mapDirectory =
                GetCurrentMapDirectory();

            string metadataPath =
                Path.Combine(
                    mapDirectory,
                    MetadataFileName
                );

            Debug.Log(
                "================================\n" +
                "CARREGANDO MAPA NATIVO\n" +
                "Folder solicitado: " +
                mapFolderName +
                "\n" +
                "Diretório: " +
                mapDirectory +
                "\n" +
                "================================"
            );

            if (!Directory.Exists(
                    mapDirectory))
            {
                throw new DirectoryNotFoundException(
                    "Pasta do mapa não encontrada:\n" +
                    mapDirectory
                );
            }

            if (!File.Exists(
                    metadataPath))
            {
                throw new FileNotFoundException(
                    "map.json não encontrado.",
                    metadataPath
                );
            }

            string json =
                File.ReadAllText(
                    metadataPath
                );

            MapMetadata metadata =
                JsonUtility.FromJson<MapMetadata>(
                    json
                );

            if (metadata == null)
            {
                throw new InvalidDataException(
                    "Não foi possível interpretar " +
                    "o map.json."
                );
            }

            UpgradeMissingMetadata(
                metadata
            );

            ValidateTerrainFileName(
                metadata.terrainFile
            );

            string terrainPath =
                Path.Combine(
                    mapDirectory,
                    metadata.terrainFile
                );

            if (!File.Exists(
                    terrainPath))
            {
                throw new FileNotFoundException(
                    "Arquivo terrain não encontrado.",
                    terrainPath
                );
            }

            Debug.Log(
                "map.json encontrado.\n" +
                "Mapa: " +
                metadata.name +
                "\n" +
                "Terrain: " +
                terrainPath
            );

            VoxelMapData mapData =
                VxmSerializer.Load(
                    terrainPath
                );

            ValidateMetadata(
                metadata,
                mapData
            );

            Debug.Log(
                "terrain.vxm carregado.\n" +
                "Dimensões: " +
                mapData.SizeX +
                " x " +
                mapData.SizeY +
                " x " +
                mapData.SizeZ
            );

            world.LoadMapData(
                mapData
            );

            ApplyLoadedMetadata(
                metadata
            );

            LogLoadedGameplay(
                metadata.gameplay
            );

            Debug.Log(
                "================================\n" +
                "MAPA NATIVO CARREGADO COM SUCESSO\n" +
                metadata.name +
                "\n" +
                "================================"
            );

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "Erro ao carregar mapa nativo:\n" +
                exception
            );

            return false;
        }
    }

    // ============================================================
    // APPLY METADATA
    // ============================================================

    private void ApplyLoadedMetadata(
        MapMetadata metadata)
    {
        mapName =
            metadata.name;

        author =
            metadata.author;

        description =
            metadata.description;

        if (metadata.gameplay == null)
        {
            return;
        }

        defaultGameMode =
            metadata.gameplay
                .defaultGameMode;

        supportedGameModes =
            metadata.gameplay
                .supportedGameModes != null
                ? metadata.gameplay
                    .supportedGameModes
                : Array.Empty<string>();

        teams =
            metadata.gameplay.teams != null
                ? metadata.gameplay.teams
                : Array.Empty<MapTeamMetadata>();
    }

    // ============================================================
    // GAMEPLAY LOG
    // ============================================================

    private static void LogLoadedGameplay(
        MapGameplayMetadata gameplay)
    {
        if (gameplay == null)
        {
            Debug.LogWarning(
                "Mapa carregado sem gameplay metadata."
            );

            return;
        }

        int teamCount =
            gameplay.teams != null
                ? gameplay.teams.Length
                : 0;

        Debug.Log(
            "Gameplay carregado:\n" +
            "Default Mode: " +
            gameplay.defaultGameMode +
            "\n" +
            "Times: " +
            teamCount
        );

        if (gameplay.teams == null)
        {
            return;
        }

        foreach (MapTeamMetadata team
                 in gameplay.teams)
        {
            if (team == null)
            {
                continue;
            }

            int spawnCount =
                team.spawnPoints != null
                    ? team.spawnPoints.Length
                    : 0;

            Debug.Log(
                "Time: " +
                team.displayName +
                "\n" +
                "Spawn Policy: " +
                team.spawnPolicy +
                "\n" +
                "Spawns: " +
                spawnCount +
                "\n" +
                "Base Policy: " +
                team.basePolicy +
                "\n" +
                "Flag Policy: " +
                team.flagPolicy
            );
        }
    }

    // ============================================================
    // METADATA UPGRADE
    // ============================================================

    private static void UpgradeMissingMetadata(
        MapMetadata metadata)
    {
        if (metadata.gameplay == null)
        {
            metadata.gameplay =
                new MapGameplayMetadata();
        }

        if (metadata.gameplay
                .supportedGameModes == null)
        {
            metadata.gameplay
                .supportedGameModes =
                    Array.Empty<string>();
        }

        if (metadata.gameplay.teams == null)
        {
            metadata.gameplay.teams =
                Array.Empty<MapTeamMetadata>();
        }

        foreach (MapTeamMetadata team
                 in metadata.gameplay.teams)
        {
            if (team == null)
            {
                continue;
            }

            if (team.spawnPoints == null)
            {
                team.spawnPoints =
                    Array.Empty<MapCoordinate>();
            }
        }

        if (string.IsNullOrWhiteSpace(
                metadata.terrainFile))
        {
            metadata.terrainFile =
                DefaultTerrainFile;
        }
    }

    // ============================================================
    // VALIDATION
    // ============================================================

    private static void ValidateMetadata(
        MapMetadata metadata,
        VoxelMapData mapData)
    {
        if (metadata == null)
        {
            throw new InvalidDataException(
                "Metadata nulo."
            );
        }

        if (mapData == null)
        {
            throw new InvalidDataException(
                "VoxelMapData nulo."
            );
        }

        if (metadata.sizeX !=
                mapData.SizeX ||
            metadata.sizeY !=
                mapData.SizeY ||
            metadata.sizeZ !=
                mapData.SizeZ)
        {
            throw new InvalidDataException(
                "Dimensões do map.json não " +
                "correspondem ao terrain.vxm.\n" +
                "Metadata: " +
                metadata.sizeX +
                " x " +
                metadata.sizeY +
                " x " +
                metadata.sizeZ +
                "\nTerrain: " +
                mapData.SizeX +
                " x " +
                mapData.SizeY +
                " x " +
                mapData.SizeZ
            );
        }

        ValidateGameplay(
            metadata.gameplay,
            mapData
        );
    }

    private static void ValidateGameplay(
        MapGameplayMetadata gameplay,
        VoxelMapData mapData)
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

            ValidateTeamSpawn(
                team,
                mapData
            );

            ValidateTeamBase(
                team,
                mapData
            );

            ValidateTeamFlag(
                team,
                mapData
            );
        }
    }

    private static void ValidateTeamSpawn(
        MapTeamMetadata team,
        VoxelMapData mapData)
    {
        if (team.spawnPolicy !=
            MapLocationPolicy.MapDefined)
        {
            return;
        }

        if (team.spawnPoints == null ||
            team.spawnPoints.Length == 0)
        {
            throw new InvalidDataException(
                "Time " +
                team.id +
                " usa spawn MapDefined, " +
                "mas não possui spawnPoints."
            );
        }

        foreach (MapCoordinate coordinate
                 in team.spawnPoints)
        {
            ValidateCoordinate(
                coordinate,
                mapData,
                team.id +
                " spawn"
            );
        }
    }

    private static void ValidateTeamBase(
        MapTeamMetadata team,
        VoxelMapData mapData)
    {
        if (team.basePolicy !=
            MapLocationPolicy.MapDefined)
        {
            return;
        }

        if (!team.hasBase)
        {
            throw new InvalidDataException(
                "Time " +
                team.id +
                " usa base MapDefined, " +
                "mas hasBase está false."
            );
        }

        ValidateCoordinate(
            team.basePosition,
            mapData,
            team.id +
            " base"
        );
    }

    private static void ValidateTeamFlag(
        MapTeamMetadata team,
        VoxelMapData mapData)
    {
        if (team.flagPolicy !=
            MapLocationPolicy.MapDefined)
        {
            return;
        }

        if (!team.hasFlag)
        {
            throw new InvalidDataException(
                "Time " +
                team.id +
                " usa flag MapDefined, " +
                "mas hasFlag está false."
            );
        }

        ValidateCoordinate(
            team.flagPosition,
            mapData,
            team.id +
            " flag"
        );
    }

    private static void ValidateCoordinate(
        MapCoordinate coordinate,
        VoxelMapData mapData,
        string description)
    {
        if (coordinate.x < 0 ||
            coordinate.x >= mapData.SizeX ||
            coordinate.y < 0 ||
            coordinate.y >= mapData.SizeY ||
            coordinate.z < 0 ||
            coordinate.z >= mapData.SizeZ)
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
    // TERRAIN FILE SECURITY
    // ============================================================

    private static void ValidateTerrainFileName(
        string terrainFile)
    {
        if (string.IsNullOrWhiteSpace(
                terrainFile))
        {
            throw new InvalidDataException(
                "terrainFile vazio."
            );
        }

        if (Path.IsPathRooted(
                terrainFile))
        {
            throw new InvalidDataException(
                "terrainFile não pode ser " +
                "um caminho absoluto."
            );
        }

        if (terrainFile.Contains("..") ||
            terrainFile.Contains("/") ||
            terrainFile.Contains("\\"))
        {
            throw new InvalidDataException(
                "terrainFile contém um " +
                "caminho inválido."
            );
        }
    }

    // ============================================================
    // CREATED DATE
    // ============================================================

    private static string ReadExistingCreationDate(
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

            MapMetadata metadata =
                JsonUtility.FromJson<MapMetadata>(
                    json
                );

            if (metadata == null)
            {
                return string.Empty;
            }

            return metadata.createdUtc;
        }
        catch
        {
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
            return "TestMap";
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
            return "TestMap";
        }

        return result;
    }
}