using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(VoxelRaycaster))]
public class BlockPlacer : MonoBehaviour
{
    [Header("Block")]
    [SerializeField]
    private BlockType blockType = BlockType.Dirt;

    private VoxelRaycaster voxelRaycaster;

    private void Awake()
    {
        voxelRaycaster =
            GetComponent<VoxelRaycaster>();
    }

    private void Update()
    {
        if (Mouse.current == null)
            return;

        if (!Mouse.current.rightButton.wasPressedThisFrame)
            return;

        TryPlaceBlock();
    }

    private void TryPlaceBlock()
    {
        if (!voxelRaycaster.TryGetVoxelHit(
                out _,
                out Vector3Int adjacentVoxel,
                out _))
        {
            return;
        }

        VoxelWorld world =
            voxelRaycaster.World;

        // Não coloca um bloco onde já existe outro.
        if (world.IsBlockSolid(
                adjacentVoxel.x,
                adjacentVoxel.y,
                adjacentVoxel.z))
        {
            return;
        }

        bool placed =
            world.SetBlock(
                adjacentVoxel.x,
                adjacentVoxel.y,
                adjacentVoxel.z,
                new Block(blockType)
            );

        if (placed)
        {
            Debug.Log(
                $"Voxel colocado: {adjacentVoxel} | {blockType}"
            );
        }
    }
}