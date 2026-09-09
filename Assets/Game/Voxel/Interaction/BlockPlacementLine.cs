using System.Collections.Generic;
using UnityEngine;

public static class BlockPlacementLine
{
    public static List<Vector3Int> Calculate(
        Vector3Int start,
        Vector3Int end,
        int maxBlocks)
    {
        List<Vector3Int> result =
            new List<Vector3Int>();

        Vector3 delta = end - start;

        int steps = Mathf.Max(
            Mathf.Abs(end.x - start.x),
            Mathf.Abs(end.y - start.y),
            Mathf.Abs(end.z - start.z)
        );

        if (steps == 0)
        {
            result.Add(start);
            return result;
        }

        int count =
            Mathf.Min(
                steps + 1,
                maxBlocks
            );

        Vector3 previous =
            new Vector3(
                int.MinValue,
                int.MinValue,
                int.MinValue
            );

        for (int i = 0; i < count; i++)
        {
            float t =
                steps == 0
                    ? 0f
                    : i / (float)steps;

            Vector3 point =
                Vector3.Lerp(
                    start,
                    end,
                    t
                );

            Vector3Int voxel =
                new Vector3Int(
                    Mathf.RoundToInt(point.x),
                    Mathf.RoundToInt(point.y),
                    Mathf.RoundToInt(point.z)
                );

            if (voxel != previous)
            {
                result.Add(voxel);
                previous = voxel;
            }
        }

        return result;
    }
}