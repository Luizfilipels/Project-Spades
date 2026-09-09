using UnityEngine;

public static class ChunkMeshBuilder
{
    private static readonly Vector3Int[] NeighborDirections =
    {
        new Vector3Int(0, 1, 0),   // Top
        new Vector3Int(0, -1, 0),  // Bottom
        new Vector3Int(-1, 0, 0),  // Left
        new Vector3Int(1, 0, 0),   // Right
        new Vector3Int(0, 0, 1),   // Front
        new Vector3Int(0, 0, -1)   // Back
    };

    public static MeshData Build(
        ChunkData chunk,
        VoxelWorld world,
        Vector2Int chunkCoordinates)
    {
        MeshData meshData =
            new MeshData();

        for (int x = 0;
             x < ChunkData.SizeX;
             x++)
        {
            for (int y = 0;
                 y < ChunkData.SizeY;
                 y++)
            {
                for (int z = 0;
                     z < ChunkData.SizeZ;
                     z++)
                {
                    Block block =
                        chunk.GetBlock(
                            x,
                            y,
                            z
                        );

                    if (!block.IsSolid)
                        continue;

                    AddVisibleFaces(
                        world,
                        meshData,
                        chunkCoordinates,
                        x,
                        y,
                        z
                    );
                }
            }
        }

        return meshData;
    }

    private static void AddVisibleFaces(
        VoxelWorld world,
        MeshData meshData,
        Vector2Int chunkCoordinates,
        int localX,
        int localY,
        int localZ)
    {
        int worldX =
            chunkCoordinates.x *
            ChunkData.SizeX +
            localX;

        int worldZ =
            chunkCoordinates.y *
            ChunkData.SizeZ +
            localZ;

        for (int face = 0;
             face < NeighborDirections.Length;
             face++)
        {
            Vector3Int direction =
                NeighborDirections[face];

            int neighborWorldX =
                worldX + direction.x;

            int neighborWorldY =
                localY + direction.y;

            int neighborWorldZ =
                worldZ + direction.z;

            bool neighborIsSolid =
                world.IsBlockSolid(
                    neighborWorldX,
                    neighborWorldY,
                    neighborWorldZ
                );

            if (!neighborIsSolid)
            {
                AddFace(
                    meshData,
                    localX,
                    localY,
                    localZ,
                    face
                );
            }
        }
    }

    private static void AddFace(
        MeshData meshData,
        int x,
        int y,
        int z,
        int face)
    {
        int startVertex =
            meshData.Vertices.Count;

        Vector3 position =
            new Vector3(
                x,
                y,
                z
            );

        switch (face)
        {
            case 0:
                AddTopFace(
                    meshData,
                    position
                );
                break;

            case 1:
                AddBottomFace(
                    meshData,
                    position
                );
                break;

            case 2:
                AddLeftFace(
                    meshData,
                    position
                );
                break;

            case 3:
                AddRightFace(
                    meshData,
                    position
                );
                break;

            case 4:
                AddFrontFace(
                    meshData,
                    position
                );
                break;

            case 5:
                AddBackFace(
                    meshData,
                    position
                );
                break;
        }

        meshData.Triangles.Add(
            startVertex
        );

        meshData.Triangles.Add(
            startVertex + 1
        );

        meshData.Triangles.Add(
            startVertex + 2
        );

        meshData.Triangles.Add(
            startVertex
        );

        meshData.Triangles.Add(
            startVertex + 2
        );

        meshData.Triangles.Add(
            startVertex + 3
        );

        meshData.UVs.Add(
            new Vector2(0, 0)
        );

        meshData.UVs.Add(
            new Vector2(0, 1)
        );

        meshData.UVs.Add(
            new Vector2(1, 1)
        );

        meshData.UVs.Add(
            new Vector2(1, 0)
        );
    }

    private static void AddTopFace(
        MeshData meshData,
        Vector3 p)
    {
        meshData.Vertices.Add(
            p + new Vector3(0, 1, 0)
        );

        meshData.Vertices.Add(
            p + new Vector3(0, 1, 1)
        );

        meshData.Vertices.Add(
            p + new Vector3(1, 1, 1)
        );

        meshData.Vertices.Add(
            p + new Vector3(1, 1, 0)
        );
    }

    private static void AddBottomFace(
        MeshData meshData,
        Vector3 p)
    {
        meshData.Vertices.Add(
            p + new Vector3(0, 0, 1)
        );

        meshData.Vertices.Add(
            p + new Vector3(0, 0, 0)
        );

        meshData.Vertices.Add(
            p + new Vector3(1, 0, 0)
        );

        meshData.Vertices.Add(
            p + new Vector3(1, 0, 1)
        );
    }

    private static void AddLeftFace(
        MeshData meshData,
        Vector3 p)
    {
        meshData.Vertices.Add(
            p + new Vector3(0, 0, 0)
        );

        meshData.Vertices.Add(
            p + new Vector3(0, 0, 1)
        );

        meshData.Vertices.Add(
            p + new Vector3(0, 1, 1)
        );

        meshData.Vertices.Add(
            p + new Vector3(0, 1, 0)
        );
    }

    private static void AddRightFace(
        MeshData meshData,
        Vector3 p)
    {
        meshData.Vertices.Add(
            p + new Vector3(1, 0, 1)
        );

        meshData.Vertices.Add(
            p + new Vector3(1, 0, 0)
        );

        meshData.Vertices.Add(
            p + new Vector3(1, 1, 0)
        );

        meshData.Vertices.Add(
            p + new Vector3(1, 1, 1)
        );
    }

    private static void AddFrontFace(
        MeshData meshData,
        Vector3 p)
    {
        meshData.Vertices.Add(
            p + new Vector3(0, 0, 1)
        );

        meshData.Vertices.Add(
            p + new Vector3(1, 0, 1)
        );

        meshData.Vertices.Add(
            p + new Vector3(1, 1, 1)
        );

        meshData.Vertices.Add(
            p + new Vector3(0, 1, 1)
        );
    }

    private static void AddBackFace(
        MeshData meshData,
        Vector3 p)
    {
        meshData.Vertices.Add(
            p + new Vector3(1, 0, 0)
        );

        meshData.Vertices.Add(
            p + new Vector3(0, 0, 0)
        );

        meshData.Vertices.Add(
            p + new Vector3(0, 1, 0)
        );

        meshData.Vertices.Add(
            p + new Vector3(1, 1, 0)
        );
    }
}