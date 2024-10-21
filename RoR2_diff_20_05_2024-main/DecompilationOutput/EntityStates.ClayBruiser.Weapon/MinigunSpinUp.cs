using RoR2;
using UnityEngine;

namespace EntityStates.ClayBruiser.Weapon;

public class MinigunSpinUp : MinigunState
{
	public static float baseDuration;

	public static string sound;

	public static GameObject chargeEffectPrefab;

	private GameObject chargeInstance;

	private float duration;

	private static int WeaponIsReadyParamHash = Animator.StringToHash("WeaponIsReady");

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		Util.PlaySound(sound, base.gameObject);
		GetModelAnimator().SetBool(WeaponIsReadyParamHash, value: true);
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
			outer.SetNextState(new MinigunFire());
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
