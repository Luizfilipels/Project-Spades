using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(VoxelRaycaster))]
[RequireComponent(typeof(BlockPlacementPreview))]
public class BlockPlacer : MonoBehaviour
{
    [Header("Block")]
    [SerializeField]
    private BlockType blockType =
        BlockType.Dirt;

    [Header("Build Line")]
    [SerializeField]
    private int maxBlocksPerLine = 8;

    [SerializeField]
    private float placementInterval = 0.05f;

    private VoxelRaycaster voxelRaycaster;
    private BlockPlacementPreview preview;

    private bool isPlanning;

    private Vector3Int startVoxel;

    private readonly List<Vector3Int>
        plannedBlocks =
            new List<Vector3Int>();

    private void Awake()
    {
        voxelRaycaster =
            GetComponent<VoxelRaycaster>();

        preview =
            GetComponent<BlockPlacementPreview>();
    }

    private void Update()
    {
        if (Mouse.current == null)
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

        startVoxel = adjacentVoxel;

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
            if (!voxelRaycaster.World
                .IsBlockSolid(
                    voxel.x,
                    voxel.y,
                    voxel.z))
            {
                plannedBlocks.Add(
                    voxel
                );
            }
        }

        preview.Show(
            plannedBlocks,
            voxelRaycaster.World
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
                blocksToPlace
            )
        );
    }

    private IEnumerator PlaceBlocksSequentially(
        List<Vector3Int> blocks)
    {
        foreach (Vector3Int voxel in blocks)
        {
            if (!voxelRaycaster.World
                .IsBlockSolid(
                    voxel.x,
                    voxel.y,
                    voxel.z))
            {
                voxelRaycaster.World.SetBlock(
                    voxel.x,
                    voxel.y,
                    voxel.z,
                    new Block(blockType)
                );
            }

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