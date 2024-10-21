using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.GreaterWispMonster;

public class FireCannons : BaseState
{
	[SerializeField]
	public GameObject projectilePrefab;

	[SerializeField]
	public GameObject effectPrefab;

	public static float baseDuration = 2f;

	[SerializeField]
	public float damageCoefficient = 1.2f;

	[SerializeField]
	public float force = 20f;

	private float duration;

	private static int FireCannonsStateHash = Animator.StringToHash("FireCannons");

	private static int FireCannonsParamHash = Animator.StringToHash("FireCannons.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		Ray aimRay = GetAimRay();
		string text = "MuzzleLeft";
		string text2 = "MuzzleRight";
		duration = baseDuration / attackSpeedStat;
		if ((bool)effectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(effectPrefab, base.gameObject, text, transmit: false);
			EffectManager.SimpleMuzzleFlash(effectPrefab, base.gameObject, text2, transmit: false);
		}
		PlayAnimation("Gesture", FireCannonsStateHash, FireCannonsParamHash, duration);
		if (!base.isAuthority || !base.modelLocator || !base.modelLocator.modelTransform)
		{
			return;
		}
		ChildLocator component = base.modelLocator.modelTransform.GetComponent<ChildLocator>();
		if ((bool)component)
		{
			int childIndex = component.FindChildIndex(text);
			int childIndex2 = component.FindChildIndex(text2);
			Transform transform = component.FindChild(childIndex);
			Transform transform2 = component.FindChild(childIndex2);
			if ((bool)transform)
			{
				ProjectileManager.instance.FireProjectile(projectilePrefab, transform.position, Util.QuaternionSafeLookRotation(aimRay.direction), base.gameObject, damageStat * damageCoefficient, force, Util.CheckRoll(critStat, base.characterBody.master));
			}
			if ((bool)transform2)
			{
				ProjectileManager.instance.FireProjectile(projectilePrefab, transform2.position, Util.QuaternionSafeLookRotation(aimRay.direction), base.gameObject, damageStat * damageCoefficient, force, Util.CheckRoll(critStat, base.characterBody.master));
			}
		}
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
