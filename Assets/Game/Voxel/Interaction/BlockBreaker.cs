using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(VoxelRaycaster))]
public class BlockBreaker : MonoBehaviour
{
    private VoxelRaycaster voxelRaycaster;

    private void Awake()
    {
        voxelRaycaster =
            GetComponent<VoxelRaycaster>();
    }

    private void Update()
    {
        if (Mouse.current == null)
            return;

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        TryBreakBlock();
    }

    private void TryBreakBlock()
    {
        if (!voxelRaycaster.TryGetVoxelHit(
                out Vector3Int voxel,
                out _,
                out _))
        {
            return;
        }

        bool removed =
            voxelRaycaster.World.RemoveBlock(
                voxel.x,
                voxel.y,
                voxel.z
            );

        if (removed)
        {
            Debug.Log(
                $"Voxel removido: {voxel}"
            );
        }
    }
}