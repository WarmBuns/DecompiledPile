using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI;

public class InfiniteTowerWaveProgressBar : MonoBehaviour
{
	[SerializeField]
	[Tooltip("The bar we're filling up")]
	private Image barImage;

	[SerializeField]
	private Animator animator;

	private InfiniteTowerWaveController waveController;

	private float previousFillAmount;

	private void OnEnable()
	{
		InfiniteTowerRun infiniteTowerRun = Run.instance as InfiniteTowerRun;
		if ((bool)infiniteTowerRun)
		{
			waveController = infiniteTowerRun.waveController;
		}
	}

	private void Update()
	{
		if (!waveController)
		{
			return;
		}
		float normalizedProgress = waveController.GetNormalizedProgress();
		if (normalizedProgress > previousFillAmount)
		{
			previousFillAmount = normalizedProgress;
			if ((bool)animator)
			{
				int layerIndex = animator.GetLayerIndex("Base");
				animator.Play("Idle", layerIndex);
				animator.Update(0f);
				animator.Play((normalizedProgress >= 1f) ? "Ready" : "Pulse", layerIndex);
			}
			if ((bool)barImage)
			{
				barImage.fillAmount = normalizedProgress;
			}
		}
	}
}
