using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed class LegacyMapEntry
{
    public string Name { get; }

    public string VxlPath { get; }

    public string TxtPath { get; }

    public string UgcPath { get; }

    public bool HasTxt =>
        !string.IsNullOrEmpty(TxtPath) &&
        File.Exists(TxtPath);

    public bool HasUgc =>
        !string.IsNullOrEmpty(UgcPath) &&
        File.Exists(UgcPath);

    public LegacyMapEntry(
        string name,
        string vxlPath,
        string txtPath,
        string ugcPath)
    {
        Name = name;
        VxlPath = vxlPath;
        TxtPath = txtPath;
        UgcPath = ugcPath;
    }
}

public static class LegacyMapCatalog
{
    private const string LegacyMapsFolder =
        "LegacyMaps";

    public static string GetMapsDirectory()
    {
        return Path.Combine(
            Application.persistentDataPath,
            LegacyMapsFolder
        );
    }

    public static List<LegacyMapEntry> FindMaps()
    {
        string directory =
            GetMapsDirectory();

        Directory.CreateDirectory(
            directory
        );

        string[] allFiles =
            Directory.GetFiles(
                directory,
                "*",
                SearchOption.TopDirectoryOnly
            );

        List<string> vxlFiles =
            new List<string>();

        foreach (string file in allFiles)
        {
            if (string.Equals(
                    Path.GetExtension(file),
                    ".vxl",
                    StringComparison.OrdinalIgnoreCase))
            {
                vxlFiles.Add(file);
            }
        }

        vxlFiles.Sort(
            StringComparer.OrdinalIgnoreCase
        );

        List<LegacyMapEntry> maps =
            new List<LegacyMapEntry>();

        foreach (string vxlPath in vxlFiles)
        {
            string mapName =
                Path.GetFileNameWithoutExtension(
                    vxlPath
                );

            string txtPath =
                FindSiblingFile(
                    allFiles,
                    mapName,
                    ".txt"
                );

            string ugcPath =
                FindSiblingFile(
                    allFiles,
                    mapName,
                    ".ugc"
                );

            maps.Add(
                new LegacyMapEntry(
                    mapName,
                    vxlPath,
                    txtPath,
                    ugcPath
                )
            );
        }

        return maps;
    }

    private static string FindSiblingFile(
        string[] allFiles,
        string mapName,
        string extension)
    {
        string desiredName =
            mapName + extension;

        foreach (string file in allFiles)
        {
            string fileName =
                Path.GetFileName(file);

            if (string.Equals(
                    fileName,
                    desiredName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return file;
            }
        }

        return null;
    }
}