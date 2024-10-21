using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.VoidSurvivor.Weapon;

public class CrushBase : BaseState
{
	[SerializeField]
	public GameObject muzzleflashEffectPrefab;

	[SerializeField]
	public float baseDuration = 2f;

	[SerializeField]
	public string enterSoundString;

	[SerializeField]
	public float recoilAmplitude;

	[SerializeField]
	public float bloom;

	[SerializeField]
	public string muzzle;

	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	[SerializeField]
	public string animationPlaybackRateParam;

	[SerializeField]
	public float selfHealFraction;

	[SerializeField]
	public float corruptionChange;

	private float duration;

	public override void OnEnter()
	{
		base.OnEnter();
		GetAimRay();
		duration = baseDuration / attackSpeedStat;
		StartAimMode(duration + 2f);
		PlayAnimation(animationLayerName, animationStateName, animationPlaybackRateParam, duration);
		Util.PlaySound(enterSoundString, base.gameObject);
		AddRecoil(-1f * recoilAmplitude, -1.5f * recoilAmplitude, -0.25f * recoilAmplitude, 0.25f * recoilAmplitude);
		base.characterBody.AddSpreadBloom(bloom);
		if ((bool)muzzleflashEffectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(muzzleflashEffectPrefab, base.gameObject, muzzle, transmit: false);
		}
		if (NetworkServer.active)
		{
			ProcChainMask procChainMask = default(ProcChainMask);
			procChainMask.AddProc(ProcType.VoidSurvivorCrush);
			if (selfHealFraction > 0f)
			{
				base.healthComponent.HealFraction(selfHealFraction, procChainMask);
			}
			else
			{
				DamageInfo damageInfo = new DamageInfo();
				damageInfo.damage = (0f - base.healthComponent.fullCombinedHealth) * selfHealFraction;
				damageInfo.position = base.characterBody.corePosition;
				damageInfo.force = Vector3.zero;
				damageInfo.damageColorIndex = DamageColorIndex.Default;
				damageInfo.crit = false;
				damageInfo.attacker = null;
				damageInfo.inflictor = null;
				damageInfo.damageType = DamageType.NonLethal | DamageType.BypassArmor;
				damageInfo.procCoefficient = 0f;
				damageInfo.procChainMask = procChainMask;
				base.healthComponent.TakeDamage(damageInfo);
			}
			VoidSurvivorController component = GetComponent<VoidSurvivorController>();
			if ((bool)component)
			{
				component.AddCorruption(corruptionChange);
			}
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && base.fixedAge >= duration)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
