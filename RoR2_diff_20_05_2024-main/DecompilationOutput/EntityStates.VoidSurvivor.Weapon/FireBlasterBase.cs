using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.VoidSurvivor.Weapon;

public class FireBlasterBase : BaseSkillState, SteppedSkillDef.IStepSetter
{
	[SerializeField]
	public GameObject projectilePrefab;

	[SerializeField]
	public GameObject muzzleflashEffectPrefab;

	[SerializeField]
	public float baseDuration = 2f;

	[SerializeField]
	public float damageCoefficient = 1.2f;

	[SerializeField]
	public float force = 20f;

	[SerializeField]
	public string attackSoundString;

	[SerializeField]
	public float attackSoundPitchPerStep;

	[SerializeField]
	public float recoilAmplitude;

	[SerializeField]
	public float bloom;

	[SerializeField]
	public string muzzle;

	[SerializeField]
	public float spread;

	[SerializeField]
	public int projectileCount;

	[SerializeField]
	public float yawPerProjectile;

	[SerializeField]
	public float offsetPerProjectile;

	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	[SerializeField]
	public string animationPlaybackRateParam;

	[SerializeField]
	public int burstCount;

	[SerializeField]
	public bool canCharge;

	private float duration;

	private float interruptDuration;

	public int step;

	void SteppedSkillDef.IStepSetter.SetStep(int i)
	{
		step = i;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		base.activatorSkillSlot = base.skillLocator.primary;
		GetAimRay();
		duration = baseDuration / attackSpeedStat;
		StartAimMode(duration + 2f);
		PlayAnimation(animationLayerName, animationStateName, animationPlaybackRateParam, duration);
		Util.PlayAttackSpeedSound(attackSoundString, base.gameObject, 1f + (float)step * attackSoundPitchPerStep);
		AddRecoil(-1f * recoilAmplitude, -1.5f * recoilAmplitude, -0.25f * recoilAmplitude, 0.25f * recoilAmplitude);
		base.characterBody.AddSpreadBloom(bloom);
		if ((bool)muzzleflashEffectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(muzzleflashEffectPrefab, base.gameObject, muzzle, transmit: false);
		}
		if (base.isAuthority)
		{
			FireProjectiles();
		}
	}

	private void FireProjectiles()
	{
		for (int i = 0; i < projectileCount; i++)
		{
			float num = (float)i - (float)(projectileCount - 1) / 2f;
			float bonusYaw = num * yawPerProjectile;
			float num2 = num * offsetPerProjectile;
			Ray aimRay = GetAimRay();
			aimRay.direction = Util.ApplySpread(aimRay.direction, 0f, base.characterBody.spreadBloomAngle + spread, 1f, 1f, bonusYaw);
			FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
			fireProjectileInfo.projectilePrefab = projectilePrefab;
			fireProjectileInfo.position = aimRay.origin + Vector3.Cross(aimRay.direction, Vector3.up) * num2;
			fireProjectileInfo.rotation = Util.QuaternionSafeLookRotation(aimRay.direction);
			fireProjectileInfo.owner = base.gameObject;
			fireProjectileInfo.damage = damageStat * damageCoefficient;
			fireProjectileInfo.force = force;
			fireProjectileInfo.crit = Util.CheckRoll(critStat, base.characterBody.master);
			ProjectileManager.instance.FireProjectile(fireProjectileInfo);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && base.fixedAge >= duration)
		{
			if (step >= burstCount && IsKeyDownAuthority() && canCharge)
			{
				outer.SetNextState(new ChargeMegaBlaster());
			}
			else
			{
				outer.SetNextState(new Idle());
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
		writer.Write((byte)step);
	}

	public override void OnDeserialize(NetworkReader reader)
	{
		base.OnDeserialize(reader);
		step = reader.ReadByte();
	}
}
