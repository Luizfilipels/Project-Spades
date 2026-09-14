using System;

[Serializable]
public class MapGameModeMetadata
{
    /*
     * ctf
     * tdm
     * dem
     * tc
     * mh
     * oc
     * vip
     * zom
     * dia
     */
    public string id =
        string.Empty;

    /*
     * Zonas específicas desse modo.
     *
     * Exemplos:
     * Blue spawn
     * Green spawn
     * Blue base
     * Neutral base
     */
    public MapZoneMetadata[] zones =
        Array.Empty<MapZoneMetadata>();

    /*
     * Entidades pontuais específicas
     * desse modo.
     */
    public MapPointEntityMetadata[] entities =
        Array.Empty<MapPointEntityMetadata>();
}