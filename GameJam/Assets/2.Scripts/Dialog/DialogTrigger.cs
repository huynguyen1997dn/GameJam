using UnityEngine;

public class DialogTrigger : MonoBehaviour
{
    [SerializeField] private PhaseId phaseId;

    public void Trigger()
    {
        UIManager.Instance.OnShowPopup(PopupId.DialogPopup, phaseId);
    }

    public void TriggerWithPhase(PhaseId targetPhase)
    {
        phaseId = targetPhase;
        UIManager.Instance.OnShowPopup(PopupId.DialogPopup, targetPhase);
    }
}
