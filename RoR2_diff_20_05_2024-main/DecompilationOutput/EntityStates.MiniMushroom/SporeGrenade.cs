using System.Linq;
using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.MiniMushroom;

public class SporeGrenade : BaseState
{
	public static GameObject chargeEffectPrefab;

	public static string attackSoundString;

	public static string chargeUpSoundString;

	public static float recoilAmplitude = 1f;

	public static GameObject projectilePrefab;

	public static float baseDuration = 2f;

	public static string muzzleString;

	public static float damageCoefficient;

	public static float timeToTarget = 3f;

	public static float projectileVelocity = 55f;

	public static float minimumDistance;

	public static float maximumDistance;

	public static float baseChargeTime = 2f;

	private uint chargeupSoundID;

	private Ray projectileRay;

	private Transform modelTransform;

	private float duration;

	private float chargeTime;

	private bool hasFired;

	private Animator modelAnimator;

	private GameObject chargeEffectInstance;

	private static int ChargeStateHash = Animator.StringToHash("Charge");

	private static int isChargedParamHash = Animator.StringToHash("isCharged");

	private static int EmptyStateHash = Animator.StringToHash("Empty");

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		chargeTime = baseChargeTime / attackSpeedStat;
		modelAnimator = GetModelAnimator();
		if ((bool)modelAnimator)
		{
			modelAnimator.SetBool(isChargedParamHash, value: false);
			PlayAnimation("Gesture, Additive", ChargeStateHash);
			chargeupSoundID = Util.PlaySound(chargeUpSoundString, base.characterBody.modelLocator.modelTransform.gameObject);
		}
		Transform transform = FindModelChild("ChargeSpot");
		if ((bool)transform)
		{
			chargeEffectInstance = Object.Instantiate(chargeEffectPrefab, transform);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (!(base.fixedAge >= chargeTime))
		{
			return;
		}
		if (!hasFired)
		{
			hasFired = true;
			modelAnimator?.SetBool(isChargedParamHash, value: true);
			if (base.isAuthority)
			{
				FireGrenade(muzzleString);
			}
		}
		if (base.isAuthority && base.fixedAge >= duration)
		{
			outer.SetNextStateToMain();
		}
	}

	public override void OnExit()
	{
		PlayAnimation("Gesture, Additive", EmptyStateHash);
		AkSoundEngine.StopPlayingID(chargeupSoundID);
		if ((bool)chargeEffectInstance)
		{
			EntityState.Destroy(chargeEffectInstance);
		}
		base.OnExit();
	}

	private void FireGrenade(string targetMuzzle)
	{
		Ray aimRay = GetAimRay();
		Ray ray = new Ray(aimRay.origin, Vector3.up);
		Transform transform = FindModelChild(targetMuzzle);
		if ((bool)transform)
		{
			ray.origin = transform.position;
		}
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
			if (magnitude2 > maximumDistance)
			{
				magnitude2 = maximumDistance;
			}
			float y = Trajectory.CalculateInitialYSpeed(timeToTarget, vector2.y);
			float num = magnitude2 / timeToTarget;
			Vector3 direction = new Vector3(vector4.x * num, y, vector4.y * num);
			magnitude = direction.magnitude;
			ray.direction = direction;
		}
		Quaternion rotation = Util.QuaternionSafeLookRotation(ray.direction + Random.insideUnitSphere * 0.05f);
		ProjectileManager.instance.FireProjectile(projectilePrefab, ray.origin, rotation, base.gameObject, damageStat * damageCoefficient, 0f, Util.CheckRoll(critStat, base.characterBody.master), DamageColorIndex.Default, null, magnitude);
	}
}
