using RoR2;
using RoR2.Audio;
using UnityEngine;

namespace EntityStates.VoidRaidCrab;

public class SpinBeamAttack : BaseSpinBeamAttackState
{
	public static AnimationCurve revolutionsCurve;

	public static GameObject beamVfxPrefab;

	public static float beamRadius = 16f;

	public static float beamMaxDistance = 400f;

	public static float beamDpsCoefficient = 1f;

	public static float beamTickFrequency = 4f;

	public static GameObject beamImpactEffectPrefab;

	public static LoopSoundDef loopSound;

	public static string enterSoundString;

	private float beamTickTimer;

	private LoopSoundManager.SoundLoopPtr loopPtr;

	public override void OnEnter()
	{
		base.OnEnter();
		CreateBeamVFXInstance(beamVfxPrefab);
		loopPtr = LoopSoundManager.PlaySoundLoopLocal(base.gameObject, loopSound);
		Util.PlaySound(enterSoundString, base.gameObject);
	}

	public override void OnExit()
	{
		LoopSoundManager.StopSoundLoopLocal(loopPtr);
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= base.duration && base.isAuthority)
		{
			outer.SetNextState(new SpinBeamWindDown());
		}
		if (base.isAuthority)
		{
			if (beamTickTimer <= 0f)
			{
				beamTickTimer += 1f / beamTickFrequency;
				FireBeamBulletAuthority();
			}
			beamTickTimer -= GetDeltaTime();
		}
		SetHeadYawRevolutions(revolutionsCurve.Evaluate(base.normalizedFixedAge));
	}

	private void FireBeamBulletAuthority()
	{
		Ray beamRay = GetBeamRay();
		BulletAttack bulletAttack = new BulletAttack();
		bulletAttack.muzzleName = BaseSpinBeamAttackState.muzzleTransformNameInChildLocator;
		bulletAttack.origin = beamRay.origin;
		bulletAttack.aimVector = beamRay.direction;
		bulletAttack.minSpread = 0f;
		bulletAttack.maxSpread = 0f;
		bulletAttack.maxDistance = 400f;
		bulletAttack.hitMask = LayerIndex.CommonMasks.bullet;
		bulletAttack.stopperMask = 0;
		bulletAttack.bulletCount = 1u;
		bulletAttack.radius = beamRadius;
		bulletAttack.smartCollision = false;
		bulletAttack.queryTriggerInteraction = QueryTriggerInteraction.Ignore;
		bulletAttack.procCoefficient = 1f;
		bulletAttack.procChainMask = default(ProcChainMask);
		bulletAttack.owner = base.gameObject;
		bulletAttack.weapon = base.gameObject;
		bulletAttack.damage = beamDpsCoefficient * damageStat / beamTickFrequency;
		bulletAttack.damageColorIndex = DamageColorIndex.Default;
		bulletAttack.damageType = DamageType.Generic;
		bulletAttack.falloffModel = BulletAttack.FalloffModel.None;
		bulletAttack.force = 0f;
		bulletAttack.hitEffectPrefab = beamImpactEffectPrefab;
		bulletAttack.tracerEffectPrefab = null;
		bulletAttack.isCrit = false;
		bulletAttack.HitEffectNormal = false;
		bulletAttack.Fire();
	}
}
