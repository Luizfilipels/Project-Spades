using System;

[Serializable]
public struct MapCoordinate
{
    public int x;
    public int y;
    public int z;

    public MapCoordinate(
        int x,
        int y,
        int z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }
}