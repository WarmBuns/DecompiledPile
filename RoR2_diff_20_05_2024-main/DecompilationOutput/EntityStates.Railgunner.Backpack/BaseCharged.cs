using System.Collections.Generic;
using RoR2;
using RoR2.HudOverlay;
using RoR2.Skills;
using RoR2.UI;
using UnityEngine;

namespace EntityStates.Railgunner.Backpack;

public abstract class BaseCharged : BaseBackpack
{
	[SerializeField]
	public float duration;

	[SerializeField]
	public SkillDef primaryOverride;

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

	private OverlayController overlayController;

	private GenericSkill primarySkill;

	private List<ImageFillController> fillUiList = new List<ImageFillController>();

	private CrosshairUtils.OverrideRequest crosshairOverrideRequest;

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation(animationLayerName, animationStateName, animationPlaybackRateParam, duration);
		SkillLocator component = base.gameObject.GetComponent<SkillLocator>();
		if ((bool)component && (bool)component.primary)
		{
			primarySkill = component.primary;
			primarySkill.SetSkillOverride(this, primaryOverride, GenericSkill.SkillOverridePriority.Contextual);
		}
		OverlayCreationParams overlayCreationParams = default(OverlayCreationParams);
		overlayCreationParams.prefab = overlayPrefab;
		overlayCreationParams.childLocatorEntry = overlayChildLocatorEntry;
		OverlayCreationParams overlayCreationParams2 = overlayCreationParams;
		overlayController = HudOverlayManager.AddOverlay(base.gameObject, overlayCreationParams2);
		overlayController.onInstanceAdded += OnOverlayInstanceAdded;
		overlayController.onInstanceRemove += OnOverlayInstanceRemoved;
		if ((bool)crosshairOverridePrefab)
		{
			crosshairOverrideRequest = CrosshairUtils.RequestOverrideForBody(base.characterBody, crosshairOverridePrefab, CrosshairUtils.OverridePriority.PrioritySkill);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (duration > 0f)
		{
			foreach (ImageFillController fillUi in fillUiList)
			{
				fillUi.SetTValue(1f - base.fixedAge / duration);
			}
		}
		if (base.fixedAge >= duration)
		{
			outer.SetNextState(InstantiateExpiredState());
		}
		base.characterBody.SetAimTimer(3f);
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
		if ((bool)base.skillLocator)
		{
			primarySkill.UnsetSkillOverride(this, primaryOverride, GenericSkill.SkillOverridePriority.Contextual);
		}
		base.OnExit();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Death;
	}

	protected abstract EntityState InstantiateExpiredState();

	private void OnOverlayInstanceAdded(OverlayController controller, GameObject instance)
	{
		fillUiList.Add(instance.GetComponent<ImageFillController>());
	}

	private void OnOverlayInstanceRemoved(OverlayController controller, GameObject instance)
	{
		fillUiList.Remove(instance.GetComponent<ImageFillController>());
	}
}
