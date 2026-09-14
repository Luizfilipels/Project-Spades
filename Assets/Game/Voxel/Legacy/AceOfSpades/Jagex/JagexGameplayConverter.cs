using System;
using System.Collections.Generic;
using System.IO;

public static class JagexGameplayConverter
{
    private const string CommonMode =
        "nor";

    public static JagexGameplayData Convert(
        JagexUgcData ugc,
        string vxlFilePath)
    {
        if (ugc == null)
        {
            throw new ArgumentNullException(
                nameof(ugc)
            );
        }

        if (string.IsNullOrWhiteSpace(
                vxlFilePath))
        {
            throw new ArgumentException(
                "Caminho VXL inválido.",
                nameof(vxlFilePath)
            );
        }

        if (!File.Exists(
                vxlFilePath))
        {
            throw new FileNotFoundException(
                "Arquivo VXL não encontrado.",
                vxlFilePath
            );
        }

        JagexGameplayData result =
            new JagexGameplayData();

        result.SourceZShift =
            JagexCoordinateConverter
                .DetectSourceZShift(
                    vxlFilePath
                );

        result.SupportedGameModes =
            JagexUgcImporter.GetModes(
                ugc
            );

        if (ugc.ugc_entities == null)
        {
            return result;
        }

        foreach (JagexUgcEntity entity
                 in ugc.ugc_entities)
        {
            ConvertEntity(
                entity,
                result
            );
        }

        return result;
    }

    // ============================================================
    // ENTITY
    // ============================================================

    private static void ConvertEntity(
        JagexUgcEntity entity,
        JagexGameplayData result)
    {
        if (entity == null)
        {
            AddIssue(
                result,
                null,
                "Entidade UGC nula."
            );

            return;
        }

        if (entity.position == null ||
            entity.position.Length != 3)
        {
            AddIssue(
                result,
                entity,
                "Posição UGC inválida."
            );

            return;
        }

        string item =
            Normalize(
                entity.item
            );

        string mode =
            Normalize(
                entity.mode
            );

        if (string.IsNullOrEmpty(
                mode))
        {
            mode =
                CommonMode;
        }

        JagexZoneKind zoneKind;
        JagexTeam team;
        JagexZoneSize zoneSize;

        if (TryParseZone(
                item,
                out zoneKind,
                out team,
                out zoneSize))
        {
            ConvertZone(
                entity,
                item,
                mode,
                zoneKind,
                team,
                zoneSize,
                result
            );

            return;
        }

        string pointKind;

        if (TryParsePointEntity(
                item,
                out pointKind))
        {
            ConvertPointEntity(
                entity,
                item,
                mode,
                pointKind,
                result
            );

            return;
        }

        AddIssue(
            result,
            entity,
            "Tipo de entidade UGC ainda " +
            "não suportado."
        );
    }

    // ============================================================
    // ZONES
    // ============================================================

    private static void ConvertZone(
        JagexUgcEntity entity,
        string item,
        string mode,
        JagexZoneKind kind,
        JagexTeam team,
        JagexZoneSize size,
        JagexGameplayData result)
    {
        try
        {
            MapCoordinate coordinate =
                JagexCoordinateConverter
                    .ToWorldCoordinate(
                        entity,
                        result.SourceZShift
                    );

            JagexGameplayZone zone =
                new JagexGameplayZone();

            zone.mode =
                mode;

            zone.item =
                item;

            zone.kind =
                kind;

            zone.team =
                team;

            zone.size =
                size;

            zone.center =
                coordinate;

            zone.rawX =
                entity.position[0];

            zone.rawY =
                entity.position[1];

            zone.rawZ =
                entity.position[2];

            result.Zones.Add(
                zone
            );
        }
        catch (Exception exception)
        {
            AddIssue(
                result,
                entity,
                "Falha ao converter zona: " +
                exception.Message
            );
        }
    }

    private static bool TryParseZone(
        string item,
        out JagexZoneKind kind,
        out JagexTeam team,
        out JagexZoneSize size)
    {
        kind =
            JagexZoneKind.Spawn;

        team =
            JagexTeam.Neutral;

        size =
            JagexZoneSize.Small;

        if (TryParseSize(
                item,
                out size) == false)
        {
            return false;
        }

        // --------------------------------------------------------
        // BLUE SPAWN
        // --------------------------------------------------------

        if (StartsWith(
                item,
                "ugc_spawnblue_"))
        {
            kind =
                JagexZoneKind.Spawn;

            team =
                JagexTeam.Blue;

            return true;
        }

        // --------------------------------------------------------
        // GREEN SPAWN
        // --------------------------------------------------------

        if (StartsWith(
                item,
                "ugc_spawngreen_"))
        {
            kind =
                JagexZoneKind.Spawn;

            team =
                JagexTeam.Green;

            return true;
        }

        // --------------------------------------------------------
        // BLUE BASE
        // --------------------------------------------------------

        if (StartsWith(
                item,
                "ugc_baseblue_"))
        {
            kind =
                JagexZoneKind.Base;

            team =
                JagexTeam.Blue;

            return true;
        }

        // --------------------------------------------------------
        // GREEN BASE
        // --------------------------------------------------------

        if (StartsWith(
                item,
                "ugc_basegreen_"))
        {
            kind =
                JagexZoneKind.Base;

            team =
                JagexTeam.Green;

            return true;
        }

        // --------------------------------------------------------
        // NEUTRAL BASE
        //
        // Ex:
        // ugc_base_small
        // ugc_base_med
        // ugc_base_large
        // --------------------------------------------------------

        if (StartsWith(
                item,
                "ugc_base_"))
        {
            kind =
                JagexZoneKind.Base;

            team =
                JagexTeam.Neutral;

            return true;
        }

        return false;
    }

    private static bool TryParseSize(
        string item,
        out JagexZoneSize size)
    {
        size =
            JagexZoneSize.Small;

        if (EndsWith(
                item,
                "_small"))
        {
            size =
                JagexZoneSize.Small;

            return true;
        }

        if (EndsWith(
                item,
                "_med") ||
            EndsWith(
                item,
                "_medium"))
        {
            size =
                JagexZoneSize.Medium;

            return true;
        }

        if (EndsWith(
                item,
                "_large"))
        {
            size =
                JagexZoneSize.Large;

            return true;
        }

        return false;
    }

    // ============================================================
    // POINT ENTITIES
    // ============================================================

    private static bool TryParsePointEntity(
        string item,
        out string kind)
    {
        kind =
            string.Empty;

        switch (item)
        {
            case "ugc_ammo_drop":
                kind =
                    "ammo";
                return true;

            case "ugc_health_drop":
                kind =
                    "health";
                return true;

            case "ugc_block_drop":
                kind =
                    "block";
                return true;

            case "ugc_bomb_drop":
                kind =
                    "bomb";
                return true;
        }

        return false;
    }

    private static void ConvertPointEntity(
        JagexUgcEntity entity,
        string item,
        string mode,
        string kind,
        JagexGameplayData result)
    {
        try
        {
            MapCoordinate coordinate =
                JagexCoordinateConverter
                    .ToWorldCoordinate(
                        entity,
                        result.SourceZShift
                    );

            JagexGameplayPointEntity point =
                new JagexGameplayPointEntity();

            point.mode =
                mode;

            point.item =
                item;

            point.kind =
                kind;

            point.position =
                coordinate;

            point.rawX =
                entity.position[0];

            point.rawY =
                entity.position[1];

            point.rawZ =
                entity.position[2];

            result.PointEntities.Add(
                point
            );
        }
        catch (Exception exception)
        {
            AddIssue(
                result,
                entity,
                "Falha ao converter entidade: " +
                exception.Message
            );
        }
    }

    // ============================================================
    // ISSUES
    // ============================================================

    private static void AddIssue(
        JagexGameplayData result,
        JagexUgcEntity entity,
        string reason)
    {
        JagexGameplayIssue issue =
            new JagexGameplayIssue();

        issue.reason =
            reason;

        if (entity != null)
        {
            issue.mode =
                Normalize(
                    entity.mode
                );

            issue.item =
                Normalize(
                    entity.item
                );

            if (entity.position != null &&
                entity.position.Length == 3)
            {
                issue.rawX =
                    entity.position[0];

                issue.rawY =
                    entity.position[1];

                issue.rawZ =
                    entity.position[2];
            }
        }

        result.Issues.Add(
            issue
        );
    }

    // ============================================================
    // HELPERS
    // ============================================================

    private static string Normalize(
        string value)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            return string.Empty;
        }

        return value
            .Trim()
            .ToLowerInvariant();
    }

    private static bool StartsWith(
        string value,
        string prefix)
    {
        return value.StartsWith(
            prefix,
            StringComparison.OrdinalIgnoreCase
        );
    }

    private static bool EndsWith(
        string value,
        string suffix)
    {
        return value.EndsWith(
            suffix,
            StringComparison.OrdinalIgnoreCase
        );
    }
}