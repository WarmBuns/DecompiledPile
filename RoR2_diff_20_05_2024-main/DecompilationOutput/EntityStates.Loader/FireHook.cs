using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.Loader;

public class FireHook : BaseSkillState
{
	[SerializeField]
	public GameObject projectilePrefab;

	public static float damageCoefficient;

	public static GameObject muzzleflashEffectPrefab;

	public static string fireSoundString;

	public GameObject hookInstance;

	protected ProjectileStickOnImpact hookStickOnImpact;

	private bool isStuck;

	private bool hadHookInstance;

	private uint soundID;

	private static int FireHookIntroStateHash = Animator.StringToHash("FireHookIntro");

	private static int FireHookLoopStateHash = Animator.StringToHash("FireHookLoop");

	private static int FireHookExitStateHash = Animator.StringToHash("FireHookExit");

	public override void OnEnter()
	{
		base.OnEnter();
		if (base.isAuthority)
		{
			Ray ray = GetAimRay();
			TrajectoryAimAssist.ApplyTrajectoryAimAssist(ref ray, projectilePrefab, base.gameObject);
			FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
			fireProjectileInfo.position = ray.origin;
			fireProjectileInfo.rotation = Quaternion.LookRotation(ray.direction);
			fireProjectileInfo.crit = base.characterBody.RollCrit();
			fireProjectileInfo.damage = damageStat * damageCoefficient;
			fireProjectileInfo.force = 0f;
			fireProjectileInfo.damageColorIndex = DamageColorIndex.Default;
			fireProjectileInfo.procChainMask = default(ProcChainMask);
			fireProjectileInfo.projectilePrefab = projectilePrefab;
			fireProjectileInfo.owner = base.gameObject;
			FireProjectileInfo fireProjectileInfo2 = fireProjectileInfo;
			ProjectileManager.instance.FireProjectile(fireProjectileInfo2);
		}
		EffectManager.SimpleMuzzleFlash(muzzleflashEffectPrefab, base.gameObject, "MuzzleLeft", transmit: false);
		Util.PlaySound(fireSoundString, base.gameObject);
		PlayAnimation("Grapple", FireHookIntroStateHash);
	}

	public void SetHookReference(GameObject hook)
	{
		hookInstance = hook;
		hookStickOnImpact = hook.GetComponent<ProjectileStickOnImpact>();
		hadHookInstance = true;
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if ((bool)hookStickOnImpact)
		{
			if (hookStickOnImpact.stuck && !isStuck)
			{
				PlayAnimation("Grapple", FireHookLoopStateHash);
			}
			isStuck = hookStickOnImpact.stuck;
		}
		if (base.isAuthority && !hookInstance && hadHookInstance)
		{
			outer.SetNextStateToMain();
		}
	}

	public override void OnExit()
	{
		PlayAnimation("Grapple", FireHookExitStateHash);
		EffectManager.SimpleMuzzleFlash(muzzleflashEffectPrefab, base.gameObject, "MuzzleLeft", transmit: false);
		base.OnExit();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Pain;
	}
}
