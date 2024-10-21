using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.Loader;

public class SwingZapFist : BaseSwingChargedFist
{
	public static float selfKnockback;

	private bool hasHit;

	protected override void OnMeleeHitAuthority()
	{
		if (!hasHit)
		{
			base.OnMeleeHitAuthority();
			hasHit = true;
			if ((bool)FindModelChild(swingEffectMuzzleString))
			{
				FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
				fireProjectileInfo.position = FindModelChild(swingEffectMuzzleString).position;
				fireProjectileInfo.rotation = Quaternion.LookRotation(punchVelocity);
				fireProjectileInfo.crit = base.isCritAuthority;
				fireProjectileInfo.damage = 1f * damageStat;
				fireProjectileInfo.owner = base.gameObject;
				fireProjectileInfo.projectilePrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/LoaderZapCone");
				ProjectileManager.instance.FireProjectile(fireProjectileInfo);
			}
		}
	}

	protected override void AuthorityExitHitPause()
	{
		base.AuthorityExitHitPause();
		outer.SetNextStateToMain();
	}

	public override void OnExit()
	{
		base.OnExit();
		if (base.isAuthority && hasHit && (bool)base.healthComponent)
		{
			Vector3 vector = punchVelocity;
			vector.y = Mathf.Min(vector.y, 0f);
			vector = vector.normalized;
			vector *= 0f - selfKnockback;
			if ((bool)base.characterMotor)
			{
				base.characterMotor.ApplyForce(vector, alwaysApply: true);
			}
		}
	}
}
