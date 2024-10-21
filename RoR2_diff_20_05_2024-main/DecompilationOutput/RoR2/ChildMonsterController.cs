using EntityStates;
using EntityStates.ChildMonster;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;

namespace RoR2;

[RequireComponent(typeof(CharacterBody))]
[RequireComponent(typeof(EntityStateMachine))]
public class ChildMonsterController : MonoBehaviour
{
	public CharacterBody characterBody;

	public EntityStateMachine stateMachine;

	public GameObject tpEffectPrefab;

	[FormerlySerializedAs("frolicThreshold")]
	[Tooltip("If Child receives this much cumulative damage, they will start trying to teleport.")]
	public float cumulativeDamageThresholdBeforeFrolic = 10f;

	[Tooltip("Does not apply to 'falling off cliffs!' teleporting.")]
	public float teleportCooldown = 20f;

	[Tooltip("Which skill slot is 'Frolic' (teleport) categorized as.")]
	public SkillSlot frolicSkillSlot;

	private float inAirTimer;

	public float amountToFallBeforeTp = 1.5f;

	private float cachedYPosition;

	private SkillLocator skillLocator;

	private float lastTeleportTimestamp = -1f;

	private float healthAtLastTeleport;

	private BaseState requestedTeleportState;

	private bool hasAuthority;

	private void Start()
	{
		skillLocator = characterBody.skillLocator;
		cachedYPosition = characterBody.corePosition.y;
		hasAuthority = Util.HasEffectiveAuthority(characterBody.networkIdentity);
		RegisterTeleport(addInvincibility: false);
		lastTeleportTimestamp = -1f;
	}

	private void Update()
	{
		if (characterBody == null || !characterBody.healthComponent.alive)
		{
			return;
		}
		TryRefreshTeleportAbility();
		if (hasAuthority)
		{
			HandleFalling();
			HandleCumulativeDamage();
			if (requestedTeleportState != null)
			{
				SetNextStateToTeleport();
			}
		}
	}

	private void SetNextStateToTeleport()
	{
		if (stateMachine.CanInterruptState(requestedTeleportState.GetMinimumInterruptPriority()))
		{
			stateMachine.SetNextState(requestedTeleportState);
			requestedTeleportState = null;
		}
	}

	private void HandleFalling()
	{
		if (!characterBody.characterMotor.isGrounded && characterBody.corePosition.y < cachedYPosition)
		{
			inAirTimer += Time.deltaTime;
		}
		else
		{
			inAirTimer = 0f;
		}
		cachedYPosition = characterBody.corePosition.y;
		if (inAirTimer > amountToFallBeforeTp)
		{
			inAirTimer = 0f;
			requestedTeleportState = new FrolicAway();
		}
	}

	private void TryRefreshTeleportAbility()
	{
		if (lastTeleportTimestamp != -1f && CheckTeleportAvailable())
		{
			lastTeleportTimestamp = -1f;
		}
	}

	private void HandleCumulativeDamage()
	{
		if (requestedTeleportState == null && CheckTeleportAvailable() && healthAtLastTeleport - characterBody.healthComponent.health >= cumulativeDamageThresholdBeforeFrolic)
		{
			requestedTeleportState = new Frolic();
		}
	}

	public bool CheckTeleportAvailable()
	{
		if (lastTeleportTimestamp != -1f)
		{
			return Time.time - lastTeleportTimestamp >= teleportCooldown;
		}
		return true;
	}

	public void RegisterTeleport(bool addInvincibility = true)
	{
		lastTeleportTimestamp = Time.time;
		healthAtLastTeleport = characterBody.healthComponent.health;
		requestedTeleportState = null;
		skillLocator.GetSkill(frolicSkillSlot)?.DeductStock(100);
		if (addInvincibility && NetworkServer.active && !characterBody.HasBuff(RoR2Content.Buffs.HiddenInvincibility))
		{
			characterBody.AddTimedBuff(RoR2Content.Buffs.HiddenInvincibility, 1.5f);
		}
	}
}
