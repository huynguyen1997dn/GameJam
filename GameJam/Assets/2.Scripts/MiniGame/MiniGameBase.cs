using System;
using UnityEngine;

public partial class EventId
{
    public const string CompleteGame = "CompleteGame";
    public const string FailGame = "FailGame";
    public const string MiniGameProgressUpdate = "MiniGameProgressUpdate";

}
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
        EventDispatcher.Dispatch(EventId.CompleteGame);
    }

    protected void FailGame()
    {
        OnGameFailed?.Invoke();
        EventDispatcher.Dispatch(EventId.FailGame);


    }
}
