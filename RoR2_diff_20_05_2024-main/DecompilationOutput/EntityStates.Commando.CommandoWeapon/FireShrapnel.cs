using RoR2;
using UnityEngine;

namespace EntityStates.Commando.CommandoWeapon;

public class FireShrapnel : BaseState
{
	public static GameObject effectPrefab;

	public static GameObject hitEffectPrefab;

	public static GameObject tracerEffectPrefab;

	public static float damageCoefficient;

	public static float blastRadius;

	public static float force;

	public static float minSpread;

	public static float maxSpread;

	public static int bulletCount;

	public static float baseDuration = 2f;

	public static string attackSoundString;

	public static float maxDistance;

	private float duration;

	private Ray modifiedAimRay;

	private static int FireLaserStateHash = Animator.StringToHash("FireLaser");

	private static int FireLaserParamHash = Animator.StringToHash("FireLaser.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		modifiedAimRay = GetAimRay();
		Util.PlaySound(attackSoundString, base.gameObject);
		string muzzleName = "MuzzleLaser";
		if ((bool)base.characterBody)
		{
			base.characterBody.SetAimTimer(2f);
		}
		PlayAnimation("Gesture", FireLaserStateHash, FireLaserParamHash, duration);
		if ((bool)effectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(effectPrefab, base.gameObject, muzzleName, transmit: false);
		}
		if (base.isAuthority)
		{
			if (Physics.Raycast(modifiedAimRay, out var hitInfo, 1000f, (int)LayerIndex.world.mask | (int)LayerIndex.CommonMasks.characterBodiesOrDefault))
			{
				BlastAttack blastAttack = new BlastAttack();
				blastAttack.attacker = base.gameObject;
				blastAttack.inflictor = base.gameObject;
				blastAttack.teamIndex = TeamComponent.GetObjectTeam(base.gameObject);
				blastAttack.baseDamage = damageStat * damageCoefficient;
				blastAttack.baseForce = force * 0.2f;
				blastAttack.position = hitInfo.point;
				blastAttack.radius = blastRadius;
				blastAttack.Fire();
			}
			BulletAttack bulletAttack = new BulletAttack();
			bulletAttack.owner = base.gameObject;
			bulletAttack.weapon = base.gameObject;
			bulletAttack.origin = modifiedAimRay.origin;
			bulletAttack.aimVector = modifiedAimRay.direction;
			bulletAttack.radius = 0.25f;
			bulletAttack.minSpread = minSpread;
			bulletAttack.maxSpread = maxSpread;
			bulletAttack.bulletCount = ((bulletCount > 0) ? ((uint)bulletCount) : 0u);
			bulletAttack.damage = 0f;
			bulletAttack.procCoefficient = 0f;
			bulletAttack.force = force;
			bulletAttack.tracerEffectPrefab = tracerEffectPrefab;
			bulletAttack.muzzleName = muzzleName;
			bulletAttack.hitEffectPrefab = hitEffectPrefab;
			bulletAttack.isCrit = Util.CheckRoll(critStat, base.characterBody.master);
			bulletAttack.Fire();
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
