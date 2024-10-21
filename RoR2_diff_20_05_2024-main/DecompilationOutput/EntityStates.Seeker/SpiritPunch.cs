using System;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Seeker;

public class SpiritPunch : BaseState, SteppedSkillDef.IStepSetter
{
	public enum Gauntlet
	{
		Left,
		Right,
		Explode
	}

	[SerializeField]
	public GameObject projectilePrefab;

	[SerializeField]
	public GameObject projectileFinisherPrefab;

	[SerializeField]
	public GameObject muzzleflashEffectPrefab;

	[SerializeField]
	public float procCoefficient;

	[SerializeField]
	public float damageCoefficient;

	[SerializeField]
	public float dmgBuffIncrease = 0.2f;

	[SerializeField]
	public float comboDamageCoefficient;

	[SerializeField]
	public float force = 20f;

	public static float attackSpeedAltAnimationThreshold;

	[SerializeField]
	public float baseDuration;

	[SerializeField]
	public float paddingBetweenAttacks = 0.2f;

	[SerializeField]
	public string attackSoundString;

	[SerializeField]
	public string attackSoundStringAlt;

	[SerializeField]
	public float attackSoundPitch;

	[SerializeField]
	public float bloom;

	private float duration;

	private bool hasFiredGauntlet;

	private string muzzleString;

	private Transform muzzleTransform;

	private Animator animator;

	private ChildLocator childLocator;

	private Gauntlet gauntlet;

	private float extraDmgFromBuff;

	public static float recoilAmplitude;

	private string animationStateName;

	public static event Action<bool> onSpiritOrbFired;

	public void SetStep(int i)
	{
		gauntlet = (Gauntlet)i;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		base.characterBody.SetAimTimer(2f);
		animator = GetModelAnimator();
		if ((bool)animator)
		{
			childLocator = animator.GetComponent<ChildLocator>();
		}
		switch (gauntlet)
		{
		case Gauntlet.Left:
			muzzleString = "MuzzleLeft";
			animationStateName = "Cast1Left";
			break;
		case Gauntlet.Right:
			muzzleString = "MuzzleRight";
			animationStateName = "Cast1Right";
			break;
		case Gauntlet.Explode:
			muzzleString = "MuzzleEnergyBomb";
			animationStateName = "SpiritPunchFinisher";
			extraDmgFromBuff = dmgBuffIncrease * (float)base.characterBody.GetBuffCount(DLC2Content.Buffs.ChakraBuff);
			break;
		}
		bool @bool = animator.GetBool("isMoving");
		bool bool2 = animator.GetBool("isGrounded");
		if (!@bool && bool2)
		{
			PlayCrossfade("FullBody, Override", animationStateName, "FireGauntlet.playbackRate", duration, 0.025f);
			return;
		}
		PlayCrossfade("Gesture, Additive", animationStateName, "FireGauntlet.playbackRate", duration, 0.025f);
		PlayCrossfade("Gesture, Override", animationStateName, "FireGauntlet.playbackRate", duration, 0.025f);
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	private void FireGauntlet()
	{
		if (!hasFiredGauntlet)
		{
			base.characterBody.AddSpreadBloom(bloom);
			Ray ray = GetAimRay();
			TrajectoryAimAssist.ApplyTrajectoryAimAssist(ref ray, projectilePrefab, base.gameObject);
			if ((bool)childLocator)
			{
				muzzleTransform = childLocator.FindChild(muzzleString);
			}
			if ((bool)muzzleflashEffectPrefab)
			{
				EffectManager.SimpleMuzzleFlash(muzzleflashEffectPrefab, base.gameObject, muzzleString, transmit: false);
			}
			if (base.isAuthority)
			{
				float damage = damageStat * (damageCoefficient + extraDmgFromBuff);
				ProjectileManager.instance.FireProjectile(projectilePrefab, muzzleTransform.position, Util.QuaternionSafeLookRotation(ray.direction), base.gameObject, damage, force, Util.CheckRoll(critStat, base.characterBody.master), DamageColorIndex.Default, null, 70f + attackSpeedStat * 2f);
			}
			AddRecoil(-0.1f * recoilAmplitude, 0.1f * recoilAmplitude, -1f * recoilAmplitude, 1f * recoilAmplitude);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration - duration * 0.25f || hasFiredGauntlet)
		{
			if (gauntlet == Gauntlet.Explode && !hasFiredGauntlet)
			{
				Util.PlayAttackSpeedSound(attackSoundStringAlt, base.gameObject, attackSoundPitch);
				projectilePrefab = projectileFinisherPrefab;
				damageCoefficient = comboDamageCoefficient;
				FireGauntlet();
				SpiritPunch.onSpiritOrbFired?.Invoke(obj: true);
				_ = base.characterMotor.isGrounded;
				hasFiredGauntlet = true;
			}
			else if (!hasFiredGauntlet)
			{
				Util.PlayAttackSpeedSound(attackSoundString, base.gameObject, attackSoundPitch);
				FireGauntlet();
				hasFiredGauntlet = true;
				SpiritPunch.onSpiritOrbFired?.Invoke(obj: false);
			}
			if (base.isAuthority && base.fixedAge >= duration + duration * paddingBetweenAttacks)
			{
				outer.SetNextStateToMain();
			}
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}

	public override void OnSerialize(NetworkWriter writer)
	{
		base.OnSerialize(writer);
		writer.Write((byte)gauntlet);
	}

	public override void OnDeserialize(NetworkReader reader)
	{
		base.OnDeserialize(reader);
		gauntlet = (Gauntlet)reader.ReadByte();
	}
}
