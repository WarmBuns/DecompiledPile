using System.Collections.Generic;
using RoR2;
using RoR2.Audio;
using RoR2.HudOverlay;
using RoR2.Skills;
using RoR2.UI;
using UnityEngine;

namespace EntityStates.Railgunner.Reload;

public class Reloading : BaseState
{
	[SerializeField]
	public float baseDuration;

	[SerializeField]
	public float boostWindowDelay;

	[SerializeField]
	public float boostWindowDuration;

	[SerializeField]
	public float boostGracePeriod;

	[SerializeField]
	public SkillDef primaryOverride;

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

	[SerializeField]
	public LoopSoundDef loopSoundDef;

	[SerializeField]
	public string endNoBoostSoundString;

	[SerializeField]
	public string failSoundString;

	private float duration;

	private float adjustedBoostWindowDelay;

	private float adjustedBoostWindowDuration;

	private GenericSkill primarySkill;

	private OverlayController overlayController;

	private List<ActiveReloadBarController> reloadUiList = new List<ActiveReloadBarController>();

	private bool hasAttempted;

	private LoopSoundManager.SoundLoopPtr loopPtr;

	public bool AttemptBoost()
	{
		if (IsInBoostWindow())
		{
			outer.SetNextState(new BoostConfirm());
			return true;
		}
		foreach (ActiveReloadBarController reloadUi in reloadUiList)
		{
			reloadUi.SetWasFailure(wasFailure: true);
		}
		return false;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		float num = boostWindowDelay + boostWindowDuration;
		float num2 = baseDuration - num;
		duration = num + num2 / attackSpeedStat;
		adjustedBoostWindowDelay = Mathf.Min(boostWindowDelay / baseDuration * duration, boostWindowDelay);
		adjustedBoostWindowDuration = Mathf.Max((boostWindowDelay + boostWindowDuration) / baseDuration * duration, num) - adjustedBoostWindowDelay;
		if ((bool)loopSoundDef)
		{
			loopPtr = LoopSoundManager.PlaySoundLoopLocalRtpc(base.gameObject, loopSoundDef, "attackSpeed", Util.CalculateAttackSpeedRtpcValue(attackSpeedStat));
		}
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
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		float num = ((duration > 0f) ? (base.fixedAge / duration) : 0f);
		foreach (ActiveReloadBarController reloadUi in reloadUiList)
		{
			reloadUi.SetTValue(num);
			reloadUi.SetIsWindowActive(IsInBoostWindow());
		}
		if (duration <= 0f || num >= 1f)
		{
			Util.PlaySound(endNoBoostSoundString, base.gameObject);
			outer.SetNextState(new Waiting());
		}
		if ((bool)base.inputBank && base.inputBank.skill1.justPressed && !hasAttempted)
		{
			hasAttempted = true;
			if (!AttemptBoost())
			{
				Util.PlaySound(failSoundString, base.gameObject);
			}
		}
	}

	public override void OnExit()
	{
		if (overlayController != null)
		{
			overlayController.onInstanceAdded -= OnOverlayInstanceAdded;
			overlayController.onInstanceRemove -= OnOverlayInstanceRemoved;
			reloadUiList.Clear();
		}
		if ((bool)base.skillLocator)
		{
			primarySkill.UnsetSkillOverride(this, primaryOverride, GenericSkill.SkillOverridePriority.Contextual);
		}
		for (int i = 0; i < base.skillLocator.skillSlotCount; i++)
		{
			GenericSkill skillAtIndex = base.skillLocator.GetSkillAtIndex(i);
			if ((bool)skillAtIndex)
			{
				RailgunSkillDef railgunSkillDef = skillAtIndex.skillDef as RailgunSkillDef;
				if ((bool)railgunSkillDef && railgunSkillDef.restockOnReload)
				{
					skillAtIndex.stock = skillAtIndex.maxStock;
				}
			}
		}
		if (loopPtr.isValid)
		{
			LoopSoundManager.StopSoundLoopLocal(loopPtr);
		}
		base.OnExit();
	}

	public bool IsInBoostWindow()
	{
		if (adjustedBoostWindowDelay - base.fixedAge < boostGracePeriod)
		{
			return base.fixedAge - (adjustedBoostWindowDelay + adjustedBoostWindowDuration) < boostGracePeriod;
		}
		return false;
	}

	public override void ModifyNextState(EntityState nextState)
	{
		if (nextState is BoostConfirm boostConfirm)
		{
			boostConfirm.overlayController = overlayController;
			if ((bool)primarySkill)
			{
				boostConfirm.OverridePrimary(primarySkill, primaryOverride);
			}
		}
		else if (overlayController != null)
		{
			HudOverlayManager.RemoveOverlay(overlayController);
		}
	}

	private void OnOverlayInstanceAdded(OverlayController controller, GameObject instance)
	{
		ActiveReloadBarController component = instance.GetComponent<ActiveReloadBarController>();
		float num = ((duration > 0f) ? (1f / duration) : 0f);
		component.SetWindowRange(num * adjustedBoostWindowDelay, num * (adjustedBoostWindowDelay + adjustedBoostWindowDuration));
		reloadUiList.Add(component);
	}

	private void OnOverlayInstanceRemoved(OverlayController controller, GameObject instance)
	{
		ActiveReloadBarController component = instance.GetComponent<ActiveReloadBarController>();
		reloadUiList.Remove(component);
	}
}
