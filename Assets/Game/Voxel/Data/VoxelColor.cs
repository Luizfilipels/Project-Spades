public struct VoxelColor
{
    public byte R;
    public byte G;
    public byte B;
    public byte A;

    public VoxelColor(
        byte r,
        byte g,
        byte b,
        byte a = 255)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }
}