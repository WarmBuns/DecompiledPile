using UnityEngine;

namespace RoR2.UI;

public class RunTimerUIController : MonoBehaviour
{
	public TimerText runStopwatchTimerTextController;

	public SpriteAsNumberManager spriteAsNumberManager;

	private void Start()
	{
		if ((bool)runStopwatchTimerTextController && runStopwatchTimerTextController.TryGetComponent<HGTextMeshProUGUI>(out var component))
		{
			component.enabled = true;
		}
		if ((bool)spriteAsNumberManager)
		{
			spriteAsNumberManager.gameObject.SetActive(value: false);
		}
	}

	private void Update()
	{
		if ((bool)runStopwatchTimerTextController)
		{
			runStopwatchTimerTextController.seconds = (Run.instance ? Run.instance.GetRunStopwatch() : 0f);
		}
		else if ((bool)spriteAsNumberManager)
		{
			spriteAsNumberManager.SetTimerValue(Run.instance ? ((int)Run.instance.GetRunStopwatch()) : 0);
		}
	}
}
