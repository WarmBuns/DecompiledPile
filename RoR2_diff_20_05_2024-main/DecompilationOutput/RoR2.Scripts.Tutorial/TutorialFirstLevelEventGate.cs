using UnityEngine;
using UnityEngine.Events;

namespace RoR2.Scripts.Tutorial;

public class TutorialFirstLevelEventGate : MonoBehaviour
{
	public UnityEvent ExecTutorialIsEnabled;

	public UnityEvent ExecTutorialIsDisabled;

	public void Exec()
	{
		if (TutorialManager.isTutorialEnabled && (bool)Run.instance && Run.instance.stageClearCount == 0)
		{
			ExecTutorialIsEnabled?.Invoke();
		}
		else
		{
			ExecTutorialIsDisabled?.Invoke();
		}
	}
}
