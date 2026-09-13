public static class VxlCoordinateConverter
{
    public const int MapWidth = 512;
    public const int MapDepth = 512;

    public const int ClassicMapHeight = 64;
    public const int RetailMapHeight = 240;

    public static int ToWorldX(
        int aosX)
    {
        return aosX;
    }

    public static int ToWorldY(
        int aosZ,
        int mapHeight)
    {
        return
            (mapHeight - 1) -
            aosZ;
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
        int worldY,
        int mapHeight)
    {
        return
            (mapHeight - 1) -
            worldY;
    }
}