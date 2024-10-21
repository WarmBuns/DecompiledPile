using RoR2;
using UnityEngine;

namespace EntityStates.ParentMonster;

public class GroundSlam : BaseState
{
	public static float duration = 3.5f;

	public static float damageCoefficient = 4f;

	public static float forceMagnitude = 16f;

	public static float radius = 3f;

	private BlastAttack attack;

	public static string attackSoundString;

	public static GameObject slamImpactEffect;

	public static GameObject meleeTrailEffectL;

	public static GameObject meleeTrailEffectR;

	private Animator modelAnimator;

	private Transform modelTransform;

	private bool hasAttacked;

	public override void OnEnter()
	{
		base.OnEnter();
		modelAnimator = GetModelAnimator();
		modelTransform = GetModelTransform();
		Util.PlayAttackSpeedSound(attackSoundString, base.gameObject, attackSpeedStat);
		PlayCrossfade("Body", "Slam", "Slam.playbackRate", duration, 0.1f);
		if ((bool)base.characterDirection)
		{
			base.characterDirection.moveVector = GetAimRay().direction;
		}
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if ((bool)modelAnimator && modelAnimator.GetFloat("Slam.hitBoxActive") > 0.5f && !hasAttacked)
		{
			if (base.isAuthority)
			{
				if ((bool)base.characterDirection)
				{
					base.characterDirection.moveVector = base.characterDirection.forward;
				}
				if ((bool)modelTransform)
				{
					Transform transform = FindModelChild("SlamZone");
					if ((bool)transform)
					{
						attack = new BlastAttack();
						attack.attacker = base.gameObject;
						attack.inflictor = base.gameObject;
						attack.teamIndex = TeamComponent.GetObjectTeam(base.gameObject);
						attack.baseDamage = damageStat * damageCoefficient;
						attack.baseForce = forceMagnitude;
						attack.position = transform.position;
						attack.radius = radius;
						attack.Fire();
					}
				}
			}
			hasAttacked = true;
		}
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}
