using System;

[Serializable]
public class MapTeamMetadata
{
    // ============================================================
    // TEAM
    // ============================================================

    public string id =
        string.Empty;

    public string displayName =
        string.Empty;

    // ============================================================
    // SPAWN
    // ============================================================

    /*
     * Define como o servidor deve determinar
     * os pontos de spawn desse time.
     */
    public MapLocationPolicy spawnPolicy =
        MapLocationPolicy.MapDefined;

    /*
     * Usado quando spawnPolicy == MapDefined.
     *
     * Quando a política for ServerDefault,
     * essa lista pode estar vazia.
     */
    public MapCoordinate[] spawnPoints =
        Array.Empty<MapCoordinate>();

    // ============================================================
    // BASE
    // ============================================================

    public MapLocationPolicy basePolicy =
        MapLocationPolicy.MapDefined;

    /*
     * Mantemos hasBase por compatibilidade
     * com o sistema de metadata que já existe.
     *
     * MapDefined + hasBase = true
     * significa que basePosition é válida.
     */
    public bool hasBase = false;

    public MapCoordinate basePosition;

    // ============================================================
    // FLAG
    // ============================================================

    public MapLocationPolicy flagPolicy =
        MapLocationPolicy.MapDefined;

    public bool hasFlag = false;

    public MapCoordinate flagPosition;
}