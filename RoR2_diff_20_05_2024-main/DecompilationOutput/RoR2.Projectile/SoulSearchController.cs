using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Projectile;

[RequireComponent(typeof(ProjectileTargetComponent))]
[RequireComponent(typeof(Rigidbody))]
public class SoulSearchController : MonoBehaviour
{
	private new Transform transform;

	private Rigidbody rigidbody;

	private TeamFilter teamFilter;

	private HurtBox trackingTarget;

	private ProjectileController projectileController;

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
		projectileController = GetComponent<ProjectileController>();
	}

	private void FixedUpdate()
	{
		timer += Time.fixedDeltaTime;
		if (!trackingTarget)
		{
			trackingTarget = projectileController.owner.GetComponent<HuntressTracker>().GetTrackingTarget();
		}
		else
		{
			HealthComponent component = trackingTarget.GetComponent<HealthComponent>();
			if ((bool)component && !component.alive)
			{
				Object.Destroy(base.gameObject);
			}
		}
		if (timer > deathTimer)
		{
			Object.Destroy(base.gameObject);
		}
		rigidbody.velocity = transform.forward * maxVelocity;
		if ((bool)trackingTarget && timer >= delayTimer)
		{
			rigidbody.velocity = transform.forward * (maxVelocity + timer * acceleration);
			Vector3 vector = trackingTarget.transform.position + Random.insideUnitSphere * turbulence - transform.position;
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
}
