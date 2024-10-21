using System;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Chef;

public class Dice : BaseState
{
	[SerializeField]
	public GameObject projectilePrefab;

	[SerializeField]
	public GameObject effectPrefab;

	[SerializeField]
	public GameObject projectileEnhancedPrefab;

	[SerializeField]
	public GameObject effectEnhancedPrefab;

	[SerializeField]
	public float baseDuration = 2f;

	[SerializeField]
	public float damageCoefficient = 1.2f;

	[SerializeField]
	public float boostedDamageCoefficient = 1.2f;

	[SerializeField]
	public float force = 20f;

	[SerializeField]
	public string attackString;

	[SerializeField]
	public string returnString;

	[SerializeField]
	public string yesChefAttackString;

	[SerializeField]
	public string yesChefReturnString;

	[SerializeField]
	public float recoilAmplitude;

	[SerializeField]
	public float bloom;

	[SerializeField]
	public float recallAnimationTransitionTime = 0.2f;

	[SerializeField]
	public float approximateCleaverDistance = 80f;

	private float duration;

	private ChefController chefController;

	private bool hasBoost;

	private bool recallInputPressed;

	private const float recallBackupCountdown_Duration = 3f;

	private float recallBackupCountdown = 3f;

	public override void OnEnter()
	{
		base.OnEnter();
		recallBackupCountdown = 3f;
		if (!chefController)
		{
			chefController = GetComponent<ChefController>();
		}
		chefController.characterBody = base.characterBody;
		chefController.spreadBloom = bloom;
		hasBoost = base.characterBody.HasBuff(DLC2Content.Buffs.Boosted);
		if (hasBoost)
		{
			damageCoefficient = boostedDamageCoefficient;
			if (NetworkServer.active)
			{
				base.characterBody.RemoveBuff(DLC2Content.Buffs.Boosted);
			}
		}
		chefController.NetworkcatchDirtied = false;
		chefController.recallCleaver = false;
		Ray ray = GetAimRay();
		TrajectoryAimAssist.ApplyTrajectoryAimAssist(ref ray, approximateCleaverDistance, base.gameObject);
		duration = baseDuration;
		StartAimMode(duration + 2f);
		if (hasBoost)
		{
			PlayAnimation("Gesture, Override", "FireSliceAndDice", "FireSliceAndDice.playbackRate", duration);
			PlayAnimation("Gesture, Additive", "FireSliceAndDice", "FireSliceAndDice.playbackRate", duration);
			Util.PlaySound(yesChefAttackString, base.gameObject);
		}
		else
		{
			PlayAnimation("Gesture, Override", "FireDice", "FireDice.playbackRate", duration);
			PlayAnimation("Gesture, Additive", "FireDice", "FireDice.playbackRate", duration);
			Util.PlaySound(attackString, base.gameObject);
		}
		AddRecoil(-1f * recoilAmplitude, -1.5f * recoilAmplitude, -0.25f * recoilAmplitude, 0.25f * recoilAmplitude);
		string muzzleName = "MouthMuzzle";
		GameObject gameObject = (hasBoost ? effectEnhancedPrefab : effectPrefab);
		if ((bool)gameObject)
		{
			EffectManager.SimpleMuzzleFlash(gameObject, base.characterBody.aimOriginTransform.gameObject, muzzleName, transmit: false);
		}
		chefController.cleaverAway = true;
		if (!base.isAuthority)
		{
			return;
		}
		GameObject gameObject2 = (hasBoost ? projectileEnhancedPrefab : projectilePrefab);
		int[] array = ((!hasBoost) ? new int[1] { 1 } : new int[3] { 8, 4, 4 });
		int num = array.Length;
		for (int i = 0; i < num; i++)
		{
			int num2 = array[i];
			float num3 = (float)(i % 2) * (0.5f / (float)num2);
			for (int j = 0; j < num2; j++)
			{
				FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
				fireProjectileInfo.projectilePrefab = gameObject2;
				fireProjectileInfo.position = ray.origin;
				float f = (num3 + (float)j / (float)num2) * MathF.PI * 2f;
				float f2 = Mathf.Acos(0.02f + (float)i / (float)num);
				float x = Mathf.Sin(f2) * Mathf.Sin(f);
				float y = Mathf.Cos(f2);
				float z = Mathf.Sin(f2) * Mathf.Cos(f);
				Quaternion quaternion = Quaternion.LookRotation(new Vector3(x, y, z));
				fireProjectileInfo.rotation = Util.QuaternionSafeLookRotation(ray.direction) * quaternion;
				fireProjectileInfo.owner = base.gameObject;
				fireProjectileInfo.damage = damageStat * damageCoefficient;
				fireProjectileInfo.damageTypeOverride = DamageType.Generic;
				fireProjectileInfo.force = force;
				fireProjectileInfo.crit = Util.CheckRoll(critStat, base.characterBody.master);
				if (!NetworkServer.active && (bool)chefController)
				{
					chefController.CacheCleaverProjectileFireInfo(fireProjectileInfo);
				}
				ProjectileManager.instance.FireProjectile(fireProjectileInfo);
			}
		}
	}

	public override void OnExit()
	{
		chefController.SetYesChefHeatState(newYesChefHeatState: false);
		if (NetworkServer.active)
		{
			base.characterBody.RemoveBuff(DLC2Content.Buffs.boostedFireEffect);
			if (!hasBoost)
			{
				chefController.DestroyCleavers();
			}
		}
		if (base.isAuthority)
		{
			chefController.ClearSkillOverrides();
		}
		if (chefController.cleaverAway)
		{
			chefController.recallCleaver = true;
		}
		if (chefController.catchDirtied)
		{
			chefController.NetworkcatchDirtied = false;
			PlayAnimation("Gesture, Override", "DiceReturnCatch", "DiceReturnCatch.playbackRate", duration);
			PlayAnimation("Gesture, Additive", "DiceReturnCatch", "DiceReturnCatch.playbackRate", duration);
		}
		else
		{
			PlayAnimation("Gesture, Override", "BufferEmpty", null, duration);
			PlayAnimation("Gesture, Additive", "BufferEmpty", null, duration);
		}
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		bool flag = base.fixedAge > 0f;
		base.FixedUpdate();
		bool flag2 = base.isAuthority;
		if (flag)
		{
			recallInputPressed = base.inputBank.skill1.justPressed;
		}
		if (recallInputPressed)
		{
			recallInputPressed = false;
			if (chefController.cleaverAway && flag)
			{
				if (chefController.recallCleaver)
				{
					chefController.recallCleaver = true;
				}
				else
				{
					chefController.recallCleaver = true;
					recallBackupCountdown = 3f;
					PlayAnimation("Gesture, Override", "DiceReturnHold", null, duration, recallAnimationTransitionTime);
					PlayAnimation("Gesture, Additive", "DiceReturnHold", null, duration, recallAnimationTransitionTime);
				}
			}
		}
		bool flag3 = false;
		if (flag2 && chefController.recallCleaver)
		{
			recallBackupCountdown -= Time.deltaTime;
			if (recallBackupCountdown <= 0f)
			{
				flag3 = true;
			}
		}
		if (chefController.catchDirtied)
		{
			if (hasBoost)
			{
				Util.PlaySound(yesChefReturnString, base.gameObject);
			}
			else
			{
				Util.PlaySound(returnString, base.gameObject);
			}
			if (flag2)
			{
				outer.SetNextStateToMain();
			}
		}
		else if (flag3 || (!chefController.cleaverAway && flag))
		{
			chefController.NetworkcatchDirtied = true;
			if (flag2)
			{
				outer.SetNextStateToMain();
			}
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
