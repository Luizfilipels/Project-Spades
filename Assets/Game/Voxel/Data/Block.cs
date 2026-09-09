public struct Block
{
    public BlockType Type;
    public VoxelColor Color;

    public Block(BlockType type)
    {
        Type = type;
        Color = GetDefaultColor(type);
    }

    public Block(
        BlockType type,
        VoxelColor color)
    {
        Type = type;
        Color = color;
    }

    public bool IsSolid
    {
        get
        {
            return Type != BlockType.Air;
        }
    }

    private static VoxelColor GetDefaultColor(
        BlockType type)
    {
        switch (type)
        {
            case BlockType.Grass:
                return new VoxelColor(
                    86,
                    140,
                    72
                );

            case BlockType.Dirt:
                return new VoxelColor(
                    125,
                    88,
                    58
                );

            case BlockType.Stone:
                return new VoxelColor(
                    120,
                    120,
                    125
                );

            default:
                return new VoxelColor(
                    0,
                    0,
                    0,
                    0
                );
        }
    }
}