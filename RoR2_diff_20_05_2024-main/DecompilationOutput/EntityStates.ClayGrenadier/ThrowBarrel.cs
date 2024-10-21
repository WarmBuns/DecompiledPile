using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.ClayGrenadier;

public class ThrowBarrel : GenericProjectileBaseState
{
	[SerializeField]
	public float aimCalculationRaycastDistance;

	[SerializeField]
	public string animationLayerName = "Body";

	[SerializeField]
	public string animationStateName = "FaceSlam";

	[SerializeField]
	public string playbackRateParam = "FaceSlam.playbackRate";

	[SerializeField]
	public int projectileCount;

	[SerializeField]
	public float projectilePitchBonusPerProjectile;

	[SerializeField]
	public float projectileYawBonusPerProjectile;

	[SerializeField]
	public GameObject chargeEffectPrefab;

	[SerializeField]
	public string chargeEffectMuzzleString;

	[SerializeField]
	public string enterSoundString;

	private GameObject chargeInstance;

	private int currentProjectileCount;

	protected override void PlayAnimation(float duration)
	{
		base.PlayAnimation(duration);
		PlayCrossfade(animationLayerName, animationStateName, playbackRateParam, duration, 0.2f);
	}

	public override void OnEnter()
	{
		base.OnEnter();
		Util.PlayAttackSpeedSound(enterSoundString, base.gameObject, attackSpeedStat);
		if ((bool)base.characterDirection)
		{
			base.characterDirection.moveVector = GetAimRay().direction;
		}
		base.characterBody.SetAimTimer(0f);
		Transform transform = FindModelChild(chargeEffectMuzzleString);
		if ((bool)transform && (bool)chargeEffectPrefab)
		{
			chargeInstance = Object.Instantiate(chargeEffectPrefab, transform.position, transform.rotation);
			chargeInstance.transform.parent = transform;
			ScaleParticleSystemDuration component = chargeInstance.GetComponent<ScaleParticleSystemDuration>();
			if ((bool)component)
			{
				component.newDuration = delayBeforeFiringProjectile;
			}
		}
	}

	protected override Ray ModifyProjectileAimRay(Ray aimRay)
	{
		RaycastHit hitInfo = default(RaycastHit);
		Ray ray = aimRay;
		float desiredForwardSpeed = projectilePrefab.GetComponent<ProjectileSimple>().desiredForwardSpeed;
		ray.origin = aimRay.origin;
		if (Util.CharacterRaycast(base.gameObject, ray, out hitInfo, float.PositiveInfinity, (int)LayerIndex.world.mask | (int)LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Ignore))
		{
			float num = desiredForwardSpeed;
			Vector3 vector = hitInfo.point - aimRay.origin;
			Vector2 vector2 = new Vector2(vector.x, vector.z);
			float magnitude = vector2.magnitude;
			float y = Trajectory.CalculateInitialYSpeed(magnitude / num, vector.y);
			Vector3 vector3 = new Vector3(vector2.x / magnitude * num, y, vector2.y / magnitude * num);
			desiredForwardSpeed = vector3.magnitude;
			aimRay.direction = vector3 / desiredForwardSpeed;
		}
		aimRay.direction = Util.ApplySpread(aimRay.direction, 0f, 0f, 1f, 1f, projectileYawBonusPerProjectile * (float)currentProjectileCount, projectilePitchBonusPerProjectile * (float)currentProjectileCount);
		return aimRay;
	}

	protected override void FireProjectile()
	{
		for (int i = 0; i < projectileCount; i++)
		{
			base.FireProjectile();
			currentProjectileCount++;
		}
		if ((bool)chargeInstance)
		{
			EntityState.Destroy(chargeInstance);
		}
	}

	public override void OnExit()
	{
		if ((bool)chargeInstance)
		{
			EntityState.Destroy(chargeInstance);
		}
		base.OnExit();
	}
}
