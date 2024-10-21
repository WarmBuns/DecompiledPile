using RoR2;
using UnityEngine;

namespace EntityStates.MajorConstruct.Weapon;

public class TerminateLaser : BaseState
{
	[SerializeField]
	public float duration;

	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	[SerializeField]
	public string animationPlaybackParameterName;

	[SerializeField]
	public string muzzleName;

	[SerializeField]
	public GameObject muzzleEffectPrefab;

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

	private Vector3 blastPosition;

	public TerminateLaser()
	{
	}

	public TerminateLaser(Vector3 blastPosition)
	{
		this.blastPosition = blastPosition;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation(animationLayerName, animationStateName, animationPlaybackParameterName, duration);
		if ((bool)muzzleEffectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(muzzleEffectPrefab, base.gameObject, muzzleName, transmit: false);
		}
		Util.PlaySound(enterSoundString, base.gameObject);
		if (base.isAuthority)
		{
			BlastAttack blastAttack = new BlastAttack();
			blastAttack.attacker = base.gameObject;
			blastAttack.inflictor = base.gameObject;
			blastAttack.teamIndex = TeamComponent.GetObjectTeam(base.gameObject);
			blastAttack.baseDamage = damageStat * blastDamageCoefficient;
			blastAttack.baseForce = blastForceMagnitude;
			blastAttack.position = blastPosition;
			blastAttack.radius = blastRadius;
			blastAttack.bonusForce = blastBonusForce;
			blastAttack.Fire();
			EffectData effectData = new EffectData
			{
				origin = blastPosition,
				scale = blastRadius
			};
			EffectManager.SpawnEffect(explosionEffectPrefab, effectData, transmit: true);
		}
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
