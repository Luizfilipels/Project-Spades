using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(VoxelRaycaster))]
[RequireComponent(typeof(BlockPlacementPreview))]
[RequireComponent(typeof(BlockPlacementService))]
public class BlockPlacer : MonoBehaviour
{
    [Header("Player Build State")]
    [SerializeField]
    private PlayerBuildState buildState;

    [Header("Build Line")]
    [SerializeField]
    private int maxBlocksPerLine = 8;

    [SerializeField]
    private float placementInterval = 0.05f;

    private VoxelRaycaster voxelRaycaster;

    private BlockPlacementPreview preview;

    private BlockPlacementService
        placementService;

    private bool isPlanning;

    private Vector3Int startVoxel;

    private BlockType plannedBlockType;

    private VoxelColor plannedColor;

    private readonly List<Vector3Int>
        plannedBlocks =
            new List<Vector3Int>();

    private void Awake()
    {
        voxelRaycaster =
            GetComponent<VoxelRaycaster>();

        preview =
            GetComponent<BlockPlacementPreview>();

        placementService =
            GetComponent<BlockPlacementService>();

        if (buildState == null)
        {
            buildState =
                GetComponent<PlayerBuildState>();
        }
    }

    private void Update()
    {
        if (Mouse.current == null)
            return;

        if (buildState == null)
            return;

        if (Mouse.current.rightButton
            .wasPressedThisFrame)
        {
            BeginPlacement();
        }

        if (isPlanning &&
            Mouse.current.rightButton.isPressed)
        {
            UpdatePlacement();
        }

        if (isPlanning &&
            Mouse.current.rightButton
                .wasReleasedThisFrame)
        {
            FinishPlacement();
        }
    }

    private void BeginPlacement()
    {
        if (!voxelRaycaster.TryGetVoxelHit(
                out _,
                out Vector3Int adjacentVoxel,
                out _))
        {
            return;
        }

        startVoxel =
            adjacentVoxel;

        plannedBlockType =
            buildState.SelectedBlockType;

        plannedColor =
            buildState.SelectedColor;

        isPlanning = true;

        UpdatePlan(
            startVoxel
        );
    }

    private void UpdatePlacement()
    {
        if (!voxelRaycaster.TryGetVoxelHit(
                out _,
                out Vector3Int adjacentVoxel,
                out _))
        {
            return;
        }

        UpdatePlan(
            adjacentVoxel
        );
    }

    private void UpdatePlan(
        Vector3Int endVoxel)
    {
        List<Vector3Int> line =
            BlockPlacementLine.Calculate(
                startVoxel,
                endVoxel,
                maxBlocksPerLine
            );

        plannedBlocks.Clear();

        foreach (Vector3Int voxel in line)
        {
            if (placementService
                .CanPlaceBlock(voxel))
            {
                plannedBlocks.Add(
                    voxel
                );
            }
        }

        preview.Show(
            plannedBlocks,
            placementService.World
        );
    }

    private void FinishPlacement()
    {
        isPlanning = false;

        preview.Hide();

        if (plannedBlocks.Count == 0)
            return;

        List<Vector3Int> blocksToPlace =
            new List<Vector3Int>(
                plannedBlocks
            );

        plannedBlocks.Clear();

        StartCoroutine(
            PlaceBlocksSequentially(
                blocksToPlace,
                plannedBlockType,
                plannedColor
            )
        );
    }

    private IEnumerator
        PlaceBlocksSequentially(
            List<Vector3Int> blocks,
            BlockType blockType,
            VoxelColor color)
    {
        foreach (Vector3Int voxel in blocks)
        {
            placementService.TryPlaceBlock(
                voxel,
                blockType,
                color
            );

            if (placementInterval > 0f)
            {
                yield return
                    new WaitForSeconds(
                        placementInterval
                    );
            }
        }
    }
}