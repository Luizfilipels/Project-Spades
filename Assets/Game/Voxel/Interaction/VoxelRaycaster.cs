using UnityEngine;
using UnityEngine.InputSystem;

public class VoxelRaycaster : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Camera targetCamera;

    [SerializeField]
    private VoxelWorld world;

    [Header("Raycast")]
    [SerializeField]
    private float maxDistance = 100f;

    [SerializeField]
    private LayerMask hitMask = ~0;

    [Header("Debug")]
    [SerializeField]
    private bool useMousePosition = true;

    public VoxelWorld World => world;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (world == null)
        {
            world = FindAnyObjectByType<VoxelWorld>();
        }
    }

    public bool TryGetVoxelHit(
        out Vector3Int hitVoxel,
        out Vector3Int adjacentVoxel,
        out RaycastHit hit)
    {
        hitVoxel = default;
        adjacentVoxel = default;
        hit = default;

        if (targetCamera == null ||
            world == null)
        {
            return false;
        }

        Ray ray = CreateRay();

        if (!Physics.Raycast(
                ray,
                out hit,
                maxDistance,
                hitMask))
        {
            return false;
        }

        Vector3 insidePoint =
            hit.point -
            hit.normal * 0.01f;

        Vector3 outsidePoint =
            hit.point +
            hit.normal * 0.01f;

        Vector3 localInside =
            world.transform.InverseTransformPoint(
                insidePoint
            );

        Vector3 localOutside =
            world.transform.InverseTransformPoint(
                outsidePoint
            );

        hitVoxel = new Vector3Int(
            Mathf.FloorToInt(localInside.x),
            Mathf.FloorToInt(localInside.y),
            Mathf.FloorToInt(localInside.z)
        );

        adjacentVoxel = new Vector3Int(
            Mathf.FloorToInt(localOutside.x),
            Mathf.FloorToInt(localOutside.y),
            Mathf.FloorToInt(localOutside.z)
        );

        return true;
    }

    private Ray CreateRay()
    {
        if (useMousePosition &&
            Mouse.current != null)
        {
            Vector2 mousePosition =
                Mouse.current.position.ReadValue();

            return targetCamera.ScreenPointToRay(
                mousePosition
            );
        }

        return targetCamera.ViewportPointToRay(
            new Vector3(
                0.5f,
                0.5f,
                0f
            )
        );
    }
}