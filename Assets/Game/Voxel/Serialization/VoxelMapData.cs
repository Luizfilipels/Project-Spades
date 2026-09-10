using System;

public sealed class VoxelMapData
{
    private readonly Block[] blocks;

    public int SizeX { get; }
    public int SizeY { get; }
    public int SizeZ { get; }

    public int VoxelCount =>
        blocks.Length;

    public VoxelMapData(
        int sizeX,
        int sizeY,
        int sizeZ)
    {
        if (sizeX <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(sizeX)
            );

        if (sizeY <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(sizeY)
            );

        if (sizeZ <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(sizeZ)
            );

        long voxelCount =
            (long)sizeX *
            sizeY *
            sizeZ;

        if (voxelCount > int.MaxValue)
        {
            throw new ArgumentException(
                "O mapa possui voxels demais."
            );
        }

        SizeX = sizeX;
        SizeY = sizeY;
        SizeZ = sizeZ;

        blocks =
            new Block[(int)voxelCount];
    }

    public Block GetBlock(
        int x,
        int y,
        int z)
    {
        ValidateCoordinates(
            x,
            y,
            z
        );

        return blocks[
            GetIndex(x, y, z)
        ];
    }

    public void SetBlock(
        int x,
        int y,
        int z,
        Block block)
    {
        ValidateCoordinates(
            x,
            y,
            z
        );

        blocks[
            GetIndex(x, y, z)
        ] = block;
    }

    private int GetIndex(
        int x,
        int y,
        int z)
    {
        return
            x +
            z * SizeX +
            y * SizeX * SizeZ;
    }

    private void ValidateCoordinates(
        int x,
        int y,
        int z)
    {
        if (x < 0 ||
            x >= SizeX ||
            y < 0 ||
            y >= SizeY ||
            z < 0 ||
            z >= SizeZ)
        {
            throw new ArgumentOutOfRangeException(
                $"Voxel fora do mapa: ({x}, {y}, {z})"
            );
        }
    }
}