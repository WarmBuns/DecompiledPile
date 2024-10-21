using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.VoidRaidCrab;

public class FireFinalStand : BaseWardWipeState
{
	[SerializeField]
	public float duration;

	[SerializeField]
	public string muzzleName;

	[SerializeField]
	public GameObject muzzleFlashPrefab;

	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	[SerializeField]
	public string animationPlaybackRateParam;

	[SerializeField]
	public string enterSoundString;

	[SerializeField]
	public BuffDef requiredBuffToKill;

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation(animationLayerName, animationStateName, animationPlaybackRateParam, duration);
		Util.PlaySound(enterSoundString, base.gameObject);
		if ((bool)muzzleFlashPrefab)
		{
			EffectManager.SimpleMuzzleFlash(muzzleFlashPrefab, base.gameObject, muzzleName, transmit: false);
		}
		if (!fogDamageController)
		{
			return;
		}
		if (NetworkServer.active)
		{
			foreach (CharacterBody affectedBody in fogDamageController.GetAffectedBodies())
			{
				if (!requiredBuffToKill || affectedBody.HasBuff(requiredBuffToKill))
				{
					affectedBody.healthComponent.Suicide(base.gameObject, base.gameObject, DamageType.VoidDeath);
				}
			}
		}
		fogDamageController.enabled = false;
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
		return InterruptPriority.Pain;
	}
}
