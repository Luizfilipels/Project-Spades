using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

using Stopwatch =
    System.Diagnostics.Stopwatch;

public class LegacyMapImporter : MonoBehaviour
{
    [Header("World")]
    [SerializeField]
    private VoxelWorld world;

    [Header("Selection")]
    [SerializeField]
    private int selectedMapIndex = 0;

    [Header("Debug Hotkeys")]
    [SerializeField]
    private bool enableDebugHotkeys = true;

    private List<LegacyMapEntry> maps =
        new List<LegacyMapEntry>();

    private void Awake()
    {
        if (world == null)
        {
            world =
                FindAnyObjectByType<VoxelWorld>();
        }

        RefreshMapList();
    }

    private void Start()
    {
        LogSelectedMap();
    }

    private void Update()
    {
        if (!enableDebugHotkeys ||
            Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.f6Key
            .wasPressedThisFrame)
        {
            SelectPreviousMap();
        }

        if (Keyboard.current.f7Key
            .wasPressedThisFrame)
        {
            SelectNextMap();
        }

        if (Keyboard.current.f8Key
            .wasPressedThisFrame)
        {
            ImportSelectedMap();
        }
    }

    // ============================================================
    // MAP CATALOG
    // ============================================================

    public void RefreshMapList()
    {
        maps =
            LegacyMapCatalog.FindMaps();

        if (maps.Count == 0)
        {
            selectedMapIndex = 0;

            Debug.LogWarning(
                "Nenhum mapa VXL encontrado em:\n" +
                LegacyMapCatalog.GetMapsDirectory()
            );

            return;
        }

        selectedMapIndex =
            Mathf.Clamp(
                selectedMapIndex,
                0,
                maps.Count - 1
            );

        Debug.Log(
            "Mapas VXL encontrados: " +
            maps.Count
        );
    }

    public void SelectNextMap()
    {
        if (!EnsureMapsAvailable())
        {
            return;
        }

        selectedMapIndex++;

        if (selectedMapIndex >= maps.Count)
        {
            selectedMapIndex = 0;
        }

        LogSelectedMap();
    }

    public void SelectPreviousMap()
    {
        if (!EnsureMapsAvailable())
        {
            return;
        }

        selectedMapIndex--;

        if (selectedMapIndex < 0)
        {
            selectedMapIndex =
                maps.Count - 1;
        }

        LogSelectedMap();
    }

    // ============================================================
    // IMPORT
    // ============================================================

    public bool ImportSelectedMap()
    {
        if (world == null)
        {
            Debug.LogError(
                "LegacyMapImporter: " +
                "VoxelWorld não definido."
            );

            return false;
        }

        if (!EnsureMapsAvailable())
        {
            return false;
        }

        if (selectedMapIndex < 0 ||
            selectedMapIndex >= maps.Count)
        {
            Debug.LogError(
                "Índice de mapa inválido."
            );

            return false;
        }

        LegacyMapEntry selectedMap =
            maps[selectedMapIndex];

        try
        {
            Debug.Log(
                "================================\n" +
                "IMPORTANDO: " +
                selectedMap.Name +
                "\n" +
                "================================"
            );

            LogSidecars(
                selectedMap
            );

            if (selectedMap.HasTxt)
            {
                TryReadPySnipTxt(
                    selectedMap
                );
            }

            if (selectedMap.HasUgc)
            {
                TryReadJagexUgc(
                    selectedMap
                );
            }

            Stopwatch stopwatch =
                Stopwatch.StartNew();

            Debug.Log(
                "Importando VXL:\n" +
                selectedMap.VxlPath
            );

            VoxelMapData mapData =
                VxlReader.Load(
                    selectedMap.VxlPath
                );

            Debug.Log(
                "VXL decodificado. Dimensões: " +
                mapData.SizeX +
                " x " +
                mapData.SizeY +
                " x " +
                mapData.SizeZ
            );

            world.LoadMapData(
                mapData
            );

            stopwatch.Stop();

            Debug.Log(
                "Mapa \"" +
                selectedMap.Name +
                "\" importado com sucesso.\n" +
                "Tempo total: " +
                stopwatch.Elapsed.TotalSeconds
                    .ToString("F2") +
                "s"
            );

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "Erro ao importar \"" +
                selectedMap.Name +
                "\":\n" +
                exception
            );

            return false;
        }
    }

    // ============================================================
    // PYSNIP
    // ============================================================

    private void TryReadPySnipTxt(
        LegacyMapEntry map)
    {
        try
        {
            PySnipMapScriptData data =
                PySnipMapScriptImporter.Load(
                    map.TxtPath
                );

            Debug.Log(
                "PySnip TXT carregado:\n" +
                "Nome: " +
                data.Name +
                "\n" +
                "Versão: " +
                data.Version +
                "\n" +
                "Autor: " +
                data.Author +
                "\n" +
                "Descrição: " +
                data.Description +
                "\n" +
                "Extensions: " +
                data.Extensions.Count
            );

            LogPySnipSpawnPolicy(
                data
            );

            LogPySnipEntityPolicy(
                data
            );

            LogPySnipCoordinates(
                data
            );

            MapGameplayMetadata gameplay =
                PySnipGameplayConverter.Convert(
                    data
                );

            LogNativeGameplayMetadata(
                gameplay
            );
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "O VXL será carregado, mas o " +
                "TXT PySnip não pôde ser " +
                "interpretado:\n" +
                exception
            );
        }
    }

    private static void LogPySnipSpawnPolicy(
        PySnipMapScriptData data)
    {
        string bluePolicy =
            GetSpawnPolicyText(
                data.HasCustomBlueSpawns,
                data.BlueSpawns.Count
            );

        string greenPolicy =
            GetSpawnPolicyText(
                data.HasCustomGreenSpawns,
                data.GreenSpawns.Count
            );

        Debug.Log(
            "PySnip Spawn Policy:\n" +
            "Blue: " +
            bluePolicy +
            "\n" +
            "Green: " +
            greenPolicy
        );
    }

    private static void LogPySnipEntityPolicy(
        PySnipMapScriptData data)
    {
        string blueBasePolicy =
            GetEntityPolicyText(
                data.HasBlueBase
            );

        string greenBasePolicy =
            GetEntityPolicyText(
                data.HasGreenBase
            );

        string blueFlagPolicy =
            GetEntityPolicyText(
                data.HasBlueFlag
            );

        string greenFlagPolicy =
            GetEntityPolicyText(
                data.HasGreenFlag
            );

        Debug.Log(
            "PySnip Entity Policy:\n" +
            "Blue Base: " +
            blueBasePolicy +
            "\n" +
            "Green Base: " +
            greenBasePolicy +
            "\n" +
            "Blue Flag: " +
            blueFlagPolicy +
            "\n" +
            "Green Flag: " +
            greenFlagPolicy
        );
    }

    private static string GetSpawnPolicyText(
        bool hasCustomSpawns,
        int count)
    {
        if (hasCustomSpawns)
        {
            return
                "Map Defined (" +
                count +
                ")";
        }

        return "Server Default";
    }

    private static string GetEntityPolicyText(
        bool mapDefined)
    {
        if (mapDefined)
        {
            return "Map Defined";
        }

        return "Server Default";
    }

    private static void LogPySnipCoordinates(
        PySnipMapScriptData data)
    {
        if (data.HasBlueBase)
        {
            Debug.Log(
                "Blue Base -> " +
                FormatCoordinate(
                    data.BlueBase
                )
            );
        }

        if (data.HasGreenBase)
        {
            Debug.Log(
                "Green Base -> " +
                FormatCoordinate(
                    data.GreenBase
                )
            );
        }

        if (data.HasBlueFlag)
        {
            Debug.Log(
                "Blue Flag -> " +
                FormatCoordinate(
                    data.BlueFlag
                )
            );
        }

        if (data.HasGreenFlag)
        {
            Debug.Log(
                "Green Flag -> " +
                FormatCoordinate(
                    data.GreenFlag
                )
            );
        }

        for (int i = 0;
             i < data.BlueSpawns.Count;
             i++)
        {
            Debug.Log(
                "Blue Spawn #" +
                (i + 1) +
                " -> " +
                FormatCoordinate(
                    data.BlueSpawns[i]
                )
            );
        }

        for (int i = 0;
             i < data.GreenSpawns.Count;
             i++)
        {
            Debug.Log(
                "Green Spawn #" +
                (i + 1) +
                " -> " +
                FormatCoordinate(
                    data.GreenSpawns[i]
                )
            );
        }
    }

    // ============================================================
    // NATIVE GAMEPLAY METADATA
    // ============================================================

    private static void LogNativeGameplayMetadata(
        MapGameplayMetadata gameplay)
    {
        if (gameplay == null)
        {
            Debug.LogWarning(
                "MapGameplayMetadata nulo."
            );

            return;
        }

        Debug.Log(
            "================================\n" +
            "METADATA NATIVO GERADO\n" +
            "================================"
        );

        Debug.Log(
            "Default Game Mode: " +
            gameplay.defaultGameMode
        );

        if (gameplay.supportedGameModes != null)
        {
            string supportedModes =
                string.Join(
                    ", ",
                    gameplay.supportedGameModes
                );

            Debug.Log(
                "Supported Game Modes: " +
                supportedModes
            );
        }

        if (gameplay.teams == null ||
            gameplay.teams.Length == 0)
        {
            Debug.LogWarning(
                "Nenhum time no metadata."
            );

            return;
        }

        foreach (MapTeamMetadata team
                 in gameplay.teams)
        {
            if (team == null)
            {
                continue;
            }

            int spawnCount = 0;

            if (team.spawnPoints != null)
            {
                spawnCount =
                    team.spawnPoints.Length;
            }

            Debug.Log(
                "--------------------------------\n" +
                "Time: " +
                team.displayName +
                "\n" +
                "ID: " +
                team.id +
                "\n" +
                "Spawn Policy: " +
                team.spawnPolicy +
                "\n" +
                "Spawn Points: " +
                spawnCount +
                "\n" +
                "Base Policy: " +
                team.basePolicy +
                "\n" +
                "Has Base: " +
                team.hasBase +
                "\n" +
                "Flag Policy: " +
                team.flagPolicy +
                "\n" +
                "Has Flag: " +
                team.hasFlag
            );

            if (team.spawnPolicy ==
                    MapLocationPolicy.MapDefined &&
                team.spawnPoints != null)
            {
                for (int i = 0;
                     i < team.spawnPoints.Length;
                     i++)
                {
                    Debug.Log(
                        team.displayName +
                        " Native Spawn #" +
                        (i + 1) +
                        " -> " +
                        FormatCoordinate(
                            team.spawnPoints[i]
                        )
                    );
                }
            }

            if (team.basePolicy ==
                    MapLocationPolicy.MapDefined &&
                team.hasBase)
            {
                Debug.Log(
                    team.displayName +
                    " Native Base -> " +
                    FormatCoordinate(
                        team.basePosition
                    )
                );
            }

            if (team.flagPolicy ==
                    MapLocationPolicy.MapDefined &&
                team.hasFlag)
            {
                Debug.Log(
                    team.displayName +
                    " Native Flag -> " +
                    FormatCoordinate(
                        team.flagPosition
                    )
                );
            }
        }
    }

    // ============================================================
    // JAGEX
    // ============================================================

    private void TryReadJagexUgc(
        LegacyMapEntry map)
    {
        try
        {
            JagexUgcData ugc =
                JagexUgcImporter.Load(
                    map.UgcPath
                );

            string[] modes =
                JagexUgcImporter.GetModes(
                    ugc
                );

            string modeText;

            if (modes.Length > 0)
            {
                modeText =
                    string.Join(
                        ", ",
                        modes
                    );
            }
            else
            {
                modeText =
                    "nenhum";
            }

            Debug.Log(
                "Jagex UGC carregado:\n" +
                "Título: " +
                ugc.title +
                "\n" +
                "Autor: " +
                ugc.author +
                "\n" +
                "Descrição: " +
                ugc.description +
                "\n" +
                "Baseplate: " +
                ugc.baseplate +
                "\n" +
                "Skybox: " +
                ugc.skybox_name +
                "\n" +
                "Entidades: " +
                ugc.ugc_entities.Length +
                "\n" +
                "Modos: " +
                modeText
            );
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "O VXL será carregado, mas o " +
                "arquivo UGC não pôde ser lido:\n" +
                exception
            );
        }
    }

    // ============================================================
    // GENERAL LOGGING
    // ============================================================

    private void LogSelectedMap()
    {
        if (maps.Count == 0)
        {
            Debug.LogWarning(
                "Nenhum mapa legado disponível."
            );

            return;
        }

        LegacyMapEntry map =
            maps[selectedMapIndex];

        Debug.Log(
            "Mapa selecionado: [" +
            (selectedMapIndex + 1) +
            "/" +
            maps.Count +
            "] " +
            map.Name +
            " " +
            GetSidecarDescription(map)
        );
    }

    private static void LogSidecars(
        LegacyMapEntry map)
    {
        if (map.HasTxt)
        {
            Debug.Log(
                "TXT/PySnip encontrado:\n" +
                map.TxtPath
            );
        }

        if (map.HasUgc)
        {
            Debug.Log(
                "UGC/Jagex encontrado:\n" +
                map.UgcPath
            );
        }

        if (!map.HasTxt &&
            !map.HasUgc)
        {
            Debug.Log(
                "Nenhum sidecar de " +
                "metadados encontrado."
            );
        }
    }

    private static string GetSidecarDescription(
        LegacyMapEntry map)
    {
        if (map.HasTxt &&
            map.HasUgc)
        {
            return "(TXT + UGC)";
        }

        if (map.HasUgc)
        {
            return "(UGC)";
        }

        if (map.HasTxt)
        {
            return "(TXT)";
        }

        return "(somente VXL)";
    }

    private bool EnsureMapsAvailable()
    {
        if (maps.Count > 0)
        {
            return true;
        }

        RefreshMapList();

        return maps.Count > 0;
    }

    private static string FormatCoordinate(
        MapCoordinate coordinate)
    {
        return
            "(" +
            coordinate.x +
            ", " +
            coordinate.y +
            ", " +
            coordinate.z +
            ")";
    }
}