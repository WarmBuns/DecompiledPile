using System.Collections.Generic;
using RoR2;
using RoR2.Audio;
using UnityEngine;

namespace EntityStates;

public class BasicMeleeAttack : BaseState
{
	[SerializeField]
	public float baseDuration;

	[SerializeField]
	public float damageCoefficient;

	[SerializeField]
	public string hitBoxGroupName;

	[SerializeField]
	public GameObject hitEffectPrefab;

	[SerializeField]
	public float procCoefficient;

	[SerializeField]
	public float pushAwayForce;

	[SerializeField]
	public Vector3 forceVector;

	[SerializeField]
	public float hitPauseDuration;

	[SerializeField]
	public GameObject swingEffectPrefab;

	[SerializeField]
	public string swingEffectMuzzleString;

	[SerializeField]
	public string mecanimHitboxActiveParameter;

	[SerializeField]
	public float shorthopVelocityFromHit;

	[SerializeField]
	public string beginStateSoundString;

	[SerializeField]
	public string beginSwingSoundString;

	[SerializeField]
	public NetworkSoundEventDef impactSound;

	[SerializeField]
	public bool forceForwardVelocity;

	[SerializeField]
	public AnimationCurve forwardVelocityCurve;

	[SerializeField]
	public bool scaleHitPauseDurationAndVelocityWithAttackSpeed;

	[SerializeField]
	public bool ignoreAttackSpeed;

	protected float duration;

	protected HitBoxGroup hitBoxGroup;

	protected Animator animator;

	private OverlapAttack overlapAttack;

	protected bool authorityHitThisFixedUpdate;

	protected float hitPauseTimer;

	protected Vector3 storedHitPauseVelocity;

	private Run.FixedTimeStamp meleeAttackStartTime = Run.FixedTimeStamp.positiveInfinity;

	private GameObject swingEffectInstance;

	private int meleeAttackTicks;

	protected List<HurtBox> hitResults = new List<HurtBox>();

	private bool forceFire;

	protected EffectManagerHelper _emh_swingEffectInstance;

	protected bool authorityInHitPause => hitPauseTimer > 0f;

	private bool meleeAttackHasBegun => meleeAttackStartTime.hasPassed;

	protected bool authorityHasFiredAtAll => meleeAttackTicks > 0;

	protected bool isCritAuthority { get; private set; }

	protected virtual bool allowExitFire => true;

	public virtual string GetHitBoxGroupName()
	{
		return hitBoxGroupName;
	}

	public override void Reset()
	{
		base.Reset();
		duration = 0f;
		hitBoxGroup = null;
		animator = null;
		if (overlapAttack != null)
		{
			overlapAttack.Reset();
		}
		authorityHitThisFixedUpdate = false;
		hitPauseTimer = 0f;
		storedHitPauseVelocity = Vector3.zero;
		meleeAttackStartTime = Run.FixedTimeStamp.positiveInfinity;
		swingEffectInstance = null;
		meleeAttackTicks = 0;
		forceFire = false;
		_emh_swingEffectInstance = null;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		duration = CalcDuration();
		if (duration <= Time.fixedDeltaTime * 2f)
		{
			forceFire = true;
		}
		StartAimMode();
		Util.PlaySound(beginStateSoundString, base.gameObject);
		animator = GetModelAnimator();
		if (base.isAuthority)
		{
			isCritAuthority = RollCrit();
			hitBoxGroup = FindHitBoxGroup(GetHitBoxGroupName());
			if ((bool)hitBoxGroup)
			{
				overlapAttack = new OverlapAttack
				{
					attacker = base.gameObject,
					damage = damageCoefficient * damageStat,
					damageColorIndex = DamageColorIndex.Default,
					damageType = DamageType.Generic,
					forceVector = forceVector,
					hitBoxGroup = hitBoxGroup,
					hitEffectPrefab = hitEffectPrefab,
					impactSound = (impactSound?.index ?? NetworkSoundEventIndex.Invalid),
					inflictor = base.gameObject,
					isCrit = isCritAuthority,
					procChainMask = default(ProcChainMask),
					pushAwayForce = pushAwayForce,
					procCoefficient = procCoefficient,
					teamIndex = GetTeam()
				};
			}
		}
		PlayAnimation();
	}

	protected virtual float CalcDuration()
	{
		if (ignoreAttackSpeed)
		{
			return baseDuration;
		}
		return baseDuration / attackSpeedStat;
	}

	protected virtual void AuthorityModifyOverlapAttack(OverlapAttack overlapAttack)
	{
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (string.IsNullOrEmpty(mecanimHitboxActiveParameter))
		{
			BeginMeleeAttackEffect();
		}
		else if (animator.GetFloat(mecanimHitboxActiveParameter) > 0.5f)
		{
			BeginMeleeAttackEffect();
		}
		if (base.isAuthority)
		{
			AuthorityFixedUpdate();
		}
	}

	protected void AuthorityTriggerHitPause()
	{
		if ((bool)base.characterMotor)
		{
			storedHitPauseVelocity += base.characterMotor.velocity;
			base.characterMotor.velocity = Vector3.zero;
		}
		if ((bool)animator)
		{
			animator.speed = 0f;
		}
		if ((bool)swingEffectInstance)
		{
			ScaleParticleSystemDuration component = swingEffectInstance.GetComponent<ScaleParticleSystemDuration>();
			if ((bool)component)
			{
				component.newDuration = 20f;
			}
		}
		hitPauseTimer = (scaleHitPauseDurationAndVelocityWithAttackSpeed ? (hitPauseDuration / attackSpeedStat) : hitPauseDuration);
	}

	protected virtual void BeginMeleeAttackEffect()
	{
		if (meleeAttackStartTime != Run.FixedTimeStamp.positiveInfinity)
		{
			return;
		}
		meleeAttackStartTime = Run.FixedTimeStamp.now;
		Util.PlaySound(beginSwingSoundString, base.gameObject);
		if (!swingEffectPrefab)
		{
			return;
		}
		Transform transform = FindModelChild(swingEffectMuzzleString);
		if ((bool)transform)
		{
			if (!EffectManager.ShouldUsePooledEffect(swingEffectPrefab))
			{
				swingEffectInstance = Object.Instantiate(swingEffectPrefab, transform);
			}
			else
			{
				_emh_swingEffectInstance = EffectManager.GetAndActivatePooledEffect(swingEffectPrefab, transform, inResetLocal: true);
				swingEffectInstance = _emh_swingEffectInstance.gameObject;
			}
			ScaleParticleSystemDuration component = swingEffectInstance.GetComponent<ScaleParticleSystemDuration>();
			if ((bool)component)
			{
				component.newDuration = component.initialDuration;
			}
		}
	}

	protected virtual void AuthorityExitHitPause()
	{
		hitPauseTimer = 0f;
		storedHitPauseVelocity.y = Mathf.Max(storedHitPauseVelocity.y, scaleHitPauseDurationAndVelocityWithAttackSpeed ? (shorthopVelocityFromHit / Mathf.Sqrt(attackSpeedStat)) : shorthopVelocityFromHit);
		if ((bool)base.characterMotor)
		{
			base.characterMotor.velocity = storedHitPauseVelocity;
		}
		storedHitPauseVelocity = Vector3.zero;
		if ((bool)animator)
		{
			animator.speed = 1f;
		}
		if ((bool)swingEffectInstance)
		{
			ScaleParticleSystemDuration component = swingEffectInstance.GetComponent<ScaleParticleSystemDuration>();
			if ((bool)component)
			{
				component.newDuration = component.initialDuration;
			}
		}
	}

	protected virtual void PlayAnimation()
	{
	}

	protected virtual void OnMeleeHitAuthority()
	{
	}

	private void AuthorityFireAttack()
	{
		AuthorityModifyOverlapAttack(overlapAttack);
		hitResults.Clear();
		authorityHitThisFixedUpdate = overlapAttack.Fire(hitResults);
		meleeAttackTicks++;
		if (authorityHitThisFixedUpdate)
		{
			AuthorityTriggerHitPause();
			OnMeleeHitAuthority();
		}
	}

	protected virtual void AuthorityFixedUpdate()
	{
		if (authorityInHitPause)
		{
			hitPauseTimer -= GetDeltaTime();
			if ((bool)base.characterMotor)
			{
				base.characterMotor.velocity = Vector3.zero;
			}
			base.fixedAge -= GetDeltaTime();
			if (!authorityInHitPause)
			{
				AuthorityExitHitPause();
			}
		}
		else if (forceForwardVelocity && (bool)base.characterMotor && (bool)base.characterDirection)
		{
			Vector3 vector = base.characterDirection.forward * forwardVelocityCurve.Evaluate(base.fixedAge / duration);
			_ = base.characterMotor.velocity;
			base.characterMotor.AddDisplacement(new Vector3(vector.x, 0f, vector.z));
		}
		authorityHitThisFixedUpdate = false;
		if (overlapAttack != null && (string.IsNullOrEmpty(mecanimHitboxActiveParameter) || animator.GetFloat(mecanimHitboxActiveParameter) > 0.5f || forceFire))
		{
			AuthorityFireAttack();
		}
		if (duration <= base.fixedAge)
		{
			AuthorityOnFinish();
		}
	}

	public override void OnExit()
	{
		if (base.isAuthority)
		{
			if (!outer.destroying && !authorityHasFiredAtAll && allowExitFire)
			{
				BeginMeleeAttackEffect();
				AuthorityFireAttack();
			}
			if (authorityInHitPause)
			{
				AuthorityExitHitPause();
			}
		}
		if (_emh_swingEffectInstance != null && _emh_swingEffectInstance.OwningPool != null)
		{
			_emh_swingEffectInstance.OwningPool.ReturnObject(_emh_swingEffectInstance);
		}
		else if ((bool)swingEffectInstance)
		{
			EntityState.Destroy(swingEffectInstance);
		}
		swingEffectInstance = null;
		_emh_swingEffectInstance = null;
		if ((bool)animator)
		{
			animator.speed = 1f;
		}
		base.OnExit();
	}

	protected virtual void AuthorityOnFinish()
	{
		outer.SetNextStateToMain();
	}
}
