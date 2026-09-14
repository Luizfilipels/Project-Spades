using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

public static class PySnipMapScriptImporter
{
    private const long MaxFileSize =
        2 * 1024 * 1024;

    private const int ClassicMapWidth = 512;
    private const int ClassicMapDepth = 512;

    private const int ClassicMapHeight =
        VxlCoordinateConverter.ClassicMapHeight;

    /*
     * Nomes encontrados em mapas antigos
     * PySpades / PySnip.
     *
     * Podemos adicionar aliases conforme
     * encontrarmos mapas reais diferentes.
     */
    private static readonly string[]
        BlueSpawnListNames =
        {
            "blue_spawns",
            "spawn_locations_blue",
            "blue_spawn_locations",
            "spawns_blue"
        };

    private static readonly string[]
        GreenSpawnListNames =
        {
            "green_spawns",
            "spawn_locations_green",
            "green_spawn_locations",
            "spawns_green"
        };

    public static PySnipMapScriptData Load(
        string filePath)
    {
        if (string.IsNullOrWhiteSpace(
                filePath))
        {
            throw new ArgumentException(
                "Caminho TXT inválido.",
                nameof(filePath)
            );
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                "Arquivo PySnip TXT não encontrado.",
                filePath
            );
        }

        FileInfo fileInfo =
            new FileInfo(filePath);

        if (fileInfo.Length >
            MaxFileSize)
        {
            throw new InvalidDataException(
                "Arquivo PySnip excede o " +
                "limite de segurança."
            );
        }

        string text =
            File.ReadAllText(
                filePath
            );

        PySnipMapScriptData result =
            new PySnipMapScriptData();

        ParseBasicMetadata(
            text,
            result
        );

        ParseExtensions(
            text,
            result
        );

        ParseEntityLocations(
            text,
            result
        );

        ParseSpawnAliases(
            text,
            BlueSpawnListNames,
            result.BlueSpawns
        );

        ParseSpawnAliases(
            text,
            GreenSpawnListNames,
            result.GreenSpawns
        );

        return result;
    }

    // ============================================================
    // BASIC METADATA
    // ============================================================

    private static void ParseBasicMetadata(
        string text,
        PySnipMapScriptData result)
    {
        result.Name =
            ParseStringAssignment(
                text,
                "name"
            );

        result.Version =
            ParseStringAssignment(
                text,
                "version"
            );

        result.Author =
            ParseStringAssignment(
                text,
                "author"
            );

        result.Description =
            ParseStringAssignment(
                text,
                "description"
            );
    }

    private static string
        ParseStringAssignment(
            string text,
            string variable)
    {
        string pattern =
            @"(?m)^\s*" +
            Regex.Escape(variable) +
            @"\s*=\s*(['""])(.*?)\1\s*(?:#.*)?$";

        Match match =
            Regex.Match(
                text,
                pattern
            );

        if (!match.Success)
        {
            return string.Empty;
        }

        return match
            .Groups[2]
            .Value
            .Trim();
    }

    // ============================================================
    // EXTENSIONS
    // ============================================================

    private static void ParseExtensions(
        string text,
        PySnipMapScriptData result)
    {
        Match dictionaryMatch =
            Regex.Match(
                text,
                @"(?ms)^\s*extensions\s*=\s*\{(.*?)\}"
            );

        if (!dictionaryMatch.Success)
        {
            return;
        }

        string dictionaryBody =
            dictionaryMatch
                .Groups[1]
                .Value;

        MatchCollection pairs =
            Regex.Matches(
                dictionaryBody,
                @"(['""])(.*?)\1\s*:\s*([^,\r\n}]+)"
            );

        foreach (Match pair
                 in pairs)
        {
            string key =
                pair.Groups[2]
                    .Value
                    .Trim();

            string value =
                pair.Groups[3]
                    .Value
                    .Trim();

            if (string.IsNullOrWhiteSpace(
                    key))
            {
                continue;
            }

            result.Extensions[key] =
                value;
        }
    }

    // ============================================================
    // BASES / FLAGS
    // ============================================================

    private static void ParseEntityLocations(
        string text,
        PySnipMapScriptData result)
    {
        if (TryParseNamedReturn(
                text,
                "BLUE_FLAG",
                out MapCoordinate blueFlag))
        {
            result.HasBlueFlag =
                true;

            result.BlueFlag =
                blueFlag;
        }

        if (TryParseNamedReturn(
                text,
                "BLUE_BASE",
                out MapCoordinate blueBase))
        {
            result.HasBlueBase =
                true;

            result.BlueBase =
                blueBase;
        }

        if (TryParseNamedReturn(
                text,
                "GREEN_FLAG",
                out MapCoordinate greenFlag))
        {
            result.HasGreenFlag =
                true;

            result.GreenFlag =
                greenFlag;
        }

        if (TryParseNamedReturn(
                text,
                "GREEN_BASE",
                out MapCoordinate greenBase))
        {
            result.HasGreenBase =
                true;

            result.GreenBase =
                greenBase;
        }
    }

    private static bool TryParseNamedReturn(
        string text,
        string entityName,
        out MapCoordinate coordinate)
    {
        coordinate =
            default;

        /*
         * Exemplo:
         *
         * if entity_id == BLUE_FLAG:
         *     return (145, 265, 52)
         *
         * ou:
         *
         * elif entity_id == GREEN_BASE:
         *     return (381, 252, 52)
         *
         * Não executamos Python.
         */

        string pattern =
            @"(?ms)" +
            @"(?:if|elif)\s+" +
            @"[^\r\n:]*\b" +
            Regex.Escape(entityName) +
            @"\b[^\r\n:]*:" +
            @".*?" +
            @"return\s*" +
            @"\(\s*" +
            @"(-?\d+)\s*,\s*" +
            @"(-?\d+)\s*,\s*" +
            @"(-?\d+)\s*" +
            @"\)";

        Match match =
            Regex.Match(
                text,
                pattern
            );

        if (!match.Success)
        {
            return false;
        }

        int aosX =
            ParseInteger(
                match.Groups[1].Value
            );

        int aosY =
            ParseInteger(
                match.Groups[2].Value
            );

        int aosZ =
            ParseInteger(
                match.Groups[3].Value
            );

        coordinate =
            ConvertCoordinate(
                aosX,
                aosY,
                aosZ
            );

        return true;
    }

    // ============================================================
    // SPAWN LISTS
    // ============================================================

    private static void ParseSpawnAliases(
        string text,
        string[] aliases,
        List<MapCoordinate> destination)
    {
        foreach (string alias
                 in aliases)
        {
            List<MapCoordinate>
                parsedCoordinates =
                    ParseSpawnList(
                        text,
                        alias
                    );

            if (parsedCoordinates.Count == 0)
            {
                continue;
            }

            foreach (MapCoordinate coordinate
                     in parsedCoordinates)
            {
                AddUniqueCoordinate(
                    destination,
                    coordinate
                );
            }
        }
    }

    private static List<MapCoordinate>
        ParseSpawnList(
            string text,
            string variableName)
    {
        List<MapCoordinate> result =
            new List<MapCoordinate>();

        /*
         * Exemplos:
         *
         * blue_spawns = [
         *     (100, 200, 50),
         *     (110, 210, 50)
         * ]
         *
         * ou:
         *
         * spawn_locations_blue = [
         *     (168, 276, 52),
         *     ...
         * ]
         */

        string pattern =
            @"(?ms)^\s*" +
            Regex.Escape(variableName) +
            @"\s*=\s*\[(.*?)\]";

        Match listMatch =
            Regex.Match(
                text,
                pattern
            );

        if (!listMatch.Success)
        {
            return result;
        }

        string listBody =
            listMatch
                .Groups[1]
                .Value;

        MatchCollection coordinates =
            Regex.Matches(
                listBody,
                @"\(\s*" +
                @"(-?\d+)\s*,\s*" +
                @"(-?\d+)\s*,\s*" +
                @"(-?\d+)\s*" +
                @"\)"
            );

        foreach (Match match
                 in coordinates)
        {
            int aosX =
                ParseInteger(
                    match.Groups[1]
                        .Value
                );

            int aosY =
                ParseInteger(
                    match.Groups[2]
                        .Value
                );

            int aosZ =
                ParseInteger(
                    match.Groups[3]
                        .Value
                );

            MapCoordinate coordinate =
                ConvertCoordinate(
                    aosX,
                    aosY,
                    aosZ
                );

            result.Add(
                coordinate
            );
        }

        return result;
    }

    private static void AddUniqueCoordinate(
        List<MapCoordinate> destination,
        MapCoordinate coordinate)
    {
        foreach (MapCoordinate existing
                 in destination)
        {
            if (existing.x == coordinate.x &&
                existing.y == coordinate.y &&
                existing.z == coordinate.z)
            {
                return;
            }
        }

        destination.Add(
            coordinate
        );
    }

    // ============================================================
    // COORDINATE CONVERSION
    // ============================================================

    private static MapCoordinate ConvertCoordinate(
        int aosX,
        int aosY,
        int aosZ)
    {
        ValidateClassicCoordinate(
            aosX,
            aosY,
            aosZ
        );

        /*
         * Ace of Spades:
         *
         * X = horizontal
         * Y = horizontal
         * Z = vertical para baixo
         *
         * Project Spades:
         *
         * X = horizontal
         * Y = vertical para cima
         * Z = horizontal
         */

        int worldX =
            VxlCoordinateConverter
                .ToWorldX(
                    aosX
                );

        int worldY =
            VxlCoordinateConverter
                .ToWorldY(
                    aosZ,
                    ClassicMapHeight
                );

        int worldZ =
            VxlCoordinateConverter
                .ToWorldZ(
                    aosY
                );

        return new MapCoordinate(
            worldX,
            worldY,
            worldZ
        );
    }

    private static void
        ValidateClassicCoordinate(
            int x,
            int y,
            int z)
    {
        if (x < 0 ||
            x >= ClassicMapWidth)
        {
            throw new InvalidDataException(
                $"Coordenada PySnip X " +
                $"fora do mapa: {x}."
            );
        }

        if (y < 0 ||
            y >= ClassicMapDepth)
        {
            throw new InvalidDataException(
                $"Coordenada PySnip Y " +
                $"fora do mapa: {y}."
            );
        }

        if (z < 0 ||
            z >= ClassicMapHeight)
        {
            throw new InvalidDataException(
                $"Coordenada PySnip Z " +
                $"fora do mapa clássico: {z}."
            );
        }
    }

    // ============================================================
    // HELPERS
    // ============================================================

    private static int ParseInteger(
        string value)
    {
        return int.Parse(
            value,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture
        );
    }
}