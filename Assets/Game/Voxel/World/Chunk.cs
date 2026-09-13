using UnityEngine;

[RequireComponent(typeof(ChunkRenderer))]
public class Chunk : MonoBehaviour
{
    private ChunkData data;
    private ChunkRenderer chunkRenderer;
    private VoxelWorld world;

    public Vector2Int Coordinates
    {
        get;
        private set;
    }

    public ChunkData Data =>
        data;

    private void Awake()
    {
        chunkRenderer =
            GetComponent<ChunkRenderer>();
    }

    public void Initialize(
        VoxelWorld voxelWorld,
        int chunkX,
        int chunkZ)
    {
        Initialize(
            voxelWorld,
            chunkX,
            chunkZ,
            true
        );
    }

    public void Initialize(
        VoxelWorld voxelWorld,
        int chunkX,
        int chunkZ,
        bool generateTerrain)
    {
        world =
            voxelWorld;

        Coordinates =
            new Vector2Int(
                chunkX,
                chunkZ
            );

        data =
            new ChunkData(
                voxelWorld.SizeY
            );

        if (generateTerrain)
        {
            WorldGenerator.GenerateTerrain(
                data,
                chunkX,
                chunkZ
            );
        }
    }

    public void RebuildMesh()
    {
        if (data == null ||
            world == null)
        {
            return;
        }

        MeshData meshData =
            ChunkMeshBuilder.Build(
                data,
                world,
                Coordinates
            );

        chunkRenderer.ApplyMesh(
            meshData
        );
    }
}