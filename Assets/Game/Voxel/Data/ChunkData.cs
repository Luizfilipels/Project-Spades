using System;

public class ChunkData
{
    public const int SizeX = 16;
    public const int SizeZ = 16;

    public const int DefaultSizeY = 64;
    public const int MaxSizeY = 256;

    public int SizeY { get; }

    private readonly Block[] blocks;

    public ChunkData()
        : this(DefaultSizeY)
    {
    }

    public ChunkData(int sizeY)
    {
        if (sizeY <= 0 ||
            sizeY > MaxSizeY)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sizeY),
                $"A altura do chunk deve estar entre " +
                $"1 e {MaxSizeY}."
            );
        }

        SizeY = sizeY;

        blocks =
            new Block[
                SizeX *
                SizeY *
                SizeZ
            ];
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
                $"Voxel local fora do chunk: " +
                $"({x}, {y}, {z})"
            );
        }
    }
}