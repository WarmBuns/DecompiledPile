using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class GravitatePickup : MonoBehaviour
{
	private Transform gravitateTarget;

	[Tooltip("The rigidbody to set the velocity of.")]
	public Rigidbody rigidbody;

	[Tooltip("The TeamFilter which controls which team can activate this trigger.")]
	public TeamFilter teamFilter;

	public float acceleration;

	public float maxSpeed;

	public bool gravitateAtFullHealth = true;

	private void Start()
	{
	}

	private void OnTriggerEnter(Collider other)
	{
		if (NetworkServer.active && !gravitateTarget && teamFilter.teamIndex != TeamIndex.None)
		{
			HealthComponent component = other.gameObject.GetComponent<HealthComponent>();
			if (TeamComponent.GetObjectTeam(other.gameObject) == teamFilter.teamIndex && (gravitateAtFullHealth || !component || component.health < component.fullHealth))
			{
				gravitateTarget = other.gameObject.transform;
			}
		}
	}

	private void FixedUpdate()
	{
		if ((bool)gravitateTarget)
		{
			rigidbody.velocity = Vector3.MoveTowards(rigidbody.velocity, (gravitateTarget.transform.position - base.transform.position).normalized * maxSpeed, acceleration);
		}
	}
}
