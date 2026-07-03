using System;
using UnityEngine;

public abstract class MiniGameBase : MonoBehaviour
{
    public abstract MiniGameType MiniGameType { get; }
    public abstract string MiniGameId { get; }

    public event Action OnGameComplete;
    public event Action OnGameFailed;

    public virtual void Init() { }
    public virtual void StartGame() { }
    public virtual void EndGame() { }

    protected void CompleteGame()
    {
        OnGameComplete?.Invoke();
    }

    protected void FailGame()
    {
        OnGameFailed?.Invoke();
    }
}
