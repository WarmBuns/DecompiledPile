using System.Collections.ObjectModel;
using RoR2;
using UnityEngine;

namespace EntityStates.AncientWispMonster;

public class ChannelRain : BaseState
{
	private float castTimer;

	public static float baseDuration = 4f;

	public static float explosionDelay = 2f;

	public static int explosionCount = 10;

	public static float damageCoefficient;

	public static float randomRadius;

	public static float radius;

	public static GameObject delayPrefab;

	private float duration;

	private float durationBetweenCast;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration;
		durationBetweenCast = baseDuration / (float)explosionCount / attackSpeedStat;
		PlayCrossfade("Body", "ChannelRain", 0.3f);
	}

	private void PlaceRain()
	{
		Vector3 vector = Vector3.zero;
		Ray aimRay = GetAimRay();
		aimRay.origin += Random.insideUnitSphere * randomRadius;
		if (Physics.Raycast(aimRay, out var hitInfo, (int)LayerIndex.world.mask))
		{
			vector = hitInfo.point;
		}
		if (!(vector != Vector3.zero))
		{
			return;
		}
		Transform transform = FindTargetClosest(vector, base.characterBody.GetComponent<TeamComponent>().teamIndex switch
		{
			TeamIndex.Monster => TeamIndex.Player, 
			TeamIndex.Player => TeamIndex.Monster, 
			_ => TeamIndex.Neutral, 
		});
		Vector3 vector2 = vector;
		if ((bool)transform)
		{
			vector2 = transform.transform.position;
		}
		vector2 += Random.insideUnitSphere * randomRadius;
		Ray ray = default(Ray);
		ray.origin = vector2 + Vector3.up * randomRadius;
		ray.direction = Vector3.down;
		if (Physics.Raycast(ray, out hitInfo, 500f, LayerIndex.world.mask))
		{
			Vector3 point = hitInfo.point;
			Quaternion rotation = Util.QuaternionSafeLookRotation(hitInfo.normal);
			GameObject obj = Object.Instantiate(delayPrefab, point, rotation);
			DelayBlast component = obj.GetComponent<DelayBlast>();
			component.position = point;
			component.baseDamage = base.characterBody.damage * damageCoefficient;
			component.baseForce = 2000f;
			component.bonusForce = Vector3.up * 1000f;
			component.radius = radius;
			component.attacker = base.gameObject;
			component.inflictor = null;
			component.crit = Util.CheckRoll(critStat, base.characterBody.master);
			component.maxTimer = explosionDelay;
			obj.GetComponent<TeamFilter>().teamIndex = TeamComponent.GetObjectTeam(component.attacker);
			obj.transform.localScale = new Vector3(radius, radius, 1f);
			ScaleParticleSystemDuration component2 = obj.GetComponent<ScaleParticleSystemDuration>();
			if ((bool)component2)
			{
				component2.newDuration = explosionDelay;
			}
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		castTimer += GetDeltaTime();
		if (castTimer >= durationBetweenCast)
		{
			PlaceRain();
			castTimer -= durationBetweenCast;
		}
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextState(new EndRain());
		}
	}

	private Transform FindTargetClosest(Vector3 point, TeamIndex enemyTeam)
	{
		ReadOnlyCollection<TeamComponent> teamMembers = TeamComponent.GetTeamMembers(enemyTeam);
		float num = 99999f;
		Transform result = null;
		for (int i = 0; i < teamMembers.Count; i++)
		{
			float num2 = Vector3.SqrMagnitude(teamMembers[i].transform.position - point);
			if (num2 < num)
			{
				num = num2;
				result = teamMembers[i].transform;
			}
		}
		return result;
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Frozen;
	}
}
