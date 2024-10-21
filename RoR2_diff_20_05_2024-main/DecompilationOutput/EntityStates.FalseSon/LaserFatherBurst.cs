using RoR2;
using UnityEngine;

namespace EntityStates.FalseSon;

public class LaserFatherBurst : BaseState
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

	public static float laserTimerPercent = 0.12f;

	public Vector3 laserDirection;

	private float duration;

	private Ray modifiedAimRay;

	public float charge;

	private float maxSecondaryStock;

	private float secondaryStock;

	private Transform modelTransform;

	protected Transform muzzleTransform;

	private string targetMuzzle;

	private float rayDistance;

	private bool firedLaser;

	public override void OnEnter()
	{
		base.OnEnter();
		if ((bool)base.characterBody)
		{
			base.characterBody.SetAimTimer(2f);
		}
		duration = baseDuration / attackSpeedStat;
		PlayCrossfade("Gesture, Override", "FireLaser", "FireLaser.playbackRate", duration, 0.1f);
		PlayCrossfade("Gesture, Additive", "FireLaser", "FireLaser.playbackRate", duration, 0.1f);
		maxSecondaryStock = base.skillLocator.GetSkill(SkillSlot.Secondary).maxStock;
		secondaryStock = base.skillLocator.GetSkill(SkillSlot.Secondary).stock;
		int num = (int)(maxSecondaryStock * 0.3f);
		int num2 = (int)maxSecondaryStock - (int)secondaryStock;
		if (num2 > 0)
		{
			if (num2 < num)
			{
				base.skillLocator.GetSkill(SkillSlot.Secondary).stock = base.skillLocator.GetSkill(SkillSlot.Secondary).maxStock;
			}
			else
			{
				base.skillLocator.GetSkill(SkillSlot.Secondary).stock = base.skillLocator.GetSkill(SkillSlot.Secondary).stock + num;
			}
		}
	}

	public void FireBurstLaser()
	{
		modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			ChildLocator component = modelTransform.GetComponent<ChildLocator>();
			if ((bool)component)
			{
				muzzleTransform = component.FindChild("MuzzleLaser");
			}
		}
		rayDistance = 1000f;
		Ray aimRay = GetAimRay();
		Vector3 position = muzzleTransform.transform.parent.position;
		Vector3 point = aimRay.GetPoint(rayDistance);
		laserDirection = point - position;
		modifiedAimRay = GetAimRay();
		modifiedAimRay.direction = laserDirection;
		TrajectoryAimAssist.ApplyTrajectoryAimAssist(ref modifiedAimRay, rayDistance, base.gameObject);
		GetModelAnimator();
		Util.PlaySound(attackSoundString, base.gameObject);
		targetMuzzle = "MuzzleLaser";
		if ((bool)effectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(effectPrefab, base.gameObject, targetMuzzle, transmit: false);
		}
		if (!base.isAuthority)
		{
			return;
		}
		rayDistance = 200f;
		Vector3 vector = modifiedAimRay.origin + modifiedAimRay.direction * rayDistance;
		if (Physics.Raycast(modifiedAimRay, out var hitInfo, rayDistance, LayerIndex.CommonMasks.laser))
		{
			vector = hitInfo.point;
		}
		BlastAttack blastAttack = new BlastAttack();
		blastAttack.attacker = base.gameObject;
		blastAttack.inflictor = base.gameObject;
		blastAttack.teamIndex = TeamComponent.GetObjectTeam(base.gameObject);
		blastAttack.baseDamage = damageStat * damageCoefficient;
		blastAttack.baseForce = force * 0.2f;
		blastAttack.position = vector;
		blastAttack.radius = blastRadius;
		blastAttack.falloffModel = BlastAttack.FalloffModel.SweetSpot;
		blastAttack.bonusForce = force * modifiedAimRay.direction;
		blastAttack.Fire();
		_ = modifiedAimRay.origin;
		if (!modelTransform)
		{
			return;
		}
		ChildLocator component2 = modelTransform.GetComponent<ChildLocator>();
		if ((bool)component2)
		{
			int childIndex = component2.FindChildIndex(targetMuzzle);
			if ((bool)tracerEffectPrefab)
			{
				EffectData effectData = new EffectData
				{
					origin = vector,
					start = modifiedAimRay.origin
				};
				effectData.SetChildLocatorTransformReference(base.gameObject, childIndex);
				EffectManager.SpawnEffect(tracerEffectPrefab, effectData, transmit: true);
				EffectManager.SpawnEffect(hitEffectPrefab, effectData, transmit: true);
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
		if (base.fixedAge >= duration * laserTimerPercent && !firedLaser)
		{
			firedLaser = true;
			FireBurstLaser();
		}
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
