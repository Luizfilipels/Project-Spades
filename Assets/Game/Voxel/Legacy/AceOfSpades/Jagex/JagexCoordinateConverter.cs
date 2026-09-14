using System;
using System.IO;

public static class JagexCoordinateConverter
{
    public const int MapWidth = 512;
    public const int MapDepth = 512;

    public const int TerrainHeight = 240;

    public const int TerrainMaxZ =
        TerrainHeight - 1;

    public const int UgcCoordinateHeight = 256;

    // ============================================================
    // UGC -> PROJECT SPADES
    // ============================================================

    public static MapCoordinate ToWorldCoordinate(
        int ugcX,
        int ugcY,
        int ugcZ,
        int sourceZShift)
    {
        ValidateRawUgcCoordinate(
            ugcX,
            ugcY,
            ugcZ
        );

        ValidateSourceZShift(
            sourceZShift
        );

        int normalizedRetailZ =
            ugcZ +
            sourceZShift;

        if (normalizedRetailZ < 0 ||
            normalizedRetailZ >= TerrainHeight)
        {
            throw new InvalidDataException(
                "Coordenada UGC não cabe no " +
                "terreno retail após normalização.\n" +
                "UGC Z: " +
                ugcZ +
                "\nsourceZShift: " +
                sourceZShift +
                "\nZ normalizado: " +
                normalizedRetailZ
            );
        }

        int worldX =
            ugcX;

        int worldY =
            TerrainMaxZ -
            normalizedRetailZ;

        int worldZ =
            ugcY;

        return new MapCoordinate(
            worldX,
            worldY,
            worldZ
        );
    }

    public static MapCoordinate ToWorldCoordinate(
        JagexUgcEntity entity,
        int sourceZShift)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(
                nameof(entity)
            );
        }

        if (entity.position == null ||
            entity.position.Length != 3)
        {
            throw new InvalidDataException(
                "Entidade UGC possui posição inválida."
            );
        }

        return ToWorldCoordinate(
            entity.position[0],
            entity.position[1],
            entity.position[2],
            sourceZShift
        );
    }

    public static bool TryToWorldCoordinate(
        JagexUgcEntity entity,
        int sourceZShift,
        out MapCoordinate coordinate)
    {
        coordinate =
            default(MapCoordinate);

        try
        {
            coordinate =
                ToWorldCoordinate(
                    entity,
                    sourceZShift
                );

            return true;
        }
        catch
        {
            return false;
        }
    }

    // ============================================================
    // SOURCE Z SHIFT
    // ============================================================

    public static int DetectSourceZShift(
        string vxlFilePath)
    {
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

        byte[] data =
            File.ReadAllBytes(
                vxlFilePath
            );

        return DetectSourceZShift(
            data
        );
    }

    public static int DetectSourceZShift(
        byte[] data)
    {
        if (data == null)
        {
            throw new ArgumentNullException(
                nameof(data)
            );
        }

        if (data.Length < 4)
        {
            throw new InvalidDataException(
                "Arquivo VXL muito pequeno."
            );
        }

        VxlVerticalInfo info =
            AnalyzeRetailVxl(
                data
            );

        int expectedColumns =
            MapWidth *
            MapDepth;

        if (info.ColumnCount !=
            expectedColumns)
        {
            throw new InvalidDataException(
                "Quantidade inesperada de " +
                "colunas no VXL retail.\n" +
                "Encontrado: " +
                info.ColumnCount +
                "\nEsperado: " +
                expectedColumns
            );
        }

        /*
         * O retail pode usar 240 como
         * sentinela de coluna vazia.
         *
         * Portanto:
         *
         * maxRef <= 239
         *     pode gerar shift.
         *
         * maxRef >= 240
         *     shift = 0.
         */
        int sourceZShift =
            Math.Max(
                0,
                TerrainMaxZ -
                info.MaxReference
            );

        return sourceZShift;
    }

    // ============================================================
    // VXL ANALYSIS
    // ============================================================

    private static VxlVerticalInfo AnalyzeRetailVxl(
        byte[] data)
    {
        int position = 0;
        int columns = 0;
        int maxReference = 0;

        while (position <
               data.Length)
        {
            EnsureAvailable(
                data,
                position,
                4
            );

            int spanWords =
                data[position];

            int value1 =
                data[position + 1];

            int value2 =
                data[position + 2];

            int value3 =
                data[position + 3];

            UpdateMaxReference(
                ref maxReference,
                value1,
                value2,
                value3
            );

            while (spanWords != 0)
            {
                position +=
                    spanWords *
                    4;

                EnsureAvailable(
                    data,
                    position,
                    4
                );

                spanWords =
                    data[position];

                value1 =
                    data[position + 1];

                value2 =
                    data[position + 2];

                value3 =
                    data[position + 3];

                UpdateMaxReference(
                    ref maxReference,
                    value1,
                    value2,
                    value3
                );
            }

            if (value2 >= value1)
            {
                position +=
                    8 +
                    4 *
                    (value2 - value1);
            }
            else
            {
                position += 4;
            }

            if (position >
                data.Length)
            {
                throw new EndOfStreamException(
                    "Fim inesperado do VXL."
                );
            }

            columns++;
        }

        if (position !=
            data.Length)
        {
            throw new InvalidDataException(
                "Estrutura VXL retail inválida."
            );
        }

        return new VxlVerticalInfo(
            columns,
            maxReference
        );
    }

    private static void UpdateMaxReference(
        ref int currentMax,
        int value1,
        int value2,
        int value3)
    {
        currentMax =
            Math.Max(
                currentMax,
                value1
            );

        currentMax =
            Math.Max(
                currentMax,
                value2
            );

        currentMax =
            Math.Max(
                currentMax,
                value3
            );
    }

    // ============================================================
    // VALIDATION
    // ============================================================

    private static void ValidateRawUgcCoordinate(
        int x,
        int y,
        int z)
    {
        if (x < 0 ||
            x >= MapWidth)
        {
            throw new InvalidDataException(
                "UGC X fora dos limites: " +
                x
            );
        }

        if (y < 0 ||
            y >= MapDepth)
        {
            throw new InvalidDataException(
                "UGC Y fora dos limites: " +
                y
            );
        }

        if (z < 0 ||
            z >= UgcCoordinateHeight)
        {
            throw new InvalidDataException(
                "UGC Z fora dos limites: " +
                z
            );
        }
    }

    private static void ValidateSourceZShift(
        int sourceZShift)
    {
        if (sourceZShift < 0 ||
            sourceZShift >= TerrainHeight)
        {
            throw new InvalidDataException(
                "sourceZShift inválido: " +
                sourceZShift
            );
        }
    }

    private static void EnsureAvailable(
        byte[] data,
        int offset,
        int count)
    {
        if (offset < 0 ||
            count < 0 ||
            offset >
                data.Length - count)
        {
            throw new EndOfStreamException(
                "Fim inesperado do arquivo VXL."
            );
        }
    }

    // ============================================================
    // INTERNAL DATA
    // ============================================================

    private struct VxlVerticalInfo
    {
        public int ColumnCount;
        public int MaxReference;

        public VxlVerticalInfo(
            int columnCount,
            int maxReference)
        {
            ColumnCount =
                columnCount;

            MaxReference =
                maxReference;
        }
    }
}