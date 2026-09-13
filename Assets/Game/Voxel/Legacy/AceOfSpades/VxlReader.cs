using System;
using System.IO;

public static class VxlReader
{
    private const int Width =
        VxlCoordinateConverter.MapWidth;

    private const int Depth =
        VxlCoordinateConverter.MapDepth;

    private static readonly VoxelColor
        HiddenSolidColor =
            new VoxelColor(
                100,
                100,
                100,
                255
            );

    private struct VxlInfo
    {
        public int Columns;
        public int MaxReference;
        public int Height;
    }

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

        VxlInfo info =
            Analyze(data);

        VoxelMapData map =
            new VoxelMapData(
                Width,
                info.Height,
                Depth
            );

        InitializeSolidMap(
            map
        );

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
                        aosY,
                        info.Height
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

    private static VxlInfo Analyze(
        byte[] data)
    {
        int position = 0;
        int columns = 0;
        int maxReference = 0;

        while (position < data.Length)
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

            maxReference =
                Math.Max(
                    maxReference,
                    Math.Max(
                        value1,
                        Math.Max(
                            value2,
                            value3
                        )
                    )
                );

            while (spanWords != 0)
            {
                position +=
                    spanWords * 4;

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

                maxReference =
                    Math.Max(
                        maxReference,
                        Math.Max(
                            value1,
                            Math.Max(
                                value2,
                                value3
                            )
                        )
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

            columns++;
        }

        if (position != data.Length)
        {
            throw new InvalidDataException(
                "Estrutura VXL inválida."
            );
        }

        int expectedColumns =
            Width * Depth;

        if (columns != expectedColumns)
        {
            throw new InvalidDataException(
                $"Quantidade de colunas VXL " +
                $"inválida: {columns}. " +
                $"Esperado: {expectedColumns}."
            );
        }

        int height;

        if (maxReference <= 63)
        {
            height =
                VxlCoordinateConverter
                    .ClassicMapHeight;
        }
        else if (maxReference <= 239)
        {
            height =
                VxlCoordinateConverter
                    .RetailMapHeight;
        }
        else
        {
            throw new InvalidDataException(
                $"Altura VXL não reconhecida. " +
                $"Maior referência: " +
                $"{maxReference}."
            );
        }

        return new VxlInfo
        {
            Columns = columns,
            MaxReference = maxReference,
            Height = height
        };
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
             worldZ < map.SizeZ;
             worldZ++)
        {
            for (int worldX = 0;
                 worldX < map.SizeX;
                 worldX++)
            {
                for (int worldY = 0;
                     worldY < map.SizeY;
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
        int aosY,
        int height)
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
                    topColorEnd,
                    height
                );

            if (currentZ <
                topColorStart)
            {
                SetAirRange(
                    map,
                    aosX,
                    aosY,
                    currentZ,
                    topColorStart,
                    height
                );
            }

            int colorOffset =
                spanStart + 4;

            EnsureAvailable(
                data,
                colorOffset,
                topColorCount * 4
            );

            for (int i = 0;
                 i < topColorCount;
                 i++)
            {
                int aosZ =
                    topColorStart +
                    i;

                ValidateZ(
                    aosZ,
                    height
                );

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
                    height,
                    color
                );
            }

            currentZ =
                topColorStart +
                topColorCount;

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

            int storedColorCount =
                numberOfFourByteChunks -
                1;

            int bottomColorCount =
                storedColorCount -
                topColorCount;

            if (bottomColorCount < 0)
            {
                throw new InvalidDataException(
                    "Tamanho do span " +
                    "inconsistente."
                );
            }

            int spanSize =
                numberOfFourByteChunks *
                4;

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

            int nextAirStart =
                data[
                    nextSpanOffset + 3
                ];

            if (nextAirStart < 0 ||
                nextAirStart > height)
            {
                throw new InvalidDataException(
                    $"Início do próximo trecho " +
                    $"de ar inválido: " +
                    $"{nextAirStart}."
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
                    bottomColorStart +
                    i;

                ValidateZ(
                    aosZ,
                    height
                );

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
                    height,
                    color
                );
            }

            currentZ =
                nextAirStart;

            offset =
                nextSpanOffset;
        }
    }

    private static int GetTopColorCount(
        int start,
        int end,
        int height)
    {
        /*
         * Span vazio padrão:
         *
         * clássico pode usar:
         * S = 64, E = 63
         *
         * retail usa:
         * S = 240, E = 239
         */
        if (start == height &&
            end == height - 1)
        {
            return 0;
        }

        if (start < 0 ||
            start >= height)
        {
            throw new InvalidDataException(
                $"Início de top colors " +
                $"inválido: {start}."
            );
        }

        if (end >= start)
        {
            if (end >= height)
            {
                throw new InvalidDataException(
                    $"Fim de top colors " +
                    $"inválido: {end}."
                );
            }

            return
                end -
                start +
                1;
        }

        if (end == start - 1)
        {
            return 0;
        }

        if (start == 0 &&
            end == 255)
        {
            return 0;
        }

        throw new InvalidDataException(
            "Intervalo superior de cores " +
            $"inválido. S={start}, E={end}."
        );
    }

    private static void SetAirRange(
        VoxelMapData map,
        int aosX,
        int aosY,
        int startZ,
        int endZExclusive,
        int height)
    {
        if (startZ < 0 ||
            endZExclusive > height ||
            startZ > endZExclusive)
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
                    .ToWorldX(
                        aosX
                    );

            int worldY =
                VxlCoordinateConverter
                    .ToWorldY(
                        aosZ,
                        height
                    );

            int worldZ =
                VxlCoordinateConverter
                    .ToWorldZ(
                        aosY
                    );

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
        int height,
        VoxelColor color)
    {
        ValidateZ(
            aosZ,
            height
        );

        int worldX =
            VxlCoordinateConverter
                .ToWorldX(
                    aosX
                );

        int worldY =
            VxlCoordinateConverter
                .ToWorldY(
                    aosZ,
                    height
                );

        int worldZ =
            VxlCoordinateConverter
                .ToWorldZ(
                    aosY
                );

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

        byte blue =
            data[offset];

        byte green =
            data[offset + 1];

        byte red =
            data[offset + 2];

        return new VoxelColor(
            red,
            green,
            blue,
            255
        );
    }

    private static void ValidateZ(
        int z,
        int height)
    {
        if (z < 0 ||
            z >= height)
        {
            throw new InvalidDataException(
                $"Z fora dos limites do " +
                $"VXL: {z}. " +
                $"Altura: {height}."
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