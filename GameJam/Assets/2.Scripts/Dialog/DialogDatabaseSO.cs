using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Dialog/Dialog Database", fileName = "DialogDatabase")]
public class DialogDatabaseSO : ScriptableObject
{
    [SerializeField] private List<DialogPhaseSO> phases = new();

    private Dictionary<PhaseId, DialogPhaseSO> _lookup;

    public DialogPhaseSO GetPhase(PhaseId phaseId)
    {
        if (_lookup == null)
            BuildLookup();

        _lookup.TryGetValue(phaseId, out var phase);
        return phase;
    }

    private void BuildLookup()
    {
        _lookup = new Dictionary<PhaseId, DialogPhaseSO>();
        foreach (var phase in phases.Where(p => p != null))
        {
            if (!_lookup.ContainsKey(phase.phaseId))
                _lookup.Add(phase.phaseId, phase);
        }
    }

#if UNITY_EDITOR
    public List<DialogPhaseSO> Phases => phases;
#endif
}
