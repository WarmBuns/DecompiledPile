using System.Collections.Generic;
using RoR2.HudOverlay;
using RoR2.UI;
using UnityEngine;

namespace EntityStates.Railgunner.Backpack;

public class Offline : BaseBackpack
{
	[SerializeField]
	public float baseDuration;

	[SerializeField]
	public GameObject crosshairOverridePrefab;

	[SerializeField]
	public GameObject overlayPrefab;

	[SerializeField]
	public string overlayChildLocatorEntry;

	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	[SerializeField]
	public string animationPlaybackRateParam;

	private CrosshairUtils.OverrideRequest crosshairOverrideRequest;

	private OverlayController overlayController;

	private List<ImageFillController> fillUiList = new List<ImageFillController>();

	private float duration;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		PlayAnimation(animationLayerName, animationStateName, animationPlaybackRateParam, duration);
		if ((bool)crosshairOverridePrefab)
		{
			crosshairOverrideRequest = CrosshairUtils.RequestOverrideForBody(base.characterBody, crosshairOverridePrefab, CrosshairUtils.OverridePriority.Skill);
		}
		OverlayCreationParams overlayCreationParams = default(OverlayCreationParams);
		overlayCreationParams.prefab = overlayPrefab;
		overlayCreationParams.childLocatorEntry = overlayChildLocatorEntry;
		OverlayCreationParams overlayCreationParams2 = overlayCreationParams;
		overlayController = HudOverlayManager.AddOverlay(base.gameObject, overlayCreationParams2);
		overlayController.onInstanceAdded += OnOverlayInstanceAdded;
		overlayController.onInstanceRemove += OnOverlayInstanceRemoved;
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration)
		{
			outer.SetNextState(new Reboot());
		}
		if (!(duration > 0f))
		{
			return;
		}
		foreach (ImageFillController fillUi in fillUiList)
		{
			fillUi.SetTValue(base.fixedAge / duration);
		}
	}

	public override void OnExit()
	{
		crosshairOverrideRequest?.Dispose();
		if (overlayController != null)
		{
			overlayController.onInstanceAdded -= OnOverlayInstanceAdded;
			overlayController.onInstanceRemove -= OnOverlayInstanceRemoved;
			fillUiList.Clear();
			HudOverlayManager.RemoveOverlay(overlayController);
		}
		base.OnExit();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Death;
	}

	private void OnOverlayInstanceAdded(OverlayController controller, GameObject instance)
	{
		fillUiList.Add(instance.GetComponent<ImageFillController>());
	}

	private void OnOverlayInstanceRemoved(OverlayController controller, GameObject instance)
	{
		fillUiList.Remove(instance.GetComponent<ImageFillController>());
	}
}
