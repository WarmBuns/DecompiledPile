using System.Collections.Generic;
using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.Engi.MineDeployer;

public class FireMine : BaseMineDeployerState
{
	public static float duration;

	public static float launchApex;

	public static float patternRadius;

	public static GameObject projectilePrefab;

	private int fireIndex;

	private static Vector3[] velocities;

	private static bool velocitiesResolved;

	public override void OnEnter()
	{
		base.OnEnter();
		if (base.isAuthority)
		{
			ResolveVelocities();
			Transform transform = base.transform.Find("FirePoint");
			ProjectileDamage component = GetComponent<ProjectileDamage>();
			Vector3 forward = transform.TransformVector(velocities[fireIndex]);
			FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
			fireProjectileInfo.crit = component.crit;
			fireProjectileInfo.damage = component.damage;
			fireProjectileInfo.damageColorIndex = component.damageColorIndex;
			fireProjectileInfo.force = component.force;
			fireProjectileInfo.owner = base.owner;
			fireProjectileInfo.position = transform.position;
			fireProjectileInfo.procChainMask = base.projectileController.procChainMask;
			fireProjectileInfo.projectilePrefab = projectilePrefab;
			fireProjectileInfo.rotation = Quaternion.LookRotation(forward);
			fireProjectileInfo.fuseOverride = -1f;
			fireProjectileInfo.useFuseOverride = false;
			fireProjectileInfo.speedOverride = forward.magnitude;
			fireProjectileInfo.useSpeedOverride = true;
			FireProjectileInfo fireProjectileInfo2 = fireProjectileInfo;
			ProjectileManager.instance.FireProjectile(fireProjectileInfo2);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && duration <= base.fixedAge)
		{
			int num = fireIndex + 1;
			if (num < velocities.Length)
			{
				outer.SetNextState(new FireMine
				{
					fireIndex = num
				});
			}
			else
			{
				outer.SetNextState(new WaitForDeath());
			}
		}
	}

	private static Vector3[] GeneratePoints(float radius)
	{
		Vector3[] array = new Vector3[9];
		Quaternion quaternion = Quaternion.AngleAxis(60f, Vector3.up);
		Quaternion quaternion2 = Quaternion.AngleAxis(120f, Vector3.up);
		Vector3 forward = Vector3.forward;
		array[0] = forward;
		array[1] = quaternion2 * array[0];
		array[2] = quaternion2 * array[1];
		float num = 1f;
		float num2 = Vector3.Distance(array[0], array[1]);
		float num3 = Mathf.Sqrt(num * num + num2 * num2) / num;
		array[3] = quaternion * (array[2] * num3);
		array[4] = quaternion2 * array[3];
		array[5] = quaternion2 * array[4];
		num3 = 1f;
		array[6] = quaternion * (array[5] * num3);
		array[7] = quaternion2 * array[6];
		array[8] = quaternion2 * array[7];
		float num4 = radius / array[8].magnitude;
		for (int i = 0; i < array.Length; i++)
		{
			array[i] *= num4;
		}
		return array;
	}

	private static Vector3[] GenerateHexPoints(float radius)
	{
		Vector3[] array = new Vector3[6];
		Quaternion quaternion = Quaternion.AngleAxis(60f, Vector3.up);
		ref Vector3 reference = ref array[0];
		reference = Vector3.forward * radius;
		for (int i = 1; i < array.Length; i++)
		{
			ref Vector3 reference2 = ref array[i];
			reference2 = quaternion * reference;
			reference = ref reference2;
		}
		return array;
	}

	private static Vector3[] GeneratePointsFromPattern(GameObject patternObject)
	{
		Transform transform = patternObject.transform;
		Vector3 position = transform.position;
		List<Vector3> list = new List<Vector3>();
		for (int i = 0; i < transform.childCount; i++)
		{
			Transform child = transform.GetChild(i);
			if (child.gameObject.activeInHierarchy)
			{
				list.Add(child.position - position);
			}
		}
		return list.ToArray();
	}

	private static Vector3[] GenerateVelocitiesFromPoints(Vector3[] points, float apex)
	{
		Vector3[] array = new Vector3[points.Length];
		float num = Trajectory.CalculateInitialYSpeedForHeight(apex);
		for (int i = 0; i < points.Length; i++)
		{
			Vector3 normalized = points[i].normalized;
			float num2 = Trajectory.CalculateGroundSpeedToClearDistance(num, points[i].magnitude);
			Vector3 vector = normalized * num2;
			vector.y = num;
			array[i] = vector;
		}
		return array;
	}

	private static void ResolveVelocities()
	{
		if (!velocitiesResolved)
		{
			velocities = GenerateVelocitiesFromPoints(GeneratePoints(patternRadius), launchApex);
			if (!Application.isEditor)
			{
				velocitiesResolved = true;
			}
		}
	}
}
