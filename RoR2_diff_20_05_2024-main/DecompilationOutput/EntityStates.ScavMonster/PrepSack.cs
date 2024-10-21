using RoR2;
using UnityEngine;

namespace EntityStates.ScavMonster;

public class PrepSack : SackBaseState
{
	public static float baseDuration;

	public static string sound;

	public static GameObject chargeEffectPrefab;

	private GameObject chargeInstance;

	private float duration;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		PlayCrossfade("Body", "PrepSack", "PrepSack.playbackRate", duration, 0.1f);
		Util.PlaySound(sound, base.gameObject);
		StartAimMode(duration);
		if ((bool)muzzleTransform && (bool)chargeEffectPrefab)
		{
			chargeInstance = Object.Instantiate(chargeEffectPrefab, muzzleTransform.position, muzzleTransform.rotation);
			chargeInstance.transform.parent = muzzleTransform;
			ScaleParticleSystemDuration component = chargeInstance.GetComponent<ScaleParticleSystemDuration>();
			if ((bool)component)
			{
				component.newDuration = duration;
			}
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextState(new ThrowSack());
		}
	}

	public override void OnExit()
	{
		base.OnExit();
		if ((bool)chargeInstance)
		{
			EntityState.Destroy(chargeInstance);
		}
	}
}
