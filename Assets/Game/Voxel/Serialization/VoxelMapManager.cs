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

    [Header("Multi-Mode Gameplay")]
    [SerializeField]
    private MapGameModeMetadata[] modes =
        Array.Empty<MapGameModeMetadata>();

    [SerializeField]
    private MapPointEntityMetadata[] commonEntities =
        Array.Empty<MapPointEntityMetadata>();

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

        if (Keyboard.current.f5Key
            .wasPressedThisFrame)
        {
            SaveCurrentMap();
        }

        if (Keyboard.current.f9Key
            .wasPressedThisFrame)
        {
            LoadCurrentMap();
        }

        if (Keyboard.current.numpad1Key
            .wasPressedThisFrame)
        {
            LoadCurrentMap();
        }

        if (Keyboard.current.numpad2Key
            .wasPressedThisFrame)
        {
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
        return Path.Combine(
            GetMapsDirectory(),
            SanitizeFolderName(
                mapFolderName
            )
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
                "VoxelWorld não definido."
            );

            return false;
        }

        try
        {
            string directory =
                GetCurrentMapDirectory();

            Directory.CreateDirectory(
                directory
            );

            string metadataPath =
                Path.Combine(
                    directory,
                    MetadataFileName
                );

            string terrainPath =
                Path.Combine(
                    directory,
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

            File.WriteAllText(
                metadataPath,
                JsonUtility.ToJson(
                    metadata,
                    true
                )
            );

            Debug.Log(
                "Mapa nativo salvo com sucesso.\n" +
                "Destino: " +
                directory
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
            supportedGameModes ??
            Array.Empty<string>();

        gameplay.teams =
            teams ??
            Array.Empty<MapTeamMetadata>();

        gameplay.modes =
            modes ??
            Array.Empty<MapGameModeMetadata>();

        gameplay.commonEntities =
            commonEntities ??
            Array.Empty<MapPointEntityMetadata>();

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
                "VoxelWorld não definido."
            );

            return false;
        }

        try
        {
            string directory =
                GetCurrentMapDirectory();

            string metadataPath =
                Path.Combine(
                    directory,
                    MetadataFileName
                );

            if (!File.Exists(
                    metadataPath))
            {
                throw new FileNotFoundException(
                    "map.json não encontrado.",
                    metadataPath
                );
            }

            MapMetadata metadata =
                JsonUtility.FromJson<MapMetadata>(
                    File.ReadAllText(
                        metadataPath
                    )
                );

            if (metadata == null)
            {
                throw new InvalidDataException(
                    "map.json inválido."
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
                    directory,
                    metadata.terrainFile
                );

            if (!File.Exists(
                    terrainPath))
            {
                throw new FileNotFoundException(
                    "terrain.vxm não encontrado.",
                    terrainPath
                );
            }

            VoxelMapData mapData =
                VxmSerializer.Load(
                    terrainPath
                );

            ValidateMetadata(
                metadata,
                mapData
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
                "MAPA NATIVO CARREGADO COM SUCESSO\n" +
                "Mapa: " +
                metadata.name +
                "\nDimensões: " +
                mapData.SizeX +
                " x " +
                mapData.SizeY +
                " x " +
                mapData.SizeZ
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
    // APPLY
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

        MapGameplayMetadata gameplay =
            metadata.gameplay;

        defaultGameMode =
            gameplay.defaultGameMode;

        supportedGameModes =
            gameplay.supportedGameModes;

        teams =
            gameplay.teams;

        modes =
            gameplay.modes;

        commonEntities =
            gameplay.commonEntities;
    }

    // ============================================================
    // LOG
    // ============================================================

    private static void LogLoadedGameplay(
        MapGameplayMetadata gameplay)
    {
        int teamCount =
            gameplay.teams != null
                ? gameplay.teams.Length
                : 0;

        int modeCount =
            gameplay.modes != null
                ? gameplay.modes.Length
                : 0;

        int commonCount =
            gameplay.commonEntities != null
                ? gameplay.commonEntities.Length
                : 0;

        Debug.Log(
            "Gameplay carregado:\n" +
            "Default Mode: " +
            gameplay.defaultGameMode +
            "\nTimes: " +
            teamCount +
            "\nModos multimode: " +
            modeCount +
            "\nCommon Entities: " +
            commonCount
        );

        if (gameplay.modes == null)
        {
            return;
        }

        foreach (MapGameModeMetadata mode
                 in gameplay.modes)
        {
            if (mode == null)
            {
                continue;
            }

            int zoneCount =
                mode.zones != null
                    ? mode.zones.Length
                    : 0;

            int entityCount =
                mode.entities != null
                    ? mode.entities.Length
                    : 0;

            Debug.Log(
                "Mode: " +
                mode.id +
                "\nZones: " +
                zoneCount +
                "\nEntities: " +
                entityCount
            );
        }
    }

    // ============================================================
    // UPGRADE
    // ============================================================

    private static void UpgradeMissingMetadata(
        MapMetadata metadata)
    {
        if (metadata.gameplay == null)
        {
            metadata.gameplay =
                new MapGameplayMetadata();
        }

        MapGameplayMetadata gameplay =
            metadata.gameplay;

        if (gameplay.supportedGameModes == null)
        {
            gameplay.supportedGameModes =
                Array.Empty<string>();
        }

        if (gameplay.teams == null)
        {
            gameplay.teams =
                Array.Empty<MapTeamMetadata>();
        }

        if (gameplay.modes == null)
        {
            gameplay.modes =
                Array.Empty<MapGameModeMetadata>();
        }

        if (gameplay.commonEntities == null)
        {
            gameplay.commonEntities =
                Array.Empty<MapPointEntityMetadata>();
        }

        foreach (MapTeamMetadata team
                 in gameplay.teams)
        {
            if (team != null &&
                team.spawnPoints == null)
            {
                team.spawnPoints =
                    Array.Empty<MapCoordinate>();
            }
        }

        foreach (MapGameModeMetadata mode
                 in gameplay.modes)
        {
            if (mode == null)
            {
                continue;
            }

            if (mode.zones == null)
            {
                mode.zones =
                    Array.Empty<MapZoneMetadata>();
            }

            if (mode.entities == null)
            {
                mode.entities =
                    Array.Empty<MapPointEntityMetadata>();
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
        if (metadata.sizeX !=
                mapData.SizeX ||
            metadata.sizeY !=
                mapData.SizeY ||
            metadata.sizeZ !=
                mapData.SizeZ)
        {
            throw new InvalidDataException(
                "Dimensões do map.json não " +
                "correspondem ao terrain.vxm."
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

        ValidateTeams(
            gameplay.teams,
            mapData
        );

        ValidateModes(
            gameplay.modes,
            mapData
        );

        ValidateEntities(
            gameplay.commonEntities,
            mapData
        );
    }

    private static void ValidateTeams(
        MapTeamMetadata[] teamList,
        VoxelMapData mapData)
    {
        if (teamList == null)
        {
            return;
        }

        HashSet<string> ids =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase
            );

        foreach (MapTeamMetadata team
                 in teamList)
        {
            if (team == null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(
                    team.id))
            {
                if (!ids.Add(
                        team.id))
                {
                    throw new InvalidDataException(
                        "Time duplicado: " +
                        team.id
                    );
                }
            }

            if (team.spawnPolicy ==
                    MapLocationPolicy.MapDefined &&
                team.spawnPoints != null)
            {
                foreach (MapCoordinate coordinate
                         in team.spawnPoints)
                {
                    ValidateCoordinate(
                        coordinate,
                        mapData
                    );
                }
            }

            if (team.basePolicy ==
                    MapLocationPolicy.MapDefined &&
                team.hasBase)
            {
                ValidateCoordinate(
                    team.basePosition,
                    mapData
                );
            }

            if (team.flagPolicy ==
                    MapLocationPolicy.MapDefined &&
                team.hasFlag)
            {
                ValidateCoordinate(
                    team.flagPosition,
                    mapData
                );
            }
        }
    }

    private static void ValidateModes(
        MapGameModeMetadata[] modeList,
        VoxelMapData mapData)
    {
        if (modeList == null)
        {
            return;
        }

        foreach (MapGameModeMetadata mode
                 in modeList)
        {
            if (mode == null)
            {
                continue;
            }

            if (mode.zones != null)
            {
                foreach (MapZoneMetadata zone
                         in mode.zones)
                {
                    if (zone != null)
                    {
                        ValidateCoordinate(
                            zone.center,
                            mapData
                        );
                    }
                }
            }

            ValidateEntities(
                mode.entities,
                mapData
            );
        }
    }

    private static void ValidateEntities(
        MapPointEntityMetadata[] entities,
        VoxelMapData mapData)
    {
        if (entities == null)
        {
            return;
        }

        foreach (MapPointEntityMetadata entity
                 in entities)
        {
            if (entity != null)
            {
                ValidateCoordinate(
                    entity.position,
                    mapData
                );
            }
        }
    }

    private static void ValidateCoordinate(
        MapCoordinate coordinate,
        VoxelMapData mapData)
    {
        if (coordinate.x < 0 ||
            coordinate.x >= mapData.SizeX ||
            coordinate.y < 0 ||
            coordinate.y >= mapData.SizeY ||
            coordinate.z < 0 ||
            coordinate.z >= mapData.SizeZ)
        {
            throw new InvalidDataException(
                "Coordenada fora dos limites: (" +
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
    // SECURITY
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
                terrainFile) ||
            terrainFile.Contains("..") ||
            terrainFile.Contains("/") ||
            terrainFile.Contains("\\"))
        {
            throw new InvalidDataException(
                "terrainFile inválido."
            );
        }
    }

    // ============================================================
    // DATE
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
            MapMetadata metadata =
                JsonUtility.FromJson<MapMetadata>(
                    File.ReadAllText(
                        metadataPath
                    )
                );

            return metadata != null
                ? metadata.createdUtc
                : string.Empty;
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
            return "TestMap";
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

        return string.IsNullOrWhiteSpace(
                result)
            ? "TestMap"
            : result;
    }
}