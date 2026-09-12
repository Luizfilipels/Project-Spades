using System;

[Serializable]
public class JagexUgcData
{
    public bool use_overhead_image;

    public string description;

    public string title;

    public JagexUgcEntity[] ugc_entities =
        Array.Empty<JagexUgcEntity>();

    public string skybox_name;

    public long aos_ugc_handle;

    public string author;

    public string baseplate;

    public bool modified_since_publish;

    public string[] tags =
        Array.Empty<string>();
}

[Serializable]
public class JagexUgcEntity
{
    public int[] position =
        Array.Empty<int>();

    public string mode;

    public string item;

    public int X =>
        position != null &&
        position.Length == 3
            ? position[0]
            : 0;

    public int Y =>
        position != null &&
        position.Length == 3
            ? position[1]
            : 0;

    public int Z =>
        position != null &&
        position.Length == 3
            ? position[2]
            : 0;
}