using System;

[Serializable]
public class MapPointEntityMetadata
{
    /*
     * ammo
     * health
     * block
     * bomb
     */
    public string kind =
        string.Empty;

    /*
     * Nome original da entidade UGC.
     */
    public string sourceItem =
        string.Empty;

    public MapCoordinate position;
}