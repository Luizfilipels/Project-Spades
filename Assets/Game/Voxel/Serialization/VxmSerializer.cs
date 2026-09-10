using System;
using System.IO;
using System.IO.Compression;
using System.Text;

public static class VxmSerializer
{
    private const int CurrentVersion = 1;

    private const long MaxVoxelCount =
        67_108_864;

    private static readonly byte[] Magic =
    {
        (byte)'V',
        (byte)'X',
        (byte)'M',
        (byte)'1'
    };

    public static void Save(
        string filePath,
        VoxelMapData map)
    {
        if (map == null)
        {
            throw new ArgumentNullException(
                nameof(map)
            );
        }

        string directory =
            Path.GetDirectoryName(
                filePath
            );

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(
                directory
            );
        }

        using FileStream fileStream =
            new FileStream(
                filePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None
            );

        using GZipStream gzipStream =
            new GZipStream(
                fileStream,
                CompressionLevel.Optimal
            );

        using BinaryWriter writer =
            new BinaryWriter(
                gzipStream,
                Encoding.UTF8,
                false
            );

        writer.Write(Magic);

        writer.Write(
            CurrentVersion
        );

        writer.Write(
            map.SizeX
        );

        writer.Write(
            map.SizeY
        );

        writer.Write(
            map.SizeZ
        );

        for (int z = 0;
             z < map.SizeZ;
             z++)
        {
            for (int x = 0;
                 x < map.SizeX;
                 x++)
            {
                for (int y = 0;
                     y < map.SizeY;
                     y++)
                {
                    Block block =
                        map.GetBlock(
                            x,
                            y,
                            z
                        );

                    writer.Write(
                        (byte)block.Type
                    );

                    writer.Write(
                        block.Color.R
                    );

                    writer.Write(
                        block.Color.G
                    );

                    writer.Write(
                        block.Color.B
                    );

                    writer.Write(
                        block.Color.A
                    );
                }
            }
        }
    }

    public static VoxelMapData Load(
        string filePath)
    {
        using FileStream fileStream =
            new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read
            );

        using GZipStream gzipStream =
            new GZipStream(
                fileStream,
                CompressionMode.Decompress
            );

        using BinaryReader reader =
            new BinaryReader(
                gzipStream,
                Encoding.UTF8,
                false
            );

        ValidateMagic(reader);

        int version =
            reader.ReadInt32();

        if (version != CurrentVersion)
        {
            throw new InvalidDataException(
                $"Versão VXM não suportada: {version}"
            );
        }

        int sizeX =
            reader.ReadInt32();

        int sizeY =
            reader.ReadInt32();

        int sizeZ =
            reader.ReadInt32();

        ValidateDimensions(
            sizeX,
            sizeY,
            sizeZ
        );

        VoxelMapData map =
            new VoxelMapData(
                sizeX,
                sizeY,
                sizeZ
            );

        for (int z = 0;
             z < sizeZ;
             z++)
        {
            for (int x = 0;
                 x < sizeX;
                 x++)
            {
                for (int y = 0;
                     y < sizeY;
                     y++)
                {
                    BlockType type =
                        (BlockType)
                        reader.ReadByte();

                    byte r =
                        reader.ReadByte();

                    byte g =
                        reader.ReadByte();

                    byte b =
                        reader.ReadByte();

                    byte a =
                        reader.ReadByte();

                    Block block =
                        new Block(
                            type,
                            new VoxelColor(
                                r,
                                g,
                                b,
                                a
                            )
                        );

                    map.SetBlock(
                        x,
                        y,
                        z,
                        block
                    );
                }
            }
        }

        return map;
    }

    private static void ValidateMagic(
        BinaryReader reader)
    {
        byte[] magic =
            reader.ReadBytes(
                Magic.Length
            );

        if (magic.Length !=
            Magic.Length)
        {
            throw new InvalidDataException(
                "Arquivo VXM inválido."
            );
        }

        for (int i = 0;
             i < Magic.Length;
             i++)
        {
            if (magic[i] != Magic[i])
            {
                throw new InvalidDataException(
                    "O arquivo não é um mapa VXM válido."
                );
            }
        }
    }

    private static void ValidateDimensions(
        int sizeX,
        int sizeY,
        int sizeZ)
    {
        if (sizeX <= 0 ||
            sizeY <= 0 ||
            sizeZ <= 0)
        {
            throw new InvalidDataException(
                "Dimensões inválidas no mapa."
            );
        }

        long voxelCount =
            (long)sizeX *
            sizeY *
            sizeZ;

        if (voxelCount >
            MaxVoxelCount)
        {
            throw new InvalidDataException(
                "O mapa excede o limite de segurança."
            );
        }
    }
}