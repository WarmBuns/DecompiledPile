using RoR2;
using UnityEngine;

namespace EntityStates.VoidRaidCrab.Weapon;

public abstract class BaseFireMultiBeam : BaseMultiBeamState
{
	[SerializeField]
	public float baseDuration;

	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	[SerializeField]
	public string animationPlaybackRateParam;

	[SerializeField]
	public GameObject muzzleEffectPrefab;

	[SerializeField]
	public GameObject tracerEffectPrefab;

	[SerializeField]
	public GameObject explosionEffectPrefab;

	[SerializeField]
	public float blastDamageCoefficient;

	[SerializeField]
	public float blastForceMagnitude;

	[SerializeField]
	public float blastRadius;

	[SerializeField]
	public Vector3 blastBonusForce;

	[SerializeField]
	public string enterSoundString;

	private float duration;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		Transform modelTransform = GetModelTransform();
		PlayAnimation(animationLayerName, animationStateName, animationPlaybackRateParam, duration);
		if ((bool)muzzleEffectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(muzzleEffectPrefab, base.gameObject, BaseMultiBeamState.muzzleName, transmit: false);
		}
		Util.PlayAttackSpeedSound(enterSoundString, base.gameObject, attackSpeedStat);
		if (!base.isAuthority)
		{
			return;
		}
		CalcBeamPath(out var beamRay, out var beamEndPos);
		BlastAttack blastAttack = new BlastAttack();
		blastAttack.attacker = base.gameObject;
		blastAttack.inflictor = base.gameObject;
		blastAttack.teamIndex = TeamComponent.GetObjectTeam(base.gameObject);
		blastAttack.baseDamage = damageStat * blastDamageCoefficient;
		blastAttack.baseForce = blastForceMagnitude;
		blastAttack.position = beamEndPos;
		blastAttack.radius = blastRadius;
		blastAttack.falloffModel = BlastAttack.FalloffModel.SweetSpot;
		blastAttack.bonusForce = blastBonusForce;
		blastAttack.damageType = DamageType.Generic;
		blastAttack.Fire();
		if ((bool)modelTransform)
		{
			ChildLocator component = modelTransform.GetComponent<ChildLocator>();
			if ((bool)component)
			{
				int childIndex = component.FindChildIndex(BaseMultiBeamState.muzzleName);
				if ((bool)tracerEffectPrefab)
				{
					EffectData effectData = new EffectData
					{
						origin = beamEndPos,
						start = beamRay.origin,
						scale = blastRadius
					};
					effectData.SetChildLocatorTransformReference(base.gameObject, childIndex);
					EffectManager.SpawnEffect(tracerEffectPrefab, effectData, transmit: true);
					EffectManager.SpawnEffect(explosionEffectPrefab, effectData, transmit: true);
				}
			}
		}
		OnFireBeam(beamRay.origin, beamEndPos);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration && base.isAuthority)
		{
			EntityState nextState = InstantiateNextState();
			outer.SetNextState(nextState);
		}
	}

	protected abstract EntityState InstantiateNextState();

	protected virtual void OnFireBeam(Vector3 beamStart, Vector3 beamEnd)
	{
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}
