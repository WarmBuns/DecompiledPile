using RoR2;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.VoidRaidCrab.Weapon;

public class FireGravityBump : BaseGravityBumpState
{
	[SerializeField]
	public float baseDuration;

	[SerializeField]
	public string muzzleName;

	[SerializeField]
	public GameObject muzzleFlashPrefab;

	[SerializeField]
	public bool disableAirControlUntilCollision;

	[SerializeField]
	public GameObject airborneEffectPrefab;

	[SerializeField]
	public GameObject groundedEffectPrefab;

	[SerializeField]
	public string enterSoundString;

	[SerializeField]
	public bool isSoundScaledByAttackSpeed;

	[SerializeField]
	public string leftAnimationLayerName;

	[SerializeField]
	public string leftAnimationStateName;

	[SerializeField]
	public string leftAnimationPlaybackRateParam;

	[SerializeField]
	public string rightAnimationLayerName;

	[SerializeField]
	public string rightAnimationStateName;

	[SerializeField]
	public string rightAnimationPlaybackRateParam;

	[SerializeField]
	public SkillDef skillDefToReplaceAtStocksEmpty;

	[SerializeField]
	public SkillDef nextSkillDef;

	private float duration;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		if ((bool)nextSkillDef)
		{
			GenericSkill genericSkill = base.skillLocator.FindSkillByDef(skillDefToReplaceAtStocksEmpty);
			if ((bool)genericSkill && genericSkill.stock == 0)
			{
				genericSkill.SetBaseSkill(nextSkillDef);
			}
		}
		if (isLeft)
		{
			PlayAnimation(leftAnimationLayerName, leftAnimationStateName, leftAnimationPlaybackRateParam, duration);
		}
		else
		{
			PlayAnimation(rightAnimationLayerName, rightAnimationStateName, rightAnimationPlaybackRateParam, duration);
		}
		if ((bool)muzzleFlashPrefab)
		{
			EffectManager.SimpleMuzzleFlash(muzzleFlashPrefab, base.gameObject, muzzleName, transmit: false);
		}
		if (!NetworkServer.active)
		{
			return;
		}
		BullseyeSearch bullseyeSearch = new BullseyeSearch();
		bullseyeSearch.viewer = base.characterBody;
		bullseyeSearch.teamMaskFilter = TeamMask.GetEnemyTeams(base.characterBody.teamComponent.teamIndex);
		bullseyeSearch.minDistanceFilter = 0f;
		bullseyeSearch.maxDistanceFilter = maxDistance;
		bullseyeSearch.searchOrigin = base.inputBank.aimOrigin;
		bullseyeSearch.searchDirection = base.inputBank.aimDirection;
		bullseyeSearch.maxAngleFilter = 360f;
		bullseyeSearch.filterByLoS = false;
		bullseyeSearch.filterByDistinctEntity = true;
		bullseyeSearch.RefreshCandidates();
		foreach (HurtBox result in bullseyeSearch.GetResults())
		{
			GameObject gameObject = result.healthComponent.gameObject;
			if (!gameObject)
			{
				continue;
			}
			CharacterMotor component = gameObject.GetComponent<CharacterMotor>();
			if ((bool)component)
			{
				EffectData effectData = new EffectData
				{
					origin = gameObject.transform.position
				};
				GameObject effectPrefab;
				if (component.isGrounded)
				{
					component.ApplyForce(groundedForce, alwaysApply: true, disableAirControlUntilCollision);
					effectPrefab = groundedEffectPrefab;
					effectData.rotation = Util.QuaternionSafeLookRotation(groundedForce);
				}
				else
				{
					component.ApplyForce(airborneForce, alwaysApply: true, disableAirControlUntilCollision);
					effectPrefab = airborneEffectPrefab;
					effectData.rotation = Util.QuaternionSafeLookRotation(airborneForce);
				}
				EffectManager.SpawnEffect(effectPrefab, effectData, transmit: true);
			}
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && base.fixedAge >= duration)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}
