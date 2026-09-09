using UnityEngine;

public static class WorldGenerator
{
    private const float TerrainScale = 0.025f;

    private const int BaseHeight = 12;
    private const int HeightAmplitude = 24;

    private const int Seed = 12345;

    public static void GenerateFlat(ChunkData chunk)
    {
        for (int x = 0; x < ChunkData.SizeX; x++)
        {
            for (int z = 0; z < ChunkData.SizeZ; z++)
            {
                for (int y = 0; y < ChunkData.SizeY; y++)
                {
                    BlockType type;

                    if (y < 15)
                    {
                        type = BlockType.Stone;
                    }
                    else if (y < 19)
                    {
                        type = BlockType.Dirt;
                    }
                    else if (y == 19)
                    {
                        type = BlockType.Grass;
                    }
                    else
                    {
                        type = BlockType.Air;
                    }

                    chunk.SetBlock(
                        x,
                        y,
                        z,
                        new Block(type)
                    );
                }
            }
        }
    }

    public static void GenerateTerrain(
        ChunkData chunk,
        int chunkX,
        int chunkZ)
    {
        for (int localX = 0;
             localX < ChunkData.SizeX;
             localX++)
        {
            for (int localZ = 0;
                 localZ < ChunkData.SizeZ;
                 localZ++)
            {
                int worldX =
                    chunkX * ChunkData.SizeX
                    + localX;

                int worldZ =
                    chunkZ * ChunkData.SizeZ
                    + localZ;

                int terrainHeight =
                    GetTerrainHeight(
                        worldX,
                        worldZ
                    );

                for (int y = 0;
                     y < ChunkData.SizeY;
                     y++)
                {
                    BlockType type =
                        GetBlockType(
                            y,
                            terrainHeight
                        );

                    chunk.SetBlock(
                        localX,
                        y,
                        localZ,
                        new Block(type)
                    );
                }
            }
        }
    }

    private static int GetTerrainHeight(
        int worldX,
        int worldZ)
    {
        float offsetX =
            Seed * 0.1234f;

        float offsetZ =
            Seed * 0.5678f;

        float noise =
            Mathf.PerlinNoise(
                worldX * TerrainScale
                    + offsetX,

                worldZ * TerrainScale
                    + offsetZ
            );

        int height =
            BaseHeight +
            Mathf.FloorToInt(
                noise * HeightAmplitude
            );

        return Mathf.Clamp(
            height,
            1,
            ChunkData.SizeY - 1
        );
    }

    private static BlockType GetBlockType(
        int y,
        int terrainHeight)
    {
        if (y > terrainHeight)
        {
            return BlockType.Air;
        }

        if (y == terrainHeight)
        {
            return BlockType.Grass;
        }

        if (y >= terrainHeight - 4)
        {
            return BlockType.Dirt;
        }

        return BlockType.Stone;
    }
}