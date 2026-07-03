using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameLevelConfigs", menuName = "GamePlay/GameLevel/GameLevelConfigs", order = 1)]

public class GameLevelConfigs : ScriptableObject
{
    public List<GameLevelConfig> configs;

    public GameLevelConfig GetConfig(int index)
    {
        if (index < configs.Count)
        {
            return configs[index];
        }
        System.Random rng = new System.Random(index);

        // Random.seed = index;
        GameLevelConfig result = new();
        result.seed = index;
            
        result.width = rng.Next(8,10);
        result.height = result.width;
        result.minSnakeSize = rng.Next(3,5);
        result.maxSnakeSize = rng.Next(5,6);
        result.snakeCount = rng.Next(5,7);
        result.gridData = new int[result.width * result.width];

        

        
        return result;
    }
}
