using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.VoidMegaCrab;

public class DeathState : GenericCharacterDeath
{
	public static GameObject deathBombProjectile;

	public static float duration;

	public static string muzzleName;

	private Transform muzzleTransform;

	private static int EmptyStateHash = Animator.StringToHash("Empty");

	private static int DeathStateHash = Animator.StringToHash("Death");

	private static int DeathParamHash = Animator.StringToHash("Death.playbackRate");

	protected override bool shouldAutoDestroy => false;

	protected override void PlayDeathAnimation(float crossfadeDuration = 0.1f)
	{
		PlayCrossfade("Body", DeathStateHash, DeathParamHash, duration, 0.1f);
		PlayAnimation("Left Gun Override (Arm)", EmptyStateHash);
		PlayAnimation("Right Gun Override (Arm)", EmptyStateHash);
	}

	public override void OnEnter()
	{
		base.OnEnter();
		muzzleTransform = FindModelChild(muzzleName);
		if ((bool)muzzleTransform && base.isAuthority)
		{
			FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
			fireProjectileInfo.projectilePrefab = deathBombProjectile;
			fireProjectileInfo.position = muzzleTransform.position;
			fireProjectileInfo.rotation = Util.QuaternionSafeLookRotation(Vector3.up);
			fireProjectileInfo.owner = base.gameObject;
			fireProjectileInfo.damage = damageStat;
			fireProjectileInfo.crit = base.characterBody.RollCrit();
			ProjectileManager.instance.FireProjectile(fireProjectileInfo);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration)
		{
			DestroyModel();
			if (NetworkServer.active)
			{
				DestroyBodyAsapServer();
			}
		}
	}

	public override void OnExit()
	{
		DestroyModel();
		base.OnExit();
	}
}
