using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

public class VoxelMapManager :
    MonoBehaviour
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

    [TextArea]
    [SerializeField]
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

    private const int CurrentMetadataVersion =
        1;

    private const string MapsFolder =
        "Maps";

    private const string MetadataFile =
        "map.json";

    private const string DefaultTerrainFile =
        "terrain.vxm";

    private void Awake()
    {
        if (world == null)
        {
            world =
                FindAnyObjectByType<
                    VoxelWorld>();
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
    }

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
            string mapDirectory =
                GetMapDirectory();

            Directory.CreateDirectory(
                mapDirectory
            );

            string terrainPath =
                Path.Combine(
                    mapDirectory,
                    DefaultTerrainFile
                );

            string metadataPath =
                Path.Combine(
                    mapDirectory,
                    MetadataFile
                );

            VoxelMapData mapData =
                world.CaptureMapData();

            VxmSerializer.Save(
                terrainPath,
                mapData
            );

            string now =
                DateTime.UtcNow
                    .ToString("O");

            string createdUtc =
                GetExistingCreationDate(
                    metadataPath
                );

            if (string.IsNullOrEmpty(
                    createdUtc))
            {
                createdUtc = now;
            }

            MapGameplayMetadata gameplay =
                new MapGameplayMetadata
                {
                    defaultGameMode =
                        defaultGameMode,

                    supportedGameModes =
                        supportedGameModes ??
                        Array.Empty<string>(),

                    teams =
                        teams ??
                        Array.Empty<
                            MapTeamMetadata>()
                };

            MapMetadata metadata =
                new MapMetadata
                {
                    formatVersion =
                        CurrentMetadataVersion,

                    name =
                        mapName,

                    author =
                        author,

                    description =
                        description,

                    terrainFile =
                        DefaultTerrainFile,

                    sizeX =
                        mapData.SizeX,

                    sizeY =
                        mapData.SizeY,

                    sizeZ =
                        mapData.SizeZ,

                    createdUtc =
                        createdUtc,

                    modifiedUtc =
                        now,

                    gameplay =
                        gameplay
                };

            ValidateMetadata(
                metadata,
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
                "Mapa salvo com sucesso:\n" +
                mapDirectory
            );

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "Erro ao salvar mapa:\n" +
                exception
            );

            return false;
        }
    }

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
                GetMapDirectory();

            string metadataPath =
                Path.Combine(
                    mapDirectory,
                    MetadataFile
                );

            if (!File.Exists(
                    metadataPath))
            {
                Debug.LogError(
                    "map.json não encontrado:\n" +
                    metadataPath
                );

                return false;
            }

            string json =
                File.ReadAllText(
                    metadataPath
                );

            MapMetadata metadata =
                JsonUtility.FromJson<
                    MapMetadata>(json);

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
                    mapDirectory,
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

            Debug.Log(
                $"Mapa carregado: " +
                $"{metadata.name}\n" +
                $"Modo padrão: " +
                $"{metadata.gameplay.defaultGameMode}\n" +
                $"Times: " +
                $"{metadata.gameplay.teams.Length}\n" +
                mapDirectory
            );

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "Erro ao carregar mapa:\n" +
                exception
            );

            return false;
        }
    }

    private void ApplyLoadedMetadata(
        MapMetadata metadata)
    {
        mapName =
            metadata.name;

        author =
            metadata.author;

        description =
            metadata.description;

        defaultGameMode =
            metadata.gameplay
                .defaultGameMode;

        supportedGameModes =
            metadata.gameplay
                .supportedGameModes ??
            Array.Empty<string>();

        teams =
            metadata.gameplay.teams ??
            Array.Empty<MapTeamMetadata>();
    }

    private static void
        UpgradeMissingMetadata(
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

        if (metadata.gameplay
                .teams == null)
        {
            metadata.gameplay.teams =
                Array.Empty<
                    MapTeamMetadata>();
        }
    }

    private string GetMapDirectory()
    {
        string safeFolderName =
            SanitizeFolderName(
                mapFolderName
            );

        return Path.Combine(
            Application.persistentDataPath,
            MapsFolder,
            safeFolderName
        );
    }

    private static string
        SanitizeFolderName(
            string folderName)
    {
        if (string.IsNullOrWhiteSpace(
                folderName))
        {
            return "UnnamedMap";
        }

        char[] invalidChars =
            Path.GetInvalidFileNameChars();

        foreach (char invalid
                 in invalidChars)
        {
            folderName =
                folderName.Replace(
                    invalid,
                    '_'
                );
        }

        folderName =
            folderName.Replace(
                "..",
                "_"
            );

        folderName =
            folderName.Replace(
                '/',
                '_'
            );

        folderName =
            folderName.Replace(
                '\\',
                '_'
            );

        return folderName;
    }

    private static void
        ValidateTerrainFileName(
            string terrainFile)
    {
        if (string.IsNullOrWhiteSpace(
                terrainFile))
        {
            throw new InvalidDataException(
                "terrainFile ausente."
            );
        }

        if (Path.GetFileName(
                terrainFile) !=
            terrainFile)
        {
            throw new InvalidDataException(
                "terrainFile contém " +
                "um caminho inválido."
            );
        }
    }

    private static void
        ValidateMetadata(
            MapMetadata metadata,
            VoxelMapData mapData)
    {
        if (metadata.formatVersion !=
            CurrentMetadataVersion)
        {
            throw new InvalidDataException(
                $"Versão de map.json não " +
                $"suportada: " +
                $"{metadata.formatVersion}"
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
                "As dimensões do map.json " +
                "não correspondem ao terrain.vxm."
            );
        }

        ValidateGameplay(
            metadata.gameplay,
            mapData
        );
    }

    private static void
        ValidateGameplay(
            MapGameplayMetadata gameplay,
            VoxelMapData mapData)
    {
        if (gameplay == null)
            return;

        MapTeamMetadata[] mapTeams =
            gameplay.teams ??
            Array.Empty<MapTeamMetadata>();

        HashSet<string> teamIds =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase
            );

        foreach (MapTeamMetadata team
                 in mapTeams)
        {
            if (team == null)
            {
                throw new InvalidDataException(
                    "O mapa contém um time inválido."
                );
            }

            if (string.IsNullOrWhiteSpace(
                    team.id))
            {
                throw new InvalidDataException(
                    "Todo time deve possuir um ID."
                );
            }

            if (!teamIds.Add(team.id))
            {
                throw new InvalidDataException(
                    $"ID de time duplicado: " +
                    $"{team.id}"
                );
            }

            MapCoordinate[] spawns =
                team.spawnPoints ??
                Array.Empty<MapCoordinate>();

            foreach (MapCoordinate spawn
                     in spawns)
            {
                ValidateCoordinate(
                    spawn,
                    mapData,
                    $"spawn do time {team.id}"
                );
            }

            if (team.hasBase)
            {
                ValidateCoordinate(
                    team.basePosition,
                    mapData,
                    $"base do time {team.id}"
                );
            }

            if (team.hasFlag)
            {
                ValidateCoordinate(
                    team.flagPosition,
                    mapData,
                    $"flag do time {team.id}"
                );
            }
        }
    }

    private static void
        ValidateCoordinate(
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
                $"{description} está fora " +
                $"dos limites do mapa: " +
                $"({coordinate.x}, " +
                $"{coordinate.y}, " +
                $"{coordinate.z})"
            );
        }
    }

    private static string
        GetExistingCreationDate(
            string metadataPath)
    {
        if (!File.Exists(
                metadataPath))
        {
            return null;
        }

        try
        {
            string json =
                File.ReadAllText(
                    metadataPath
                );

            MapMetadata metadata =
                JsonUtility.FromJson<
                    MapMetadata>(json);

            return metadata?.createdUtc;
        }
        catch
        {
            return null;
        }
    }
}