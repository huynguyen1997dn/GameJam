using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Dialog/Dialog Phase", fileName = "DialogPhase_")]
public class DialogPhaseSO : ScriptableObject
{
    public PhaseId phaseId;
    public List<DialogEntry> entries = new();
}
