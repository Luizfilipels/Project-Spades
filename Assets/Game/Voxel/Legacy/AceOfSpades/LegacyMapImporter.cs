using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

using Stopwatch =
    System.Diagnostics.Stopwatch;

public class LegacyMapImporter :
    MonoBehaviour
{
    [Header("World")]
    [SerializeField]
    private VoxelWorld world;

    [Header("Selection")]
    [SerializeField]
    private int selectedMapIndex = 0;

    [Header("Debug Hotkeys")]
    [SerializeField]
    private bool enableDebugHotkeys =
        true;

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

    public void RefreshMapList()
    {
        maps =
            LegacyMapCatalog.FindMaps();

        if (maps.Count == 0)
        {
            selectedMapIndex = 0;

            Debug.LogWarning(
                "Nenhum mapa VXL encontrado em:\n" +
                LegacyMapCatalog
                    .GetMapsDirectory()
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
            $"Mapas VXL encontrados: " +
            $"{maps.Count}"
        );
    }

    public void SelectNextMap()
    {
        if (!EnsureMapsAvailable())
            return;

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
            return;

        selectedMapIndex--;

        if (selectedMapIndex < 0)
        {
            selectedMapIndex =
                maps.Count - 1;
        }

        LogSelectedMap();
    }

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
                $"IMPORTANDO: {selectedMap.Name}\n" +
                "================================"
            );

            LogSidecars(
                selectedMap
            );

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
                "VXL decodificado. " +
                $"Dimensões: " +
                $"{mapData.SizeX} x " +
                $"{mapData.SizeY} x " +
                $"{mapData.SizeZ}"
            );

            world.LoadMapData(
                mapData
            );

            stopwatch.Stop();

            Debug.Log(
                $"Mapa \"{selectedMap.Name}\" " +
                "importado com sucesso.\n" +
                $"Tempo total: " +
                $"{stopwatch.Elapsed.TotalSeconds:F2}s"
            );

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"Erro ao importar " +
                $"\"{selectedMap.Name}\":\n" +
                exception
            );

            return false;
        }
    }

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

            string modeText =
                modes.Length > 0
                    ? string.Join(", ", modes)
                    : "nenhum";

            Debug.Log(
                "Jagex UGC carregado:\n" +
                $"Título: {ugc.title}\n" +
                $"Autor: {ugc.author}\n" +
                $"Descrição: {ugc.description}\n" +
                $"Baseplate: {ugc.baseplate}\n" +
                $"Skybox: {ugc.skybox_name}\n" +
                $"Entidades: " +
                $"{ugc.ugc_entities.Length}\n" +
                $"Modos: {modeText}"
            );
        }
        catch (Exception exception)
        {
            /*
             * Um sidecar UGC inválido não deve
             * impedir o terreno VXL de abrir.
             */
            Debug.LogWarning(
                "O VXL será carregado, mas o " +
                "arquivo UGC não pôde ser lido:\n" +
                exception
            );
        }
    }

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
            $"Mapa selecionado: " +
            $"[{selectedMapIndex + 1}/" +
            $"{maps.Count}] " +
            $"{map.Name} " +
            $"{GetSidecarDescription(map)}"
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
                "Nenhum sidecar de metadados " +
                "foi encontrado."
            );
        }
    }

    private static string
        GetSidecarDescription(
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
}