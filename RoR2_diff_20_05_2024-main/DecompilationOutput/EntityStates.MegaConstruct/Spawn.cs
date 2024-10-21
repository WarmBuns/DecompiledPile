using RoR2;
using UnityEngine;

namespace EntityStates.MegaConstruct;

public class Spawn : BaseState
{
	[SerializeField]
	public float duration;

	[SerializeField]
	public string muzzleName;

	[SerializeField]
	public GameObject muzzleEffectPrefab;

	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	[SerializeField]
	public string animationPlaybackRateParam;

	[SerializeField]
	public int numPads;

	[SerializeField]
	public string padChildLocatorName;

	[SerializeField]
	public GameObject padPrefab;

	[SerializeField]
	public string enterSoundString;

	[SerializeField]
	public bool depleteStocksPrimary;

	[SerializeField]
	public bool depleteStocksSecondary;

	[SerializeField]
	public bool depleteStocksUtility;

	[SerializeField]
	public bool depleteStocksSpecial;

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation(animationLayerName, animationStateName, animationPlaybackRateParam, duration);
		if ((bool)muzzleEffectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(muzzleEffectPrefab, base.gameObject, muzzleName, transmit: false);
		}
		Util.PlaySound(enterSoundString, base.gameObject);
	}

	private void CheckForDepleteStocks(SkillSlot slot, bool deplete)
	{
		GenericSkill skill = base.skillLocator.GetSkill(slot);
		if (deplete && (bool)skill)
		{
			skill.RemoveAllStocks();
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override void OnExit()
	{
		if (base.isAuthority)
		{
			CheckForDepleteStocks(SkillSlot.Primary, depleteStocksPrimary);
			CheckForDepleteStocks(SkillSlot.Secondary, depleteStocksSecondary);
			CheckForDepleteStocks(SkillSlot.Utility, depleteStocksUtility);
			CheckForDepleteStocks(SkillSlot.Special, depleteStocksSpecial);
		}
		base.OnExit();
	}
}
