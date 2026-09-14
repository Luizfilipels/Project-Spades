using System;

[Serializable]
public class MapZoneMetadata
{
    /*
     * spawn
     * base
     */
    public string kind =
        string.Empty;

    /*
     * blue
     * green
     * neutral
     */
    public string team =
        string.Empty;

    /*
     * small
     * medium
     * large
     *
     * Por enquanto preservamos o tamanho
     * sem transformar em GameObject ou Collider.
     */
    public string size =
        string.Empty;

    /*
     * Nome original da entidade.
     *
     * Ex:
     * ugc_spawnblue_large
     */
    public string sourceItem =
        string.Empty;

    /*
     * Coordenada já convertida para
     * Project Spades.
     */
    public MapCoordinate center;
}