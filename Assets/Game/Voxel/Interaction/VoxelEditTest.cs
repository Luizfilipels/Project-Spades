using System.Collections;
using UnityEngine;

public class VoxelEditTest : MonoBehaviour
{
    [SerializeField]
    private VoxelWorld world;

    [SerializeField]
    private int centerX = 64;

    [SerializeField]
    private int centerZ = 64;

    [SerializeField]
    private int radius = 8;

    [SerializeField]
    private int depth = 8;

    private IEnumerator Start()
    {
        if (world == null)
        {
            world = FindFirstObjectByType<VoxelWorld>();
        }

        // Dá tempo para o VoxelWorld gerar todos os chunks.
        yield return new WaitForSeconds(1f);

        if (world == null)
        {
            Debug.LogError(
                "VoxelEditTest: VoxelWorld não foi encontrado."
            );

            yield break;
        }

        CarveCrater();
    }

    private void CarveCrater()
    {
        int removedBlocks = 0;

        for (int x = centerX - radius;
             x <= centerX + radius;
             x++)
        {
            for (int z = centerZ - radius;
                 z <= centerZ + radius;
                 z++)
            {
                int topY =
                    FindHighestSolidBlock(
                        x,
                        z
                    );

                if (topY < 0)
                    continue;

                for (int y = topY;
                     y >= topY - depth;
                     y--)
                {
                    if (y < 0)
                        break;

                    if (world.RemoveBlock(
                            x,
                            y,
                            z))
                    {
                        removedBlocks++;
                    }
                }
            }
        }

        Debug.Log(
            $"VoxelEditTest: removidos {removedBlocks} blocos."
        );
    }

    private int FindHighestSolidBlock(
        int worldX,
        int worldZ)
    {
        for (int y = ChunkData.SizeY - 1;
             y >= 0;
             y--)
        {
            if (world.IsBlockSolid(
                    worldX,
                    y,
                    worldZ))
            {
                return y;
            }
        }

        return -1;
    }
}