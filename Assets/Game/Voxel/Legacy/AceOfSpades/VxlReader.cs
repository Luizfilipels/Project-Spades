using System;
using System.IO;

public static class VxlReader
{
    private const int Width =
        VxlCoordinateConverter.MapWidth;

    private const int Depth =
        VxlCoordinateConverter.MapDepth;

    private const int Height =
        VxlCoordinateConverter.MapHeight;

    private static readonly VoxelColor
        HiddenSolidColor =
            new VoxelColor(
                100,
                100,
                100,
                255
            );

    public static VoxelMapData Load(
        string filePath)
    {
        if (string.IsNullOrWhiteSpace(
                filePath))
        {
            throw new ArgumentException(
                "Caminho VXL inválido.",
                nameof(filePath)
            );
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                "Arquivo VXL não encontrado.",
                filePath
            );
        }

        byte[] data =
            File.ReadAllBytes(
                filePath
            );

        return Load(data);
    }

    public static VoxelMapData Load(
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

        VoxelMapData map =
            new VoxelMapData(
                Width,
                Height,
                Depth
            );

        InitializeSolidMap(map);

        int offset = 0;

        for (int aosY = 0;
             aosY < Depth;
             aosY++)
        {
            for (int aosX = 0;
                 aosX < Width;
                 aosX++)
            {
                try
                {
                    ReadColumn(
                        data,
                        ref offset,
                        map,
                        aosX,
                        aosY
                    );
                }
                catch (Exception exception)
                {
                    throw new InvalidDataException(
                        $"Erro na coluna VXL " +
                        $"({aosX}, {aosY}), " +
                        $"offset {offset}: " +
                        exception.Message,
                        exception
                    );
                }
            }
        }

        if (offset != data.Length)
        {
            throw new InvalidDataException(
                "O arquivo VXL contém dados " +
                "inesperados após a última coluna. " +
                $"Lidos: {offset} bytes. " +
                $"Arquivo: {data.Length} bytes."
            );
        }

        return map;
    }

    private static void InitializeSolidMap(
        VoxelMapData map)
    {
        Block hiddenSolid =
            new Block(
                BlockType.Stone,
                HiddenSolidColor
            );

        for (int worldZ = 0;
             worldZ < Depth;
             worldZ++)
        {
            for (int worldX = 0;
                 worldX < Width;
                 worldX++)
            {
                for (int worldY = 0;
                     worldY < Height;
                     worldY++)
                {
                    map.SetBlock(
                        worldX,
                        worldY,
                        worldZ,
                        hiddenSolid
                    );
                }
            }
        }
    }

    private static void ReadColumn(
        byte[] data,
        ref int offset,
        VoxelMapData map,
        int aosX,
        int aosY)
    {
        int currentZ = 0;

        while (true)
        {
            EnsureAvailable(
                data,
                offset,
                4
            );

            int spanStart =
                offset;

            int numberOfFourByteChunks =
                data[spanStart];

            int topColorStart =
                data[spanStart + 1];

            int topColorEnd =
                data[spanStart + 2];

            int topColorCount =
                GetTopColorCount(
                    topColorStart,
                    topColorEnd
                );

            /*
             * O reader original do PySpades faz:
             *
             * for (i = z; i < top_color_start; i++)
             *     voxel = air;
             *
             * Portanto, se currentZ >= topColorStart,
             * simplesmente não há um trecho de ar.
             */
            if (currentZ < topColorStart)
            {
                SetAirRange(
                    map,
                    aosX,
                    aosY,
                    currentZ,
                    topColorStart
                );
            }

            int colorOffset =
                spanStart + 4;

            EnsureAvailable(
                data,
                colorOffset,
                topColorCount * 4
            );

            /*
             * Top colored run.
             *
             * Usamos contagem em vez de:
             *
             * z <= topColorEnd
             *
             * porque mapas reais podem representar
             * uma top run vazia com E = S - 1.
             */
            for (int i = 0;
                 i < topColorCount;
                 i++)
            {
                int aosZ =
                    topColorStart + i;

                ValidateZ(aosZ);

                VoxelColor color =
                    ReadColor(
                        data,
                        colorOffset
                    );

                colorOffset += 4;

                SetColoredSolid(
                    map,
                    aosX,
                    aosY,
                    aosZ,
                    color
                );
            }

            currentZ =
                topColorStart +
                topColorCount;

            /*
             * N == 0:
             * último span da coluna.
             */
            if (numberOfFourByteChunks == 0)
            {
                int finalSpanSize =
                    4 *
                    (topColorCount + 1);

                EnsureAvailable(
                    data,
                    spanStart,
                    finalSpanSize
                );

                offset =
                    spanStart +
                    finalSpanSize;

                return;
            }

            /*
             * N inclui:
             *
             * 1 header
             * + top colors
             * + bottom colors
             */
            int storedColorCount =
                numberOfFourByteChunks - 1;

            int bottomColorCount =
                storedColorCount -
                topColorCount;

            if (bottomColorCount < 0)
            {
                throw new InvalidDataException(
                    "Tamanho do span inconsistente. " +
                    $"N={numberOfFourByteChunks}, " +
                    $"top={topColorCount}."
                );
            }

            int spanSize =
                numberOfFourByteChunks * 4;

            EnsureAvailable(
                data,
                spanStart,
                spanSize
            );

            int nextSpanOffset =
                spanStart +
                spanSize;

            EnsureAvailable(
                data,
                nextSpanOffset,
                4
            );

            /*
             * O byte A do PRÓXIMO span
             * indica onde começa o próximo
             * trecho de ar.
             */
            int nextAirStart =
                data[
                    nextSpanOffset + 3
                ];

            if (nextAirStart < 0 ||
                nextAirStart > Height)
            {
                throw new InvalidDataException(
                    "Início do próximo trecho " +
                    $"de ar inválido: {nextAirStart}."
                );
            }

            int bottomColorStart =
                nextAirStart -
                bottomColorCount;

            if (bottomColorStart < 0 ||
                bottomColorStart >
                    nextAirStart)
            {
                throw new InvalidDataException(
                    "Sequência inferior de " +
                    "cores inválida."
                );
            }

            /*
             * Os bottom colors estão armazenados
             * imediatamente depois dos top colors
             * do span atual.
             */
            EnsureAvailable(
                data,
                colorOffset,
                bottomColorCount * 4
            );

            for (int i = 0;
                 i < bottomColorCount;
                 i++)
            {
                int aosZ =
                    bottomColorStart + i;

                ValidateZ(aosZ);

                VoxelColor color =
                    ReadColor(
                        data,
                        colorOffset
                    );

                colorOffset += 4;

                SetColoredSolid(
                    map,
                    aosX,
                    aosY,
                    aosZ,
                    color
                );
            }

            /*
             * O reader original termina este span
             * com z = próximo A.
             */
            currentZ =
                nextAirStart;

            offset =
                nextSpanOffset;
        }
    }

    private static int GetTopColorCount(
        int start,
        int end)
    {
        if (start < 0 ||
            start >= Height)
        {
            throw new InvalidDataException(
                $"Início de top colors inválido: {start}."
            );
        }

        /*
         * Caso normal:
         *
         * S = 20
         * E = 24
         *
         * 5 voxels.
         */
        if (end >= start)
        {
            if (end >= Height)
            {
                throw new InvalidDataException(
                    $"Fim de top colors inválido: {end}."
                );
            }

            return
                end -
                start +
                1;
        }

        /*
         * Caso usado por mapas/encoders
         * compatíveis com PySpades:
         *
         * S = 20
         * E = 19
         *
         * representa uma top colored run
         * de comprimento ZERO.
         */
        if (end == start - 1)
        {
            return 0;
        }

        /*
         * Equivalente byte-wrap de:
         *
         * S = 0
         * E = -1
         *
         * armazenado como 255.
         */
        if (start == 0 &&
            end == 255)
        {
            return 0;
        }

        throw new InvalidDataException(
            "Intervalo superior de cores inválido. " +
            $"S={start}, E={end}."
        );
    }

    private static void SetAirRange(
        VoxelMapData map,
        int aosX,
        int aosY,
        int startZ,
        int endZExclusive)
    {
        if (startZ < 0 ||
            endZExclusive >
                Height ||
            startZ >
                endZExclusive)
        {
            throw new InvalidDataException(
                "Intervalo de ar fora " +
                "dos limites."
            );
        }

        for (int aosZ = startZ;
             aosZ < endZExclusive;
             aosZ++)
        {
            int worldX =
                VxlCoordinateConverter
                    .ToWorldX(aosX);

            int worldY =
                VxlCoordinateConverter
                    .ToWorldY(aosZ);

            int worldZ =
                VxlCoordinateConverter
                    .ToWorldZ(aosY);

            map.SetBlock(
                worldX,
                worldY,
                worldZ,
                new Block(
                    BlockType.Air
                )
            );
        }
    }

    private static void SetColoredSolid(
        VoxelMapData map,
        int aosX,
        int aosY,
        int aosZ,
        VoxelColor color)
    {
        ValidateZ(aosZ);

        int worldX =
            VxlCoordinateConverter
                .ToWorldX(aosX);

        int worldY =
            VxlCoordinateConverter
                .ToWorldY(aosZ);

        int worldZ =
            VxlCoordinateConverter
                .ToWorldZ(aosY);

        map.SetBlock(
            worldX,
            worldY,
            worldZ,
            new Block(
                BlockType.Stone,
                color
            )
        );
    }

    private static VoxelColor ReadColor(
        byte[] data,
        int offset)
    {
        EnsureAvailable(
            data,
            offset,
            4
        );

        /*
         * VXL:
         *
         * B
         * G
         * R
         * A/shading
         */
        byte blue =
            data[offset];

        byte green =
            data[offset + 1];

        byte red =
            data[offset + 2];

        /*
         * O quarto byte é shading no
         * formato clássico. Ainda não vamos
         * aplicá-lo ao nosso shader.
         */
        return new VoxelColor(
            red,
            green,
            blue,
            255
        );
    }

    private static void ValidateZ(
        int z)
    {
        if (z < 0 ||
            z >= Height)
        {
            throw new InvalidDataException(
                $"Z fora dos limites do VXL: {z}."
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
}