using System.Collections.Generic;
using RoR2.ConVar;
using RoR2.Projectile;
using RoR2.UI;
using UnityEngine;

namespace RoR2;

public static class TrajectoryAimAssist
{
	private struct AimAssistHit
	{
		public Vector3 point;

		public float distance;

		public Collider collider;

		public HurtBox hitHurtBox;

		public GameObject entityObject;
	}

	private static readonly LayerMask hitMask = (int)LayerIndex.world.mask | (int)LayerIndex.entityPrecise.mask;

	public static FloatConVar aimTrajectoryAssistMaxRadius = new FloatConVar("aim_trajectory_assist_max_radius", ConVarFlags.None, "3", "When mapping from the user's sensitivity setting to the actual spherecast radius, this will be the maximum value used.");

	public static FloatConVar aimTrajectoryAssistProjectileDistanceScale = new FloatConVar("aim_trajectory_assist_distance_scale", ConVarFlags.None, "0.5", "When using trajectory aim assist for a projectile, max distance will be set to speed scaled by this value");

	public static FloatConVar aimTrajectoryAssistMinProjectileSpeed = new FloatConVar("aim_trajectory_assist_min_projectile_speed", ConVarFlags.None, "60", "Trajectory aim assist will only be applied to projectiles which have speed greater than this value");

	public static FloatConVar aimTrajectoryAssistMaxChangeThreshold = new FloatConVar("aim_trajectory_assist_max_change_threshold", ConVarFlags.None, "0.8", "Controls maximum adjustment. If the dot product between original and adjusted aim vectors is less than this, the result will be discarded");

	private static void InitAimAssistHitFromRaycastHit(ref AimAssistHit aimAssistHit, Vector3 origin, ref RaycastHit raycastHit)
	{
		aimAssistHit.distance = raycastHit.distance;
		aimAssistHit.collider = raycastHit.collider;
		aimAssistHit.point = ((aimAssistHit.distance == 0f) ? origin : raycastHit.point);
		HurtBox hurtBox = (aimAssistHit.hitHurtBox = aimAssistHit.collider.GetComponent<HurtBox>());
		aimAssistHit.entityObject = (((bool)hurtBox && (bool)hurtBox.healthComponent) ? hurtBox.healthComponent.gameObject : aimAssistHit.collider.gameObject);
	}

	private static bool ShouldUseTrajectoryAimAssist(GameObject owner)
	{
		if ((bool)owner)
		{
			CharacterBody component = owner.GetComponent<CharacterBody>();
			if ((object)component != null && component.isPlayerControlled)
			{
				return GetAimAssistRadius(owner) > 0f;
			}
		}
		return false;
	}

	private static float GetAimAssistRadius(GameObject owner)
	{
		if ((bool)owner)
		{
			CharacterBody component = owner.GetComponent<CharacterBody>();
			if ((object)component != null && (bool)component.master)
			{
				PlayerCharacterMasterController component2 = component.master.GetComponent<PlayerCharacterMasterController>();
				if ((object)component2 != null)
				{
					NetworkUser networkUser = component2.networkUser;
					if ((object)networkUser != null)
					{
						LocalUser localUser = networkUser.localUser;
						if (localUser != null)
						{
							UserProfile userProfile = localUser.userProfile;
							if (userProfile != null)
							{
								return Util.Remap((localUser.eventSystem.currentInputSource == MPEventSystem.InputSource.Gamepad) ? userProfile.gamePadTrajectoryAimAssistSensitivity : userProfile.mouseTrajectoryAimAssistSensitivity, 0f, 10f, 0f, aimTrajectoryAssistMaxRadius.value);
							}
						}
					}
				}
			}
		}
		return 0f;
	}

	public static void ApplyTrajectoryAimAssist(ref Ray ray, ref FireProjectileInfo projectileInfo, float radiusMultiplier = 1f)
	{
		if (ShouldUseTrajectoryAimAssist(projectileInfo.owner) && (bool)projectileInfo.projectilePrefab)
		{
			float num = (projectileInfo.useSpeedOverride ? projectileInfo.speedOverride : (projectileInfo.projectilePrefab.GetComponent<ProjectileSimple>()?.desiredForwardSpeed ?? 0f));
			if (!(num < aimTrajectoryAssistMinProjectileSpeed.value))
			{
				float maxDistance = num * aimTrajectoryAssistProjectileDistanceScale.value;
				Vector3 forward = PerformAimAssistInternal(ray.direction, ray.origin, maxDistance, projectileInfo.owner, null, radiusMultiplier);
				projectileInfo.rotation = Util.QuaternionSafeLookRotation(forward);
			}
		}
	}

	public static void ApplyTrajectoryAimAssist(ref Ray ray, GameObject projectilePrefab, GameObject owner, float radiusMultiplier = 1f)
	{
		if (!ShouldUseTrajectoryAimAssist(owner) || !projectilePrefab)
		{
			return;
		}
		ProjectileSimple component = projectilePrefab.GetComponent<ProjectileSimple>();
		if ((object)component != null)
		{
			float desiredForwardSpeed = component.desiredForwardSpeed;
			if (!(desiredForwardSpeed < aimTrajectoryAssistMinProjectileSpeed.value))
			{
				float maxDistance = desiredForwardSpeed * aimTrajectoryAssistProjectileDistanceScale.value;
				ray.direction = PerformAimAssistInternal(ray.direction, ray.origin, maxDistance, owner, null, radiusMultiplier);
			}
		}
	}

	public static void ApplyTrajectoryAimAssist(ref Ray ray, float maxDistance, GameObject owner, float radiusMultiplier = 1f)
	{
		if (ShouldUseTrajectoryAimAssist(owner))
		{
			ray.direction = PerformAimAssistInternal(ray.direction, ray.origin, maxDistance, owner, null, radiusMultiplier);
		}
	}

	public static Vector3 ApplyTrajectoryAimAssist(Vector3 aimVector, Vector3 origin, float maxDistance, GameObject owner, GameObject weapon = null, float radiusMultiplier = 1f)
	{
		if (!ShouldUseTrajectoryAimAssist(owner))
		{
			return aimVector;
		}
		return PerformAimAssistInternal(aimVector, origin, maxDistance, owner, weapon, radiusMultiplier);
	}

	private static Vector3 PerformAimAssistInternal(Vector3 aimVector, Vector3 origin, float maxDistance, GameObject owner, GameObject weapon = null, float radiusMultiplier = 1f)
	{
		if (!owner || Mathf.Approximately(radiusMultiplier, 0f))
		{
			return aimVector;
		}
		float radius = GetAimAssistRadius(owner) * radiusMultiplier;
		List<AimAssistHit> list = new List<AimAssistHit>();
		RaycastHit[] array = Physics.SphereCastAll(origin, radius, aimVector, maxDistance, hitMask, QueryTriggerInteraction.Ignore);
		for (int i = 0; i < array.Length; i++)
		{
			AimAssistHit aimAssistHit = default(AimAssistHit);
			InitAimAssistHitFromRaycastHit(ref aimAssistHit, origin, ref array[i]);
			list.Add(aimAssistHit);
		}
		List<GameObject> list2 = new List<GameObject> { owner };
		if ((bool)weapon)
		{
			list2.Add(weapon);
		}
		Vector3 targetPosition = origin;
		if (!ProcessPotentialTargets(list, ref targetPosition, list2, owner))
		{
			return aimVector;
		}
		Vector3 normalized = (targetPosition - origin).normalized;
		if (!(Vector3.Dot(aimVector, normalized) > aimTrajectoryAssistMaxChangeThreshold.value))
		{
			return aimVector;
		}
		return normalized;
	}

	private static bool ProcessPotentialTargets(List<AimAssistHit> hits, ref Vector3 targetPosition, List<GameObject> ignoreList, GameObject owner)
	{
		TeamIndex attackerTeamIndex = TeamIndex.None;
		TeamComponent component = owner.GetComponent<TeamComponent>();
		if ((object)component != null)
		{
			attackerTeamIndex = component.teamIndex;
		}
		int count = hits.Count;
		int[] array = new int[count];
		for (int i = 0; i < count; i++)
		{
			array[i] = i;
		}
		for (int j = 0; j < count; j++)
		{
			float num = float.PositiveInfinity;
			int num2 = j;
			for (int k = j; k < count; k++)
			{
				int index = array[k];
				if (hits[index].distance < num)
				{
					num = hits[index].distance;
					num2 = k;
				}
			}
			GameObject entityObject = hits[array[num2]].entityObject;
			if (ignoreList.Contains(entityObject))
			{
				array[num2] = array[j];
				continue;
			}
			ignoreList.Add(entityObject);
			AimAssistHit aimAssistHit = hits[array[num2]];
			if (!aimAssistHit.collider || ((1 << aimAssistHit.collider.gameObject.layer) & (int)hitMask) == 0)
			{
				continue;
			}
			if ((bool)aimAssistHit.hitHurtBox)
			{
				HealthComponent healthComponent = aimAssistHit.hitHurtBox.healthComponent;
				if ((object)healthComponent != null && FriendlyFireManager.ShouldDirectHitProceed(healthComponent, attackerTeamIndex))
				{
					targetPosition = hits[array[num2]].point;
					return true;
				}
			}
			array[num2] = array[j];
		}
		return false;
	}
}
