using System;
using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace EntityStates;

public class BaseState : EntityState
{
	protected struct HitStopCachedState
	{
		public Vector3 characterVelocity;

		public string playbackName;

		public float playbackRate;
	}

	protected float attackSpeedStat = 1f;

	protected float damageStat;

	protected float critStat;

	protected float moveSpeedStat;

	private const float defaultAimDuration = 2f;

	protected bool isGrounded
	{
		get
		{
			if ((bool)base.characterMotor)
			{
				return base.characterMotor.isGrounded;
			}
			return false;
		}
	}

	public override void Reset()
	{
		base.Reset();
		attackSpeedStat = 1f;
		damageStat = 0f;
		critStat = 0f;
		moveSpeedStat = 0f;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		if ((bool)base.characterBody)
		{
			attackSpeedStat = base.characterBody.attackSpeed;
			damageStat = base.characterBody.damage;
			critStat = base.characterBody.crit;
			moveSpeedStat = base.characterBody.moveSpeed;
		}
	}

	protected Ray GetAimRay()
	{
		if ((bool)base.inputBank)
		{
			return new Ray(base.inputBank.aimOrigin, base.inputBank.aimDirection);
		}
		return new Ray(base.transform.position, base.transform.forward);
	}

	protected void AddRecoil(float verticalMin, float verticalMax, float horizontalMin, float horizontalMax)
	{
		base.cameraTargetParams.AddRecoil(verticalMin, verticalMax, horizontalMin, horizontalMax);
	}

	public OverlapAttack InitMeleeOverlap(float damageCoefficient, GameObject hitEffectPrefab, Transform modelTransform, string hitboxGroupName)
	{
		OverlapAttack overlapAttack = new OverlapAttack();
		overlapAttack.attacker = base.gameObject;
		overlapAttack.inflictor = base.gameObject;
		overlapAttack.teamIndex = TeamComponent.GetObjectTeam(overlapAttack.attacker);
		overlapAttack.damage = damageCoefficient * damageStat;
		overlapAttack.hitEffectPrefab = hitEffectPrefab;
		overlapAttack.isCrit = RollCrit();
		if ((bool)modelTransform)
		{
			overlapAttack.hitBoxGroup = Array.Find(modelTransform.GetComponents<HitBoxGroup>(), (HitBoxGroup element) => element.groupName == hitboxGroupName);
		}
		return overlapAttack;
	}

	public bool FireMeleeOverlap(OverlapAttack attack, Animator animator, string mecanimHitboxActiveParameter, float forceMagnitude, bool calculateForceVector = true)
	{
		bool result = false;
		if ((bool)animator && animator.GetFloat(mecanimHitboxActiveParameter) > 0.1f)
		{
			if (calculateForceVector)
			{
				attack.forceVector = base.transform.forward * forceMagnitude;
			}
			result = attack.Fire();
		}
		return result;
	}

	public bool FireMeleeOverlap(OverlapAttack attack, Animator animator, int mecanimHitboxActiveParameterHash, float forceMagnitude, bool calculateForceVector = true)
	{
		bool result = false;
		if ((bool)animator && animator.GetFloat(mecanimHitboxActiveParameterHash) > 0.1f)
		{
			if (calculateForceVector)
			{
				attack.forceVector = base.transform.forward * forceMagnitude;
			}
			result = attack.Fire();
		}
		return result;
	}

	public void SmallHop(CharacterMotor characterMotor, float smallHopVelocity)
	{
		if ((bool)characterMotor)
		{
			characterMotor.Motor.ForceUnground();
			characterMotor.velocity = new Vector3(characterMotor.velocity.x, Mathf.Max(characterMotor.velocity.y, smallHopVelocity), characterMotor.velocity.z);
		}
	}

	protected HitStopCachedState CreateHitStopCachedState(CharacterMotor characterMotor, Animator animator, string playbackRateAnimationParameter)
	{
		HitStopCachedState result = default(HitStopCachedState);
		result.characterVelocity = new Vector3(characterMotor.velocity.x, Mathf.Max(0f, characterMotor.velocity.y), characterMotor.velocity.z);
		result.playbackName = playbackRateAnimationParameter;
		result.playbackRate = animator.GetFloat(result.playbackName);
		return result;
	}

	protected void ConsumeHitStopCachedState(HitStopCachedState hitStopCachedState, CharacterMotor characterMotor, Animator animator)
	{
		characterMotor.velocity = hitStopCachedState.characterVelocity;
		animator.SetFloat(hitStopCachedState.playbackName, hitStopCachedState.playbackRate);
	}

	protected void StartAimMode(float duration = 2f, bool snap = false)
	{
		StartAimMode(GetAimRay(), duration, snap);
	}

	protected void StartAimMode(Ray aimRay, float duration = 2f, bool snap = false)
	{
		if ((bool)base.characterDirection && aimRay.direction != Vector3.zero)
		{
			if (snap)
			{
				base.characterDirection.forward = aimRay.direction;
			}
			else
			{
				base.characterDirection.moveVector = aimRay.direction;
			}
		}
		if ((bool)base.characterBody)
		{
			base.characterBody.SetAimTimer(duration);
		}
		if (!base.modelLocator)
		{
			return;
		}
		Transform modelTransform = base.modelLocator.modelTransform;
		if ((bool)modelTransform)
		{
			AimAnimator component = modelTransform.GetComponent<AimAnimator>();
			if ((bool)component && snap)
			{
				component.AimImmediate();
			}
		}
	}

	protected bool RollCrit()
	{
		if ((bool)base.characterBody && (bool)base.characterBody.master)
		{
			return Util.CheckRoll(critStat, base.characterBody.master);
		}
		return false;
	}

	protected Transform FindModelChild(string childName)
	{
		ChildLocator modelChildLocator = GetModelChildLocator();
		if ((bool)modelChildLocator)
		{
			return modelChildLocator.FindChild(childName);
		}
		return null;
	}

	protected T FindModelChildComponent<T>(string childName)
	{
		ChildLocator modelChildLocator = GetModelChildLocator();
		if ((bool)modelChildLocator)
		{
			return modelChildLocator.FindChildComponent<T>(childName);
		}
		return default(T);
	}

	protected GameObject FindModelChildGameObject(string childName)
	{
		ChildLocator modelChildLocator = GetModelChildLocator();
		if ((bool)modelChildLocator)
		{
			return modelChildLocator.FindChildGameObject(childName);
		}
		return null;
	}

	public TeamIndex GetTeam()
	{
		return TeamComponent.GetObjectTeam(base.gameObject);
	}

	public bool HasBuff(BuffIndex buffType)
	{
		if ((bool)base.characterBody)
		{
			return base.characterBody.HasBuff(buffType);
		}
		return false;
	}

	public bool HasBuff(BuffDef buffType)
	{
		if ((bool)base.characterBody)
		{
			return base.characterBody.HasBuff(buffType);
		}
		return false;
	}

	public int GetBuffCount(BuffIndex buffType)
	{
		if (!base.characterBody)
		{
			return 0;
		}
		return base.characterBody.GetBuffCount(buffType);
	}

	public int GetBuffCount(BuffDef buffType)
	{
		if (!base.characterBody)
		{
			return 0;
		}
		return base.characterBody.GetBuffCount(buffType);
	}

	protected void AttemptToStartSprint()
	{
		if ((bool)base.inputBank)
		{
			base.inputBank.sprint.down = true;
		}
	}

	protected HitBoxGroup FindHitBoxGroup(string groupName)
	{
		Transform modelTransform = GetModelTransform();
		if (!modelTransform)
		{
			return null;
		}
		HitBoxGroup result = null;
		List<HitBoxGroup> gameObjectComponents = GetComponentsCache<HitBoxGroup>.GetGameObjectComponents(modelTransform.gameObject);
		int i = 0;
		for (int count = gameObjectComponents.Count; i < count; i++)
		{
			if (gameObjectComponents[i].groupName == groupName)
			{
				result = gameObjectComponents[i];
				break;
			}
		}
		GetComponentsCache<HitBoxGroup>.ReturnBuffer(gameObjectComponents);
		return result;
	}
}
