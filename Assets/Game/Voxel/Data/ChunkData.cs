public class ChunkData
{
    public const int SizeX = 16;
    public const int SizeY = 64;
    public const int SizeZ = 16;

    private readonly Block[] blocks;

    public ChunkData()
    {
        blocks = new Block[SizeX * SizeY * SizeZ];
    }

    private int GetIndex(int x, int y, int z)
    {
        return x +
               z * SizeX +
               y * SizeX * SizeZ;
    }

    public Block GetBlock(int x, int y, int z)
    {
        return blocks[GetIndex(x, y, z)];
    }

    public void SetBlock(int x, int y, int z, Block block)
    {
        blocks[GetIndex(x, y, z)] = block;
    }
}