using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Level_N", menuName = "GamePlay/GameLevel/GameLevelConfig", order = 1)]

public class GameLevelConfig : ScriptableObject
{
    [System.Serializable]
    public class SegmentSkinConfig
    {
        public int segmentValue;
        public int skinIndex; // -1 = random from skinPool; >= 0 = fixed skin index
    }

    [System.Serializable]
    public class Segment
    {
        public int value;
        public int startX;
        public int startY;
        public int endX;
        public int endY;
        public Color color = Color.white;
        public int[] bodyIndices = new int[0];

        public bool IsHeadOrTailPosition(int x, int y)
        {
            return (x == startX && y == startY) || (x == endX && y == endY);
        }

        public bool ContainsPosition(int x, int y)
        {
            if (IsHeadOrTailPosition(x, y))
                return true;
            if (bodyIndices == null)
                return false;
            // int w = GameLevelConfig.Instance.Width;
            // foreach (int idx in bodyIndices)
            // {
            //     if (idx / w == y && idx % w == x)
            //         return true;
            // }
            return false;
        }
    }

    public int head = 3;

    [SerializeField] public int width = 5;
    [SerializeField] public int height = 5;
    [SerializeField] public int[] gridData;
    [SerializeField] private List<Segment> segments = new List<Segment>();

    [Header("Skin")]
    [SerializeField] public List<int> skinPool = new List<int>();
    [SerializeField] public List<SegmentSkinConfig> skinOverrides = new List<SegmentSkinConfig>();

    [Header("Generation")]
    [Min(1)] public int snakeCount = 3;
    [Min(2)] public int minSnakeSize = 4;
    [Min(2)] public int maxSnakeSize = 7;
    public int seed = 0;
    public bool autoGenerateAtRuntime = true;

    public int Width => width;
    public int Height => height;

    public List<Segment> Segments => segments;

    private void OnEnable()
    {
        if (gridData == null || gridData.Length != width * height)
        {
            InitializeGrid();
        }
    }

    private void InitializeGrid()
    {
        int newSize = width * height;
        gridData = new int[newSize];
        for (int i = 0; i < newSize; i++)
        {
            gridData[i] = -1;
        }
    }

    public void ResizeGrid(int newWidth, int newHeight)
    {
        newWidth = Mathf.Max(1, newWidth);
        newHeight = Mathf.Max(1, newHeight);

        if (newWidth == width && newHeight == height)
            return;

        int oldWidth = width;
        int oldHeight = height;

        width = newWidth;
        height = newHeight;

        int newSize = width * height;
        int[] newGrid = new int[newSize];

        for (int i = 0; i < newSize; i++)
        {
            newGrid[i] = -1;
        }

        if (gridData != null)
        {
            for (int y = 0; y < oldHeight; y++)
            {
                for (int x = 0; x < oldWidth; x++)
                {
                    int oldIndex = y * oldWidth + x;
                    int newIndex = y * width + x;
                    if (oldIndex < gridData.Length && newIndex < newSize)
                    {
                        newGrid[newIndex] = gridData[oldIndex];
                    }
                }
            }
        }

        gridData = newGrid;

        if (gridData != null)
        {
            width = newWidth;
            height = newHeight;
        }
    }

    public int GetValue(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return -1;

        int index = y * width + x;
        return gridData != null && index < gridData.Length ? gridData[index] : -1;
    }

    public void SetValue(int x, int y, int value)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return;

        int index = y * width + x;
        if (gridData != null && index < gridData.Length)
        {
            gridData[index] = value;
        }
    }

    public void AddSegment(int value, int startX, int startY, int endX, int endY)
    {
        if (value == -1) return;
        segments.RemoveAll(seg => seg.value == value);

        List<int> bodyIdxList = new List<int>();
        HashSet<int> visited = new HashSet<int>();

        visited.Add(startY * width + startX);

        int[] dx = { 0, 1, 0, -1, 1, -1, 1, -1 };
        int[] dy = { -1, 0, 1, 0, -1, -1, 1, 1 };

        int curX = startX, curY = startY;
        bool reachedTail = false;

        while (!reachedTail)
        {
            bool foundNext = false;
            for (int d = 0; d < 8; d++)
            {
                int nx = curX + dx[d];
                int ny = curY + dy[d];
                if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                if (GetValue(nx, ny) != value) continue;
                int nKey = ny * width + nx;
                if (visited.Contains(nKey)) continue;

                visited.Add(nKey);
                if (nx == endX && ny == endY) { reachedTail = true; foundNext = true; break; }
                bodyIdxList.Add(nKey);
                curX = nx; curY = ny; foundNext = true; break;
            }
            if (!foundNext) break;
        }

        segments.Add(new Segment
        {
            value = value,
            startX = startX,
            startY = startY,
            endX = endX,
            endY = endY,
            bodyIndices = bodyIdxList.ToArray()
        });
        segments.Sort((a, b) => a.value.CompareTo(b.value));
    }

    public void AddSegment(int value, int startX, int startY, int endX, int endY, int[] precomputedBodyIndices)
    {
        if (value == -1) return;
        segments.RemoveAll(seg => seg.value == value);

        segments.Add(new Segment
        {
            value       = value,
            startX      = startX,
            startY      = startY,
            endX        = endX,
            endY        = endY,
            bodyIndices = precomputedBodyIndices ?? new int[0]
        });
        segments.Sort((a, b) => a.value.CompareTo(b.value));
    }

    public void ClearSegments()
    {
        segments.Clear();
    }

    public Segment GetSegment(int value)
    {
        return segments.Find(seg => seg.value == value);
    }

    public int[] GetGridData()
    {
        return gridData;
    }

    public int GetSkinIndexForSegment(int segmentValue)
    {
        var cfg = skinOverrides?.Find(s => s.segmentValue == segmentValue);
        return cfg != null ? cfg.skinIndex : -1;
    }
}