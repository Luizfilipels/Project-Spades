using System;

[Serializable]
public class MapGameplayMetadata
{
    // ============================================================
    // GENERAL
    // ============================================================

    public string defaultGameMode =
        "ctf";

    public string[] supportedGameModes =
        new string[]
        {
            "ctf"
        };

    // ============================================================
    // LEGACY / SIMPLE TEAM DATA
    // ============================================================

    /*
     * Mantemos este campo por compatibilidade
     * com:
     *
     * - mapas nativos antigos;
     * - PySnip;
     * - Bikini Bottom;
     * - Hallway;
     *
     * Portanto NÃO remover.
     */
    public MapTeamMetadata[] teams =
        Array.Empty<MapTeamMetadata>();

    // ============================================================
    // MULTI-MODE GAMEPLAY
    // ============================================================

    /*
     * Estrutura nova.
     *
     * Cada modo pode possuir suas próprias:
     *
     * - spawn zones;
     * - base zones;
     * - neutral zones;
     * - entidades específicas.
     */
    public MapGameModeMetadata[] modes =
        Array.Empty<MapGameModeMetadata>();

    // ============================================================
    // COMMON ENTITIES
    // ============================================================

    /*
     * Entidades marcadas como "nor" no UGC.
     *
     * São compartilhadas entre os modos.
     *
     * Ex:
     * health
     * ammo
     * block
     */
    public MapPointEntityMetadata[] commonEntities =
        Array.Empty<MapPointEntityMetadata>();
}