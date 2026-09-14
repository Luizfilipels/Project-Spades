using System;
using System.Collections.Generic;

public enum JagexTeam
{
    Neutral = 0,
    Blue = 1,
    Green = 2
}

public enum JagexZoneKind
{
    Spawn = 0,
    Base = 1
}

public enum JagexZoneSize
{
    Small = 0,
    Medium = 1,
    Large = 2
}

[Serializable]
public class JagexGameplayZone
{
    public string mode =
        string.Empty;

    public string item =
        string.Empty;

    public JagexTeam team =
        JagexTeam.Neutral;

    public JagexZoneKind kind =
        JagexZoneKind.Spawn;

    public JagexZoneSize size =
        JagexZoneSize.Small;

    /*
     * Coordenada já convertida
     * para Project Spades.
     */
    public MapCoordinate center;

    /*
     * Coordenada original do .ugc.
     */
    public int rawX;
    public int rawY;
    public int rawZ;
}

[Serializable]
public class JagexGameplayPointEntity
{
    public string mode =
        string.Empty;

    public string item =
        string.Empty;

    /*
     * Ex:
     *
     * ammo
     * health
     * block
     * bomb
     */
    public string kind =
        string.Empty;

    public MapCoordinate position;

    public int rawX;
    public int rawY;
    public int rawZ;
}

[Serializable]
public class JagexGameplayIssue
{
    public string mode =
        string.Empty;

    public string item =
        string.Empty;

    public string reason =
        string.Empty;

    public int rawX;
    public int rawY;
    public int rawZ;
}

public sealed class JagexGameplayData
{
    public int SourceZShift { get; set; }

    public string[] SupportedGameModes
    {
        get;
        set;
    } = Array.Empty<string>();

    public List<JagexGameplayZone>
        Zones { get; } =
            new List<JagexGameplayZone>();

    public List<JagexGameplayPointEntity>
        PointEntities { get; } =
            new List<JagexGameplayPointEntity>();

    /*
     * Entidades conhecidas ou desconhecidas
     * que não conseguimos converter.
     *
     * Não descartamos silenciosamente nada.
     */
    public List<JagexGameplayIssue>
        Issues { get; } =
            new List<JagexGameplayIssue>();
}