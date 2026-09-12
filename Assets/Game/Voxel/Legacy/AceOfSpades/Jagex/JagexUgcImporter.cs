using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class JagexUgcImporter
{
    private const long MaxFileSize =
        16 * 1024 * 1024;

    private const int MaxEntities =
        8192;

    private const int WorldSizeXY =
        512;

    private const int UgcHeight =
        256;

    public static JagexUgcData Load(
        string filePath)
    {
        if (string.IsNullOrWhiteSpace(
                filePath))
        {
            throw new ArgumentException(
                "Caminho UGC inválido.",
                nameof(filePath)
            );
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                "Arquivo UGC não encontrado.",
                filePath
            );
        }

        FileInfo fileInfo =
            new FileInfo(filePath);

        if (fileInfo.Length >
            MaxFileSize)
        {
            throw new InvalidDataException(
                "Arquivo UGC excede o " +
                "limite de segurança."
            );
        }

        string json =
            File.ReadAllText(
                filePath
            );

        JagexUgcData data =
            JsonUtility.FromJson<
                JagexUgcData>(json);

        if (data == null)
        {
            throw new InvalidDataException(
                "Não foi possível interpretar " +
                "o arquivo UGC."
            );
        }

        Normalize(data);
        Validate(data);

        return data;
    }

    public static string[] GetModes(
        JagexUgcData data)
    {
        HashSet<string> modes =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase
            );

        if (data?.tags != null)
        {
            foreach (string tag in data.tags)
            {
                if (string.IsNullOrWhiteSpace(
                        tag))
                {
                    continue;
                }

                if (string.Equals(
                        tag,
                        "map",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                modes.Add(
                    tag.Trim().ToLowerInvariant()
                );
            }
        }

        if (data?.ugc_entities != null)
        {
            foreach (JagexUgcEntity entity
                     in data.ugc_entities)
            {
                if (entity == null ||
                    string.IsNullOrWhiteSpace(
                        entity.mode))
                {
                    continue;
                }

                string mode =
                    entity.mode
                        .Trim()
                        .ToLowerInvariant();

                // "nor" representa entidades
                // comuns a vários modos.
                if (mode != "nor")
                {
                    modes.Add(mode);
                }
            }
        }

        string[] result =
            new string[modes.Count];

        modes.CopyTo(result);

        Array.Sort(
            result,
            StringComparer.OrdinalIgnoreCase
        );

        return result;
    }

    private static void Normalize(
        JagexUgcData data)
    {
        data.title ??=
            string.Empty;

        data.description ??=
            string.Empty;

        data.author ??=
            string.Empty;

        data.baseplate ??=
            string.Empty;

        data.skybox_name ??=
            string.Empty;

        data.tags ??=
            Array.Empty<string>();

        data.ugc_entities ??=
            Array.Empty<JagexUgcEntity>();
    }

    private static void Validate(
        JagexUgcData data)
    {
        if (data.ugc_entities.Length >
            MaxEntities)
        {
            throw new InvalidDataException(
                $"UGC possui mais de " +
                $"{MaxEntities} entidades."
            );
        }

        for (int i = 0;
             i < data.ugc_entities.Length;
             i++)
        {
            JagexUgcEntity entity =
                data.ugc_entities[i];

            if (entity == null)
            {
                throw new InvalidDataException(
                    $"Entidade UGC #{i} é nula."
                );
            }

            if (entity.position == null ||
                entity.position.Length != 3)
            {
                throw new InvalidDataException(
                    $"Entidade UGC #{i} possui " +
                    "posição inválida."
                );
            }

            int x =
                entity.position[0];

            int y =
                entity.position[1];

            int z =
                entity.position[2];

            if (x < 0 ||
                x >= WorldSizeXY ||
                y < 0 ||
                y >= WorldSizeXY)
            {
                throw new InvalidDataException(
                    $"Entidade UGC #{i} está " +
                    $"fora do mapa XY: " +
                    $"({x}, {y}, {z})."
                );
            }

            if (z < 0 ||
                z >= UgcHeight)
            {
                throw new InvalidDataException(
                    $"Entidade UGC #{i} possui " +
                    $"Z inválido: {z}."
                );
            }

            if (string.IsNullOrWhiteSpace(
                    entity.item))
            {
                throw new InvalidDataException(
                    $"Entidade UGC #{i} não " +
                    "possui item."
                );
            }

            if (string.IsNullOrWhiteSpace(
                    entity.mode))
            {
                throw new InvalidDataException(
                    $"Entidade UGC #{i} não " +
                    "possui modo."
                );
            }
        }
    }
}