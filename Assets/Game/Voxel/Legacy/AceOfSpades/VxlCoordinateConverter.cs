public static class VxlCoordinateConverter
{
    public const int MapWidth = 512;
    public const int MapDepth = 512;
    public const int MapHeight = 64;

    public static int ToWorldX(
        int aosX)
    {
        return aosX;
    }

    public static int ToWorldY(
        int aosZ)
    {
        return
            (MapHeight - 1)
            - aosZ;
    }

    public static int ToWorldZ(
        int aosY)
    {
        return aosY;
    }

    public static int ToAosX(
        int worldX)
    {
        return worldX;
    }

    public static int ToAosY(
        int worldZ)
    {
        return worldZ;
    }

    public static int ToAosZ(
        int worldY)
    {
        return
            (MapHeight - 1)
            - worldY;
    }
}