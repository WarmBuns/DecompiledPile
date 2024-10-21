using RoR2;
using RoR2.CharacterAI;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.FalseSonBoss;

public class TaintedOffering : GenericProjectileBaseState
{
	[SerializeField]
	public float projectileYawBonus;

	[SerializeField]
	public float additionalShots = 8f;

	private BaseAI ai;

	public override void OnEnter()
	{
		base.OnEnter();
		if ((bool)base.characterBody)
		{
			if (base.characterBody.master.TryGetComponent<BaseAI>(out var component))
			{
				ai = component;
			}
			base.characterBody.SetAimTimer(1f);
		}
	}

	public override void FixedUpdate()
	{
		stopwatch += GetDeltaTime();
		if (stopwatch >= delayBeforeFiringProjectile && !firedProjectile)
		{
			firedProjectile = true;
			for (int num = 9; num > 0; num--)
			{
				switch (num)
				{
				case 9:
					projectileYawBonus = 0f;
					projectilePitchBonus = 0f;
					break;
				case 8:
					projectileYawBonus = -4f;
					projectilePitchBonus = -4f;
					break;
				case 7:
					projectileYawBonus = 4f;
					projectilePitchBonus = 4f;
					break;
				case 6:
					projectileYawBonus = 4f;
					projectilePitchBonus = -4f;
					break;
				case 5:
					projectileYawBonus = -4f;
					projectilePitchBonus = 4f;
					break;
				case 4:
					projectileYawBonus = 0f;
					projectilePitchBonus = 8f;
					break;
				case 3:
					projectileYawBonus = 0f;
					projectilePitchBonus = -8f;
					break;
				case 2:
					projectileYawBonus = 8f;
					projectilePitchBonus = 0f;
					break;
				case 1:
					projectileYawBonus = -8f;
					projectilePitchBonus = 0f;
					break;
				}
				FireProjectile();
			}
			DoFireEffects();
		}
		if (stopwatch >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	protected override void PlayAnimation(float duration)
	{
		base.PlayAnimation(duration);
		PlayAnimation("Gesture, Additive", "FireLunarSpike", "LunarSpike.playbackRate", duration);
		PlayAnimation("Gesture, Override", "HoldGauntletsUp", "LunarSpike.playbackRate", duration);
	}

	protected override Ray ModifyProjectileAimRay(Ray aimRay)
	{
		aimRay.origin = base.modelLocator.modelTransform.GetComponent<ChildLocator>().FindChild("MuzzleRight").position;
		if (ai != null && ai.currentEnemy != null && ai.currentEnemy.GetBullseyePosition(out var position))
		{
			aimRay.direction = position - aimRay.origin;
		}
		return aimRay;
	}

	protected override void FireProjectile()
	{
		if (base.isAuthority)
		{
			Ray aimRay = GetAimRay();
			aimRay = ModifyProjectileAimRay(aimRay);
			aimRay.direction = Util.ApplySpread(aimRay.direction, minSpread, maxSpread, 1f, 1f, projectileYawBonus, projectilePitchBonus);
			ProjectileManager.instance.FireProjectile(projectilePrefab, aimRay.origin, Util.QuaternionSafeLookRotation(aimRay.direction), base.gameObject, damageStat * damageCoefficient, force, Util.CheckRoll(critStat, base.characterBody.master));
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Frozen;
	}
}
