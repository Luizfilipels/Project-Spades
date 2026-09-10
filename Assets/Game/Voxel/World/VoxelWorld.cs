using System;
using System.Collections.Generic;
using UnityEngine;

public class VoxelWorld : MonoBehaviour
{
    [Header("World Size")]
    [SerializeField]
    private int chunksX = 8;

    [SerializeField]
    private int chunksZ = 8;

    [Header("Rendering")]
    [SerializeField]
    private Material voxelMaterial;

    private int worldSizeX;
    private int worldSizeY;
    private int worldSizeZ;

    private readonly Dictionary<Vector2Int, Chunk>
        chunks =
            new Dictionary<Vector2Int, Chunk>();

    private readonly HashSet<Vector2Int>
        dirtyChunks =
            new HashSet<Vector2Int>();

    public int SizeX => worldSizeX;
    public int SizeY => worldSizeY;
    public int SizeZ => worldSizeZ;

    private void Start()
    {
        GenerateWorld();
    }

    private void LateUpdate()
    {
        RebuildDirtyChunks();
    }

    private void GenerateWorld()
    {
        worldSizeX =
            chunksX * ChunkData.SizeX;

        worldSizeY =
            ChunkData.SizeY;

        worldSizeZ =
            chunksZ * ChunkData.SizeZ;

        CreateAllChunks(
            true
        );

        BuildAllChunkMeshes();
    }

    private void CreateAllChunks(
        bool generateTerrain)
    {
        for (int x = 0;
             x < chunksX;
             x++)
        {
            for (int z = 0;
                 z < chunksZ;
                 z++)
            {
                CreateChunk(
                    x,
                    z,
                    generateTerrain
                );
            }
        }
    }

    private void BuildAllChunkMeshes()
    {
        foreach (Chunk chunk
                 in chunks.Values)
        {
            chunk.RebuildMesh();
        }
    }

    private void CreateChunk(
        int chunkX,
        int chunkZ,
        bool generateTerrain)
    {
        GameObject chunkObject =
            new GameObject(
                $"Chunk_{chunkX}_{chunkZ}"
            );

        chunkObject.transform.SetParent(
            transform,
            false
        );

        chunkObject.transform.localPosition =
            new Vector3(
                chunkX *
                ChunkData.SizeX,

                0,

                chunkZ *
                ChunkData.SizeZ
            );

        Chunk chunk =
            chunkObject.AddComponent<Chunk>();

        MeshRenderer meshRenderer =
            chunkObject.GetComponent<
                MeshRenderer>();

        meshRenderer.sharedMaterial =
            voxelMaterial;

        Vector2Int coordinates =
            new Vector2Int(
                chunkX,
                chunkZ
            );

        chunks.Add(
            coordinates,
            chunk
        );

        chunk.Initialize(
            this,
            chunkX,
            chunkZ,
            generateTerrain
        );
    }

    public Block GetBlock(
        int worldX,
        int worldY,
        int worldZ)
    {
        if (!TryGetChunkAndLocalCoordinates(
                worldX,
                worldY,
                worldZ,
                out Chunk chunk,
                out int localX,
                out int localZ))
        {
            return new Block(
                BlockType.Air
            );
        }

        return chunk.Data.GetBlock(
            localX,
            worldY,
            localZ
        );
    }

    public bool IsBlockSolid(
        int worldX,
        int worldY,
        int worldZ)
    {
        return GetBlock(
            worldX,
            worldY,
            worldZ
        ).IsSolid;
    }

    public bool SetBlock(
        int worldX,
        int worldY,
        int worldZ,
        Block block)
    {
        if (!TryGetChunkAndLocalCoordinates(
                worldX,
                worldY,
                worldZ,
                out Chunk chunk,
                out int localX,
                out int localZ))
        {
            return false;
        }

        chunk.Data.SetBlock(
            localX,
            worldY,
            localZ,
            block
        );

        Vector2Int chunkCoordinates =
            chunk.Coordinates;

        MarkChunkDirty(
            chunkCoordinates
        );

        CheckNeighborChunks(
            chunkCoordinates,
            localX,
            localZ
        );

        return true;
    }

    public bool RemoveBlock(
        int worldX,
        int worldY,
        int worldZ)
    {
        return SetBlock(
            worldX,
            worldY,
            worldZ,
            new Block(
                BlockType.Air
            )
        );
    }

    public VoxelMapData CaptureMapData()
    {
        if (worldSizeX <= 0 ||
            worldSizeY <= 0 ||
            worldSizeZ <= 0)
        {
            throw new InvalidOperationException(
                "O mundo ainda não foi inicializado."
            );
        }

        VoxelMapData map =
            new VoxelMapData(
                worldSizeX,
                worldSizeY,
                worldSizeZ
            );

        for (int z = 0;
             z < worldSizeZ;
             z++)
        {
            for (int x = 0;
                 x < worldSizeX;
                 x++)
            {
                for (int y = 0;
                     y < worldSizeY;
                     y++)
                {
                    map.SetBlock(
                        x,
                        y,
                        z,
                        GetBlock(
                            x,
                            y,
                            z
                        )
                    );
                }
            }
        }

        return map;
    }

    public void LoadMapData(
        VoxelMapData map)
    {
        if (map == null)
        {
            throw new ArgumentNullException(
                nameof(map)
            );
        }

        if (map.SizeY >
            ChunkData.SizeY)
        {
            throw new InvalidOperationException(
                $"O mapa possui altura {map.SizeY}, " +
                $"mas a engine atualmente suporta " +
                $"até {ChunkData.SizeY}."
            );
        }

        ClearWorld();

        worldSizeX =
            map.SizeX;

        worldSizeY =
            map.SizeY;

        worldSizeZ =
            map.SizeZ;

        chunksX =
            Mathf.CeilToInt(
                worldSizeX /
                (float)ChunkData.SizeX
            );

        chunksZ =
            Mathf.CeilToInt(
                worldSizeZ /
                (float)ChunkData.SizeZ
            );

        CreateAllChunks(
            false
        );

        for (int z = 0;
             z < worldSizeZ;
             z++)
        {
            for (int x = 0;
                 x < worldSizeX;
                 x++)
            {
                for (int y = 0;
                     y < worldSizeY;
                     y++)
                {
                    SetBlockDirect(
                        x,
                        y,
                        z,
                        map.GetBlock(
                            x,
                            y,
                            z
                        )
                    );
                }
            }
        }

        BuildAllChunkMeshes();
    }

    private void SetBlockDirect(
        int worldX,
        int worldY,
        int worldZ,
        Block block)
    {
        if (!TryGetChunkAndLocalCoordinates(
                worldX,
                worldY,
                worldZ,
                out Chunk chunk,
                out int localX,
                out int localZ))
        {
            return;
        }

        chunk.Data.SetBlock(
            localX,
            worldY,
            localZ,
            block
        );
    }

    private void ClearWorld()
    {
        foreach (Chunk chunk
                 in chunks.Values)
        {
            if (chunk == null)
                continue;

            chunk.gameObject.SetActive(
                false
            );

            Destroy(
                chunk.gameObject
            );
        }

        chunks.Clear();
        dirtyChunks.Clear();
    }

    private bool
        TryGetChunkAndLocalCoordinates(
            int worldX,
            int worldY,
            int worldZ,
            out Chunk chunk,
            out int localX,
            out int localZ)
    {
        chunk = null;
        localX = 0;
        localZ = 0;

        if (worldX < 0 ||
            worldX >= worldSizeX)
        {
            return false;
        }

        if (worldY < 0 ||
            worldY >= worldSizeY)
        {
            return false;
        }

        if (worldZ < 0 ||
            worldZ >= worldSizeZ)
        {
            return false;
        }

        int chunkX =
            worldX /
            ChunkData.SizeX;

        int chunkZ =
            worldZ /
            ChunkData.SizeZ;

        Vector2Int coordinates =
            new Vector2Int(
                chunkX,
                chunkZ
            );

        if (!chunks.TryGetValue(
                coordinates,
                out chunk))
        {
            return false;
        }

        localX =
            worldX %
            ChunkData.SizeX;

        localZ =
            worldZ %
            ChunkData.SizeZ;

        return true;
    }

    private void CheckNeighborChunks(
        Vector2Int chunkCoordinates,
        int localX,
        int localZ)
    {
        if (localX == 0)
        {
            MarkChunkDirty(
                new Vector2Int(
                    chunkCoordinates.x - 1,
                    chunkCoordinates.y
                )
            );
        }

        if (localX ==
            ChunkData.SizeX - 1)
        {
            MarkChunkDirty(
                new Vector2Int(
                    chunkCoordinates.x + 1,
                    chunkCoordinates.y
                )
            );
        }

        if (localZ == 0)
        {
            MarkChunkDirty(
                new Vector2Int(
                    chunkCoordinates.x,
                    chunkCoordinates.y - 1
                )
            );
        }

        if (localZ ==
            ChunkData.SizeZ - 1)
        {
            MarkChunkDirty(
                new Vector2Int(
                    chunkCoordinates.x,
                    chunkCoordinates.y + 1
                )
            );
        }
    }

    private void MarkChunkDirty(
        Vector2Int coordinates)
    {
        if (!chunks.ContainsKey(
                coordinates))
        {
            return;
        }

        dirtyChunks.Add(
            coordinates
        );
    }

    private void RebuildDirtyChunks()
    {
        if (dirtyChunks.Count == 0)
            return;

        foreach (
            Vector2Int coordinates
            in dirtyChunks)
        {
            if (chunks.TryGetValue(
                    coordinates,
                    out Chunk chunk))
            {
                chunk.RebuildMesh();
            }
        }

        dirtyChunks.Clear();
    }
}