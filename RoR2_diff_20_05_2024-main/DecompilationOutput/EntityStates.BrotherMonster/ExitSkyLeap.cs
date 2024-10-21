using System;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using UnityEngine;

namespace EntityStates.BrotherMonster;

public class ExitSkyLeap : BaseState
{
	public static float baseDuration;

	public static float baseDurationUntilRecastInterrupt;

	public static string soundString;

	public static GameObject waveProjectilePrefab;

	public static int waveProjectileCount;

	public static float waveProjectileDamageCoefficient;

	public static float waveProjectileForce;

	public static float recastChance;

	public static int cloneCount;

	public static int cloneDuration;

	public static SkillDef replacementSkillDef;

	private float duration;

	private float durationUntilRecastInterrupt;

	private bool recast;

	private static int ExitSkyLeapStateHash = Animator.StringToHash("ExitSkyLeap");

	private static int BufferEmptyStateHash = Animator.StringToHash("BufferEmpty");

	private static int SkyLeapParamHash = Animator.StringToHash("SkyLeap.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		Util.PlaySound(soundString, base.gameObject);
		PlayAnimation("Body", ExitSkyLeapStateHash, SkyLeapParamHash, duration);
		PlayAnimation("FullBody Override", BufferEmptyStateHash);
		base.characterBody.AddTimedBuff(RoR2Content.Buffs.ArmorBoost, baseDuration);
		AimAnimator aimAnimator = GetAimAnimator();
		if ((bool)aimAnimator)
		{
			aimAnimator.enabled = true;
		}
		if (base.isAuthority)
		{
			FireRingAuthority();
		}
		if (!PhaseCounter.instance || PhaseCounter.instance.phase != 3)
		{
			return;
		}
		if (UnityEngine.Random.value < recastChance)
		{
			recast = true;
		}
		for (int i = 0; i < cloneCount; i++)
		{
			DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(LegacyResourcesAPI.Load<SpawnCard>("SpawnCards/CharacterSpawnCards/cscBrotherGlass"), new DirectorPlacementRule
			{
				placementMode = DirectorPlacementRule.PlacementMode.Approximate,
				minDistance = 3f,
				maxDistance = 20f,
				spawnOnTarget = base.gameObject.transform
			}, RoR2Application.rng);
			directorSpawnRequest.summonerBodyObject = base.gameObject;
			directorSpawnRequest.onSpawnedServer = (Action<SpawnCard.SpawnResult>)Delegate.Combine(directorSpawnRequest.onSpawnedServer, (Action<SpawnCard.SpawnResult>)delegate(SpawnCard.SpawnResult spawnResult)
			{
				spawnResult.spawnedInstance.GetComponent<Inventory>().GiveItem(RoR2Content.Items.HealthDecay, cloneDuration);
			});
			DirectorCore.instance.TrySpawnObject(directorSpawnRequest);
		}
		GenericSkill genericSkill = (base.skillLocator ? base.skillLocator.special : null);
		if ((bool)genericSkill)
		{
			genericSkill.SetSkillOverride(outer, UltChannelState.replacementSkillDef, GenericSkill.SkillOverridePriority.Contextual);
		}
	}

	private void FireRingAuthority()
	{
		float num = 360f / (float)waveProjectileCount;
		Vector3 vector = Vector3.ProjectOnPlane(base.inputBank.aimDirection, Vector3.up);
		Vector3 footPosition = base.characterBody.footPosition;
		for (int i = 0; i < waveProjectileCount; i++)
		{
			Vector3 forward = Quaternion.AngleAxis(num * (float)i, Vector3.up) * vector;
			if (base.isAuthority)
			{
				ProjectileManager.instance.FireProjectile(waveProjectilePrefab, footPosition, Util.QuaternionSafeLookRotation(forward), base.gameObject, base.characterBody.damage * waveProjectileDamageCoefficient, waveProjectileForce, Util.CheckRoll(base.characterBody.crit, base.characterBody.master));
			}
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority)
		{
			if (recast && base.fixedAge > baseDurationUntilRecastInterrupt)
			{
				outer.SetNextState(new EnterSkyLeap());
			}
			if (base.fixedAge > duration)
			{
				outer.SetNextStateToMain();
			}
		}
	}
}
