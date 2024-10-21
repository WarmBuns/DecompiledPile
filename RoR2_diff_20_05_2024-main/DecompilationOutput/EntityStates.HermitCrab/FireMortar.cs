using System.Linq;
using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.HermitCrab;

public class FireMortar : BaseState
{
	public static GameObject mortarProjectilePrefab;

	public static GameObject mortarMuzzleflashEffect;

	public static int mortarCount;

	public static string mortarMuzzleName;

	public static string mortarSoundString;

	public static float mortarDamageCoefficient;

	public static float baseDuration;

	public static float timeToTarget = 3f;

	public static float projectileVelocity = 55f;

	public static float minimumDistance;

	private float stopwatch;

	private float duration;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		PlayCrossfade("Gesture, Additive", "FireMortar", 0f);
		Util.PlaySound(mortarSoundString, base.gameObject);
		EffectManager.SimpleMuzzleFlash(mortarMuzzleflashEffect, base.gameObject, mortarMuzzleName, transmit: false);
		if (base.isAuthority)
		{
			Fire();
		}
	}

	private void Fire()
	{
		Ray aimRay = GetAimRay();
		Ray ray = new Ray(aimRay.origin, Vector3.up);
		BullseyeSearch bullseyeSearch = new BullseyeSearch();
		bullseyeSearch.searchOrigin = aimRay.origin;
		bullseyeSearch.searchDirection = aimRay.direction;
		bullseyeSearch.filterByLoS = false;
		bullseyeSearch.teamMaskFilter = TeamMask.allButNeutral;
		if ((bool)base.teamComponent)
		{
			bullseyeSearch.teamMaskFilter.RemoveTeam(base.teamComponent.teamIndex);
		}
		bullseyeSearch.sortMode = BullseyeSearch.SortMode.Angle;
		bullseyeSearch.RefreshCandidates();
		HurtBox hurtBox = bullseyeSearch.GetResults().FirstOrDefault();
		bool flag = false;
		Vector3 vector = Vector3.zero;
		RaycastHit hitInfo;
		if ((bool)hurtBox)
		{
			vector = hurtBox.transform.position;
			flag = true;
		}
		else if (Physics.Raycast(aimRay, out hitInfo, 1000f, (int)LayerIndex.world.mask | (int)LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Ignore))
		{
			vector = hitInfo.point;
			flag = true;
		}
		float magnitude = projectileVelocity;
		if (flag)
		{
			Vector3 vector2 = vector - ray.origin;
			Vector2 vector3 = new Vector2(vector2.x, vector2.z);
			float magnitude2 = vector3.magnitude;
			Vector2 vector4 = vector3 / magnitude2;
			if (magnitude2 < minimumDistance)
			{
				magnitude2 = minimumDistance;
			}
			float y = Trajectory.CalculateInitialYSpeed(timeToTarget, vector2.y);
			float num = magnitude2 / timeToTarget;
			Vector3 direction = new Vector3(vector4.x * num, y, vector4.y * num);
			magnitude = direction.magnitude;
			ray.direction = direction;
		}
		for (int i = 0; i < mortarCount; i++)
		{
			Quaternion rotation = Util.QuaternionSafeLookRotation(ray.direction + ((i != 0) ? (Random.insideUnitSphere * 0.05f) : Vector3.zero));
			ProjectileManager.instance.FireProjectile(mortarProjectilePrefab, ray.origin, rotation, base.gameObject, damageStat * mortarDamageCoefficient, 0f, Util.CheckRoll(critStat, base.characterBody.master), DamageColorIndex.Default, null, magnitude);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		stopwatch += GetDeltaTime();
		if (stopwatch > duration)
		{
			Burrowed burrowed = new Burrowed();
			burrowed.duration = Burrowed.mortarCooldown;
			outer.SetNextState(burrowed);
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
