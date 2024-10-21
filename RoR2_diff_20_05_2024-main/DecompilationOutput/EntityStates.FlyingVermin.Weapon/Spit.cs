using RoR2;
using RoR2.CharacterAI;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.FlyingVermin.Weapon;

public class Spit : GenericProjectileBaseState
{
	private static int SpitStateHash = Animator.StringToHash("Spit");

	private static int SpitParamHash = Animator.StringToHash("Spit.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		if ((bool)base.characterBody)
		{
			base.characterBody.SetAimTimer(5f);
		}
	}

	protected override void PlayAnimation(float duration)
	{
		base.PlayAnimation(duration);
		PlayAnimation("Gesture, Additive", SpitStateHash, SpitParamHash, duration);
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}

	protected override void FireProjectile()
	{
		if (base.isAuthority)
		{
			Ray aimRay = GetAimRay();
			aimRay = ModifyProjectileAimRay(aimRay);
			aimRay.direction = Util.ApplySpread(aimRay.direction, minSpread, maxSpread, 1f, 1f, 0f, projectilePitchBonus);
			float damage = damageStat * damageCoefficient;
			BaseAI component = outer.commonComponents.characterBody.master.GetComponent<BaseAI>();
			if ((object)component != null && component.shouldMissFirstOffScreenShot && component.currentEnemy.characterBody.teamComponent.teamIndex == TeamIndex.Player)
			{
				component.shouldMissFirstOffScreenShot = false;
				aimRay.direction = OffScreenMissHelper.ApplyRandomTrajectoryOffset(aimRay.direction);
				damage = 0f;
			}
			ProjectileManager.instance.FireProjectile(projectilePrefab, aimRay.origin, Util.QuaternionSafeLookRotation(aimRay.direction), base.gameObject, damage, force, Util.CheckRoll(critStat, base.characterBody.master));
		}
	}
}
