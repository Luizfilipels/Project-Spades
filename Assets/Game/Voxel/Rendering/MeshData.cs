using System.Collections.Generic;
using UnityEngine;

public class MeshData
{
    public readonly List<Vector3> Vertices = new();
    public readonly List<int> Triangles = new();
    public readonly List<Vector2> UVs = new();

    public void Clear()
    {
        Vertices.Clear();
        Triangles.Clear();
        UVs.Clear();
    }
}