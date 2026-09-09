using System;
using UnityEngine;

public class PlayerBuildState : MonoBehaviour
{
    [Header("Selected Block")]
    [SerializeField]
    private BlockType selectedBlockType =
        BlockType.Dirt;

    [Header("Selected Color")]
    [SerializeField, Range(0, 255)]
    private int red = 90;

    [SerializeField, Range(0, 255)]
    private int green = 130;

    [SerializeField, Range(0, 255)]
    private int blue = 180;

    public BlockType SelectedBlockType =>
        selectedBlockType;

    public VoxelColor SelectedColor =>
        new VoxelColor(
            (byte)red,
            (byte)green,
            (byte)blue
        );

    public event Action<VoxelColor>
        ColorChanged;

    public event Action<BlockType>
        BlockTypeChanged;

    public void SetColor(
        byte r,
        byte g,
        byte b)
    {
        red = r;
        green = g;
        blue = b;

        ColorChanged?.Invoke(
            SelectedColor
        );
    }

    public void SetBlockType(
        BlockType blockType)
    {
        if (blockType == BlockType.Air)
            return;

        selectedBlockType =
            blockType;

        BlockTypeChanged?.Invoke(
            selectedBlockType
        );
    }

    private void OnValidate()
    {
        red = Mathf.Clamp(
            red,
            0,
            255
        );

        green = Mathf.Clamp(
            green,
            0,
            255
        );

        blue = Mathf.Clamp(
            blue,
            0,
            255
        );
    }
}
