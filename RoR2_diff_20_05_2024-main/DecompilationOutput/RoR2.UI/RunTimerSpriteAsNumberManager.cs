using UnityEngine;

namespace RoR2.UI;

public class RunTimerSpriteAsNumberManager : MonoBehaviour
{
	public SpriteAsNumberManager spriteAsNumberManager;

	private void Update()
	{
		if ((bool)spriteAsNumberManager)
		{
			spriteAsNumberManager.SetTimerValue(Run.instance ? ((int)Run.instance.GetRunStopwatch()) : 0);
		}
	}
}
