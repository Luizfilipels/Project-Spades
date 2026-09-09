using UnityEngine;

public class BlockPlacementService :
    MonoBehaviour
{
    [SerializeField]
    private VoxelWorld world;

    public VoxelWorld World =>
        world;

    private void Awake()
    {
        if (world == null)
        {
            world =
                FindAnyObjectByType<VoxelWorld>();
        }
    }

    public bool CanPlaceBlock(
        Vector3Int voxel)
    {
        if (world == null)
            return false;

        return !world.IsBlockSolid(
            voxel.x,
            voxel.y,
            voxel.z
        );
    }

    public bool TryPlaceBlock(
        Vector3Int voxel,
        BlockType blockType,
        VoxelColor color)
    {
        if (world == null)
            return false;

        if (blockType == BlockType.Air)
            return false;

        if (!CanPlaceBlock(voxel))
            return false;

        return world.SetBlock(
            voxel.x,
            voxel.y,
            voxel.z,
            new Block(
                blockType,
                color
            )
        );
    }
}