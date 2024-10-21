using RoR2.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI;

public class InfiniteTowerTimeCounter : MonoBehaviour
{
	[SerializeField]
	[Tooltip("The root we're toggling")]
	private GameObject rootObject;

	[SerializeField]
	[Tooltip("The timer we're setting")]
	private TimerText timerText;

	[SerializeField]
	private Animator animator;

	[SerializeField]
	private Image barImage;

	[SerializeField]
	[Tooltip("The sound we loop while the timer is active")]
	private LoopSoundDef timerLoop;

	[SerializeField]
	[Tooltip("The sound we play on each second above criticalSecondsThreshold")]
	private string onSecondRegularSound;

	[SerializeField]
	[Tooltip("The sound we play on each second below criticalSecondsThreshold")]
	private string onSecondCriticalSound;

	[SerializeField]
	[Tooltip("Below this number of seconds remaining, we are 'critical'")]
	private float criticalSecondsThreshold;

	private InfiniteTowerWaveController waveController;

	private bool wasTimerActive;

	private LoopSoundManager.SoundLoopPtr loopPtr;

	private void OnEnable()
	{
		InfiniteTowerRun infiniteTowerRun = Run.instance as InfiniteTowerRun;
		if ((bool)infiniteTowerRun)
		{
			waveController = infiniteTowerRun.waveController;
		}
	}

	private void OnDisable()
	{
		LoopSoundManager.StopSoundLoopLocal(loopPtr);
	}

	private void Update()
	{
		if ((bool)waveController)
		{
			bool flag = waveController.secondsRemaining <= 0f;
			bool flag2 = waveController.isTimerActive;
			InfiniteTowerRun infiniteTowerRun = Run.instance as InfiniteTowerRun;
			if ((bool)infiniteTowerRun && infiniteTowerRun.IsStageTransitionWave())
			{
				flag2 = false;
			}
			if (flag && loopPtr.isValid)
			{
				LoopSoundManager.StopSoundLoopLocal(loopPtr);
			}
			rootObject.SetActive(flag2 && !flag);
			if ((bool)animator && !wasTimerActive && flag2)
			{
				int layerIndex = animator.GetLayerIndex("Base");
				animator.Play("Idle", layerIndex);
				animator.Update(0f);
				animator.Play("Finish", layerIndex);
			}
			wasTimerActive = flag2;
			if (flag2)
			{
				if ((bool)timerText)
				{
					if ((bool)timerLoop && !loopPtr.isValid && waveController.secondsRemaining > 0f)
					{
						loopPtr = LoopSoundManager.PlaySoundLoopLocal(base.gameObject, timerLoop);
					}
					int num = Mathf.FloorToInt((float)timerText.seconds);
					int num2 = Mathf.FloorToInt(waveController.secondsRemaining);
					if (num != num2)
					{
						if (waveController.secondsRemaining < criticalSecondsThreshold)
						{
							Util.PlaySound(onSecondRegularSound, RoR2Application.instance.gameObject);
						}
						else
						{
							Util.PlaySound(onSecondCriticalSound, RoR2Application.instance.gameObject);
						}
					}
					timerText.seconds = waveController.secondsRemaining;
				}
				if ((bool)barImage)
				{
					barImage.fillAmount = waveController.secondsRemaining / (float)waveController.secondsAfterWave;
				}
			}
			else if (loopPtr.isValid)
			{
				LoopSoundManager.StopSoundLoopLocal(loopPtr);
			}
		}
		else
		{
			rootObject.SetActive(value: false);
		}
	}
}
