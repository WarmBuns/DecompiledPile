using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.Captain.Weapon;

public class CallAirstrikeBase : AimThrowableBase
{
	[SerializeField]
	public float airstrikeRadius;

	[SerializeField]
	public float bloom;

	public static GameObject muzzleFlashEffect;

	public static string muzzleString;

	public static string fireAirstrikeSoundString;

	public override void OnEnter()
	{
		base.OnEnter();
		base.characterBody.SetSpreadBloom(bloom);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		base.characterBody.SetAimTimer(4f);
	}

	public override void OnExit()
	{
		Util.PlaySound(fireAirstrikeSoundString, base.gameObject);
		base.OnExit();
	}

	protected override void ModifyProjectile(ref FireProjectileInfo fireProjectileInfo)
	{
		base.ModifyProjectile(ref fireProjectileInfo);
		fireProjectileInfo.position = currentTrajectoryInfo.hitPoint;
		fireProjectileInfo.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
		fireProjectileInfo.speedOverride = 0f;
	}

	protected override bool KeyIsDown()
	{
		return base.inputBank.skill1.down;
	}

	protected override EntityState PickNextState()
	{
		return new Idle();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}
