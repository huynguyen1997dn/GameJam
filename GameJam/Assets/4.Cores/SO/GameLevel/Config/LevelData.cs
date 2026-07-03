using System;

[Serializable]
public class LevelData
{
    public int XSize;
    public int YSize;
    public ArrowData[] Arrows;
}

[Serializable]
public class ArrowData
{
    public int Dx;
    public int Dy;
    public int X;
    public int Y;
    public int[] Indices;
    public int BendCount;
}
