using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class BlockPlacementPreview : MonoBehaviour
{
    private Mesh previewMesh;
    private MeshRenderer meshRenderer;

    private static readonly Vector3[] CubeCorners =
    {
        new Vector3(0, 0, 0),
        new Vector3(1, 0, 0),
        new Vector3(1, 1, 0),
        new Vector3(0, 1, 0),

        new Vector3(0, 0, 1),
        new Vector3(1, 0, 1),
        new Vector3(1, 1, 1),
        new Vector3(0, 1, 1)
    };

    private static readonly int[] CubeEdges =
    {
        0, 1,
        1, 2,
        2, 3,
        3, 0,

        4, 5,
        5, 6,
        6, 7,
        7, 4,

        0, 4,
        1, 5,
        2, 6,
        3, 7
    };

    private void Awake()
    {
        meshRenderer =
            GetComponent<MeshRenderer>();

        previewMesh =
            new Mesh
            {
                name = "Block Placement Preview"
            };

        GetComponent<MeshFilter>()
            .sharedMesh = previewMesh;

        meshRenderer.enabled = false;
    }

    public void Show(
        IReadOnlyList<Vector3Int> voxels,
        VoxelWorld world)
    {
        if (voxels == null ||
            voxels.Count == 0)
        {
            Hide();
            return;
        }

        List<Vector3> vertices =
            new List<Vector3>();

        List<int> indices =
            new List<int>();

        foreach (Vector3Int voxel in voxels)
        {
            int vertexOffset =
                vertices.Count;

            for (int i = 0;
                 i < CubeCorners.Length;
                 i++)
            {
                Vector3 voxelLocalPosition =
                    voxel + CubeCorners[i];

                Vector3 worldPosition =
                    world.transform.TransformPoint(
                        voxelLocalPosition
                    );

                Vector3 previewLocalPosition =
                    transform.InverseTransformPoint(
                        worldPosition
                    );

                vertices.Add(
                    previewLocalPosition
                );
            }

            for (int i = 0;
                 i < CubeEdges.Length;
                 i++)
            {
                indices.Add(
                    vertexOffset +
                    CubeEdges[i]
                );
            }
        }

        previewMesh.Clear();

        previewMesh.SetVertices(
            vertices
        );

        previewMesh.SetIndices(
            indices,
            MeshTopology.Lines,
            0
        );

        previewMesh.RecalculateBounds();

        meshRenderer.enabled = true;
    }

    public void Hide()
    {
        if (previewMesh != null)
        {
            previewMesh.Clear();
        }

        if (meshRenderer != null)
        {
            meshRenderer.enabled = false;
        }
    }
}