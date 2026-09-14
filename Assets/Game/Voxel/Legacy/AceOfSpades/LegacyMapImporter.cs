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

        if (Keyboard.current.numpad0Key
            .wasPressedThisFrame)
        {
            ConvertSelectedMapToNative();
        }
    }

    // ============================================================
    // CATALOG
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

        if (selectedMapIndex >=
            maps.Count)
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
                "VoxelWorld não definido."
            );

            return false;
        }

        if (!EnsureMapsAvailable())
        {
            return false;
        }

        LegacyMapEntry map =
            GetSelectedMap();

        if (map == null)
        {
            return false;
        }

        try
        {
            Debug.Log(
                "================================\n" +
                "IMPORTANDO: " +
                map.Name +
                "\n" +
                "================================"
            );

            LogSidecars(
                map
            );

            if (map.HasUgc)
            {
                TryReadJagexUgc(
                    map
                );
            }
            else if (map.HasTxt)
            {
                TryReadPySnipTxt(
                    map
                );
            }

            Stopwatch stopwatch =
                Stopwatch.StartNew();

            Debug.Log(
                "Importando VXL:\n" +
                map.VxlPath
            );

            VoxelMapData terrain =
                VxlReader.Load(
                    map.VxlPath
                );

            Debug.Log(
                "VXL decodificado. Dimensões: " +
                terrain.SizeX +
                " x " +
                terrain.SizeY +
                " x " +
                terrain.SizeZ
            );

            world.LoadMapData(
                terrain
            );

            stopwatch.Stop();

            Debug.Log(
                "Mapa \"" +
                map.Name +
                "\" importado com sucesso.\n" +
                "Tempo: " +
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
                map.Name +
                "\":\n" +
                exception
            );

            return false;
        }
    }

    // ============================================================
    // CONVERSION
    // ============================================================

    public bool ConvertSelectedMapToNative()
    {
        if (!EnsureMapsAvailable())
        {
            return false;
        }

        LegacyMapEntry map =
            GetSelectedMap();

        if (map == null)
        {
            return false;
        }

        try
        {
            Debug.Log(
                "================================\n" +
                "CONVERSÃO NATIVA\n" +
                "Mapa: " +
                map.Name +
                "\n" +
                "================================"
            );

            Stopwatch stopwatch =
                Stopwatch.StartNew();

            string outputDirectory;

            if (map.HasUgc)
            {
                Debug.Log(
                    "Pipeline: Jagex/Retail"
                );

                outputDirectory =
                    LegacyMapNativeConverter
                        .ConvertJagexMap(
                            map
                        );
            }
            else if (map.HasTxt)
            {
                Debug.Log(
                    "Pipeline: PySnip/PySpades"
                );

                outputDirectory =
                    LegacyMapNativeConverter
                        .ConvertPySnipMap(
                            map
                        );
            }
            else
            {
                Debug.LogWarning(
                    "O mapa possui somente VXL. " +
                    "Ainda não há metadata suficiente " +
                    "para conversão completa."
                );

                return false;
            }

            stopwatch.Stop();

            Debug.Log(
                "CONVERSÃO NATIVA CONCLUÍDA\n" +
                "Destino: " +
                outputDirectory +
                "\n" +
                "terrain.vxm: OK\n" +
                "map.json: OK\n" +
                "Tempo: " +
                stopwatch.Elapsed.TotalSeconds
                    .ToString("F2") +
                "s"
            );

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "Erro na conversão nativa de \"" +
                map.Name +
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

            MapGameplayMetadata gameplay =
                PySnipGameplayConverter.Convert(
                    data
                );

            int teamCount =
                gameplay.teams != null
                    ? gameplay.teams.Length
                    : 0;

            Debug.Log(
                "PySnip TXT carregado:\n" +
                "Nome: " +
                data.Name +
                "\n" +
                "Autor: " +
                data.Author +
                "\n" +
                "Times convertidos: " +
                teamCount
            );
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "TXT PySnip não pôde ser lido:\n" +
                exception
            );
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

            JagexGameplayData intermediate =
                JagexGameplayConverter.Convert(
                    ugc,
                    map.VxlPath
                );

            MapGameplayMetadata native =
                JagexNativeGameplayConverter.Convert(
                    intermediate
                );

            Debug.Log(
                "Jagex UGC carregado:\n" +
                "Título: " +
                ugc.title +
                "\n" +
                "Autor: " +
                ugc.author +
                "\n" +
                "Source Z Shift: " +
                intermediate.SourceZShift +
                "\n" +
                "Zonas convertidas: " +
                intermediate.Zones.Count +
                "\n" +
                "Point Entities: " +
                intermediate.PointEntities.Count +
                "\n" +
                "Unsupported: " +
                intermediate.Issues.Count +
                "\n" +
                "Modos nativos: " +
                (
                    native.modes != null
                        ? native.modes.Length
                        : 0
                ) +
                "\n" +
                "Common Entities: " +
                (
                    native.commonEntities != null
                        ? native.commonEntities.Length
                        : 0
                )
            );

            LogNativeJagexModes(
                native
            );
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "UGC Jagex não pôde ser lido:\n" +
                exception
            );
        }
    }

    private static void LogNativeJagexModes(
        MapGameplayMetadata gameplay)
    {
        if (gameplay == null ||
            gameplay.modes == null)
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

            int zones =
                mode.zones != null
                    ? mode.zones.Length
                    : 0;

            int entities =
                mode.entities != null
                    ? mode.entities.Length
                    : 0;

            Debug.Log(
                "Native Mode: " +
                mode.id +
                "\nZones: " +
                zones +
                "\nEntities: " +
                entities
            );
        }
    }

    // ============================================================
    // GENERAL
    // ============================================================

    private LegacyMapEntry GetSelectedMap()
    {
        if (selectedMapIndex < 0 ||
            selectedMapIndex >= maps.Count)
        {
            Debug.LogError(
                "Índice de mapa inválido."
            );

            return null;
        }

        return maps[selectedMapIndex];
    }

    private void LogSelectedMap()
    {
        LegacyMapEntry map =
            GetSelectedMap();

        if (map == null)
        {
            return;
        }

        Debug.Log(
            "Mapa selecionado: [" +
            (selectedMapIndex + 1) +
            "/" +
            maps.Count +
            "] " +
            map.Name +
            " " +
            GetSidecarDescription(
                map
            )
        );
    }

    private static void LogSidecars(
        LegacyMapEntry map)
    {
        if (map.HasUgc)
        {
            Debug.Log(
                "Formato identificado: Jagex/Retail\n" +
                "UGC: " +
                map.UgcPath
            );

            return;
        }

        if (map.HasTxt)
        {
            Debug.Log(
                "Formato identificado: PySnip/PySpades\n" +
                "TXT: " +
                map.TxtPath
            );

            return;
        }

        Debug.Log(
            "Formato identificado: somente VXL."
        );
    }

    private static string GetSidecarDescription(
        LegacyMapEntry map)
    {
        if (map.HasUgc)
        {
            return "(Jagex UGC)";
        }

        if (map.HasTxt)
        {
            return "(PySnip TXT)";
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
}