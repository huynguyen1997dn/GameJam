using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class GameLevelConfigMigration
{
    [MenuItem("Tools/GameLevel/Migrate Old Assets to Row-Major", priority = 300)]
    public static void MigrateAll()
    {
        string[] guids = AssetDatabase.FindAssets("t:GameLevelConfig");
        List<GameLevelConfig> migrated = new List<GameLevelConfig>();

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            string fileName = Path.GetFileNameWithoutExtension(path);

            EditorUtility.DisplayProgressBar(
                "Migrating Assets",
                $"Checking {fileName}...",
                (float)i / guids.Length);

            GameLevelConfig config = AssetDatabase.LoadAssetAtPath<GameLevelConfig>(path);
            if (config == null || config.gridData == null || config.Width == 0)
                continue;

            if (NeedsMigration(config))
            {
                MigrateConfig(config);
                EditorUtility.SetDirty(config);
                migrated.Add(config);
            }
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Migration complete: {migrated.Count} assets migrated to row-major format.");
    }

    [MenuItem("Tools/GameLevel/Migrate Old Assets to Row-Major", true)]
    public static bool ValidateMigrate()
    {
        return true;
    }

    private static bool NeedsMigration(GameLevelConfig config)
    {
        int w = config.Width;
        int h = config.Height;
        if (w <= 0 || h <= 0 || config.gridData == null)
            return false;

        // For square grids, old CM formula x*W+y and new RM formula y*W+x
        // produce different data layouts but same coverage.
        // We detect old format by checking the first cell of last column.
        // Old CM: cell (W-1, 0) stored at gridData[(W-1)*W + 0] = gridData[W²-W]
        // New RM: cell (W-1, 0) should be at gridData[0*W + (W-1)] = gridData[W-1]
        // Non-square grids with W < H are guaranteed to be new (old CM was broken for them).

        // Check if data looks like old (CM) or new (RM) by testing (W-1, 0)
        int cmCellValue = config.GetValue(w - 1, 0);    // with current RM implementation
        int oldCmIndex = (w - 1) * w;                    // where old CM would have stored it

        // If GetValue(W-1, 0) reads gridData[0*W+(W-1)] from RM,
        // but old data was at gridData[(W-1)*W+0],
        // and (W-1, 0) might be empty in old format if (0, W-1) had a value...
        // This heuristic isn't reliable enough.

        // More reliable: check if width == height (old CM only worked for square)
        // Non-square grids couldn't have been created with old CM, so they must be new.
        if (w != h)
            return false;

        // For square grids, detect by checking an off-diagonal cell.
        // Old CM: SetValue(x, y) → gridData[x*W+y]
        // New RM: GetValue(x, y) → gridData[y*W+x]
        // For cell (0, 1): old stored at index 1, new reads index W.
        // If gridData[1] has value but gridData[W] is -1, likely old data.
        int cell01 = config.gridData.Length > 1 ? config.gridData[1] : -1;
        int cellW = config.gridData.Length > w ? config.gridData[w] : -1;

        if (cell01 != -1 && cellW == -1)
        {
            // grid[1] has data but grid[W] is empty.
            // Old CM: SetValue(0, 1) → grid[1]. New RM: GetValue(0, 1) → grid[W].
            // If grid[1] has data but grid[W] doesn't, likely old data transposed.
            // But this could also be legit data at (1,0) in RM format.
            // Refine: check if grid[1] matches segment at (0,1) or (1,0)
            foreach (var seg in config.Segments)
            {
                if (seg.startX == 0 && seg.startY == 1) return true;
                if (seg.endX == 0 && seg.endY == 1) return true;
                if (seg.bodyIndices != null)
                {
                    foreach (int bi in seg.bodyIndices)
                    {
                        int bx = bi % w;
                        int by = bi / w;
                        if (bx == 0 && by == 1) return true;
                    }
                }
            }
        }

        // Default: assume new format for safety (no false migration)
        return false;
    }

    private static void MigrateConfig(GameLevelConfig config)
    {
        int w = config.Width;
        int h = config.Height;
        int[] oldGrid = config.gridData;
        int[] newGrid = new int[oldGrid.Length];

        for (int i = 0; i < newGrid.Length; i++)
            newGrid[i] = -1;

        // Transpose from CM (x*W+y) to RM (y*W+x)
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int oldIndex = x * w + y;    // CM storage
                int newIndex = y * w + x;    // RM storage
                if (oldIndex < oldGrid.Length && newIndex < newGrid.Length)
                {
                    newGrid[newIndex] = oldGrid[oldIndex];
                }
            }
        }

        config.gridData = newGrid;
    }
}
