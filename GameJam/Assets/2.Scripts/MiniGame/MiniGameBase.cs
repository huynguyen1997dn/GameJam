using System;
using UnityEngine;

public partial class EventId
{
    public const string PreCompleteGame = "PreCompleteGame";

    public const string CompleteGame = "CompleteGame";
    public const string FailGame = "FailGame";
    public const string MiniGameProgressUpdate = "MiniGameProgressUpdate";
    public const string MiniGamePhaseChanged = "MiniGamePhaseChanged";

}
public abstract class MiniGameBase : MonoBehaviour
{
    public abstract MiniGameType MiniGameType { get; }
    public abstract string MiniGameId { get; }

    public event Action OnGameComplete;
    public event Action OnGameFailed;

    // When true, CompleteGame only fires OnGameComplete without dispatching global
    // events - used when this game runs as a phase inside a combo minigame.
    public bool SuppressCompleteEvents { get; set; }

    public virtual void Init() { }
    public virtual void StartGame() { }
    public virtual void EndGame() { }

    protected void CompleteGame()
    {
        OnGameComplete?.Invoke();
        if (!SuppressCompleteEvents)
            EventDispatcher.Dispatch(EventId.PreCompleteGame);
    }

    protected void FailGame()
    {
        OnGameFailed?.Invoke();
        EventDispatcher.Dispatch(EventId.FailGame);


    }
}
