public struct Block
{
    public BlockType Type;

    public Block(BlockType type)
    {
        Type = type;
    }

    public bool IsSolid
    {
        get
        {
            return Type != BlockType.Air;
        }
    }
}