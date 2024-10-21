using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Projectile;

[RequireComponent(typeof(ProjectileTargetComponent))]
[RequireComponent(typeof(Rigidbody))]
public class MissileController : MonoBehaviour
{
	private new Transform transform;

	private Rigidbody rigidbody;

	private TeamFilter teamFilter;

	private ProjectileTargetComponent targetComponent;

	public float maxVelocity;

	public float rollVelocity;

	public float acceleration;

	public float delayTimer;

	public float giveupTimer = 8f;

	public float deathTimer = 10f;

	private float timer;

	private QuaternionPID torquePID;

	public float turbulence;

	public float maxSeekDistance = 40f;

	private BullseyeSearch search = new BullseyeSearch();

	private void Awake()
	{
		if (!NetworkServer.active)
		{
			base.enabled = false;
			return;
		}
		transform = base.transform;
		rigidbody = GetComponent<Rigidbody>();
		torquePID = GetComponent<QuaternionPID>();
		teamFilter = GetComponent<TeamFilter>();
		targetComponent = GetComponent<ProjectileTargetComponent>();
	}

	private void FixedUpdate()
	{
		timer += Time.deltaTime;
		if (timer < giveupTimer)
		{
			rigidbody.velocity = transform.forward * maxVelocity;
			if ((bool)targetComponent.target && timer >= delayTimer)
			{
				rigidbody.velocity = transform.forward * (maxVelocity + timer * acceleration);
				Vector3 vector = targetComponent.target.position + Random.insideUnitSphere * turbulence - transform.position;
				if (vector != Vector3.zero)
				{
					Quaternion rotation = transform.rotation;
					Quaternion targetQuat = Util.QuaternionSafeLookRotation(vector);
					torquePID.inputQuat = rotation;
					torquePID.targetQuat = targetQuat;
					rigidbody.angularVelocity = torquePID.UpdatePID();
				}
			}
		}
		if (!targetComponent.target)
		{
			targetComponent.target = FindTarget();
		}
		else
		{
			HealthComponent component = targetComponent.target.GetComponent<HealthComponent>();
			if ((bool)component && !component.alive)
			{
				targetComponent.target = FindTarget();
			}
		}
		if (timer > deathTimer)
		{
			Object.Destroy(base.gameObject);
		}
	}

	private Transform FindTarget()
	{
		search.searchOrigin = transform.position;
		search.searchDirection = transform.forward;
		search.teamMaskFilter.RemoveTeam(teamFilter.teamIndex);
		search.RefreshCandidates();
		return search.GetResults().FirstOrDefault()?.transform;
	}
}
