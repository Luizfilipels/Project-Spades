using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class ChunkRenderer : MonoBehaviour
{
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    private Mesh chunkMesh;

    private void Awake()
    {
        meshFilter =
            GetComponent<MeshFilter>();

        meshCollider =
            GetComponent<MeshCollider>();

        chunkMesh =
            new Mesh
            {
                name = "Voxel Chunk Mesh",
                indexFormat =
                    IndexFormat.UInt32
            };

        meshFilter.sharedMesh =
            chunkMesh;
    }

    public void ApplyMesh(
        MeshData meshData)
    {
        chunkMesh.Clear();

        chunkMesh.SetVertices(
            meshData.Vertices
        );

        chunkMesh.SetTriangles(
            meshData.Triangles,
            0
        );

        chunkMesh.SetUVs(
            0,
            meshData.UVs
        );

        chunkMesh.SetColors(
            meshData.Colors
        );

        chunkMesh.RecalculateNormals();
        chunkMesh.RecalculateBounds();

        meshCollider.sharedMesh =
            null;

        meshCollider.sharedMesh =
            chunkMesh;
    }
}