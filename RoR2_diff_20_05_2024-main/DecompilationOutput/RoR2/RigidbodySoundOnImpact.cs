using RoR2.Audio;
using UnityEngine;

namespace RoR2;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class RigidbodySoundOnImpact : MonoBehaviour
{
	private Rigidbody rb;

	public string impactSoundString;

	public NetworkSoundEventDef networkedSoundEvent;

	public float minimumRelativeVelocityMagnitude;

	private float ditherTimer;

	private void Start()
	{
		rb = GetComponent<Rigidbody>();
	}

	private void FixedUpdate()
	{
		ditherTimer -= Time.fixedDeltaTime;
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (!(ditherTimer > 0f) && !rb.isKinematic && collision.transform.gameObject.layer == LayerIndex.world.intVal && collision.relativeVelocity.sqrMagnitude > minimumRelativeVelocityMagnitude * minimumRelativeVelocityMagnitude)
		{
			if (impactSoundString != null)
			{
				Util.PlaySound(impactSoundString, base.gameObject);
			}
			if (networkedSoundEvent != null)
			{
				PointSoundManager.EmitSoundServer(networkedSoundEvent.index, base.transform.position);
			}
			ditherTimer = 0.5f;
		}
	}
}
