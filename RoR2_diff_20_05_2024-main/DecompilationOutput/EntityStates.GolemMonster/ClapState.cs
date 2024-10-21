using RoR2;
using UnityEngine;

namespace EntityStates.GolemMonster;

public class ClapState : BaseState
{
	public static float duration = 3.5f;

	public static float damageCoefficient = 4f;

	public static float forceMagnitude = 16f;

	public static float radius = 3f;

	private BlastAttack attack;

	public static string attackSoundString;

	private Animator modelAnimator;

	private Transform modelTransform;

	private bool hasAttacked;

	private GameObject leftHandChargeEffect;

	private GameObject rightHandChargeEffect;

	private EffectManagerHelper _emh_leftHandChargeEffect;

	private EffectManagerHelper _emh_rightHandChargeEffect;

	public override void Reset()
	{
		base.Reset();
		modelAnimator = null;
		modelTransform = null;
		hasAttacked = false;
		leftHandChargeEffect = null;
		rightHandChargeEffect = null;
		if (attack != null)
		{
			attack.Reset();
		}
		_emh_leftHandChargeEffect = null;
		_emh_rightHandChargeEffect = null;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		modelAnimator = GetModelAnimator();
		modelTransform = GetModelTransform();
		Util.PlayAttackSpeedSound(attackSoundString, base.gameObject, attackSpeedStat);
		PlayCrossfade("Body", "Clap", "Clap.playbackRate", duration, 0.1f);
		if (!modelTransform)
		{
			return;
		}
		ChildLocator component = modelTransform.GetComponent<ChildLocator>();
		if (!component)
		{
			return;
		}
		GameObject gameObject = LegacyResourcesAPI.Load<GameObject>("Prefabs/GolemClapCharge");
		Transform transform = component.FindChild("HandL");
		Transform transform2 = component.FindChild("HandR");
		if ((bool)transform)
		{
			if (!EffectManager.ShouldUsePooledEffect(gameObject))
			{
				leftHandChargeEffect = Object.Instantiate(gameObject, transform);
			}
			else
			{
				_emh_leftHandChargeEffect = EffectManager.GetAndActivatePooledEffect(gameObject, transform, inResetLocal: true);
				leftHandChargeEffect = _emh_leftHandChargeEffect.gameObject;
			}
		}
		if ((bool)transform2)
		{
			if (!EffectManager.ShouldUsePooledEffect(gameObject))
			{
				rightHandChargeEffect = Object.Instantiate(gameObject, transform2);
				return;
			}
			_emh_rightHandChargeEffect = EffectManager.GetAndActivatePooledEffect(gameObject, transform2, inResetLocal: true);
			rightHandChargeEffect = _emh_rightHandChargeEffect.gameObject;
		}
	}

	protected void DestroyChargeEffects()
	{
		if (_emh_leftHandChargeEffect != null && _emh_leftHandChargeEffect.OwningPool != null)
		{
			_emh_leftHandChargeEffect.OwningPool.ReturnObject(_emh_leftHandChargeEffect);
		}
		else
		{
			EntityState.Destroy(leftHandChargeEffect);
		}
		leftHandChargeEffect = null;
		_emh_leftHandChargeEffect = null;
		if (_emh_rightHandChargeEffect != null && _emh_rightHandChargeEffect.OwningPool != null)
		{
			_emh_rightHandChargeEffect.OwningPool.ReturnObject(_emh_rightHandChargeEffect);
		}
		else
		{
			EntityState.Destroy(rightHandChargeEffect);
		}
		rightHandChargeEffect = null;
		_emh_rightHandChargeEffect = null;
	}

	public override void OnExit()
	{
		DestroyChargeEffects();
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if ((bool)modelAnimator && modelAnimator.GetFloat("Clap.hitBoxActive") > 0.5f && !hasAttacked)
		{
			if (base.isAuthority && (bool)modelTransform)
			{
				ChildLocator component = modelTransform.GetComponent<ChildLocator>();
				if ((bool)component)
				{
					Transform transform = component.FindChild("ClapZone");
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
						attack.attackerFiltering = AttackerFiltering.NeverHitSelf;
						attack.Fire();
					}
				}
			}
			hasAttacked = true;
			DestroyChargeEffects();
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
