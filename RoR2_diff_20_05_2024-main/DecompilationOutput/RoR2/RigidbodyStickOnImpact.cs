using UnityEngine;

namespace RoR2;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class RigidbodyStickOnImpact : MonoBehaviour
{
	private Rigidbody rb;

	public string stickSoundString;

	public GameObject stickEffectPrefab;

	public float minimumRelativeVelocityMagnitude;

	public AnimationCurve embedDistanceCurve;

	private bool stuck;

	private float stopwatchSinceStuck;

	private Vector3 contactNormal;

	private Vector3 contactPosition;

	private Vector3 transformPositionWhenContacted;

	private void Awake()
	{
		rb = GetComponent<Rigidbody>();
	}

	private void Update()
	{
		if (stuck)
		{
			stopwatchSinceStuck += Time.deltaTime;
			base.transform.position = transformPositionWhenContacted + embedDistanceCurve.Evaluate(stopwatchSinceStuck) * contactNormal;
		}
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (!stuck && !rb.isKinematic && collision.transform.gameObject.layer == LayerIndex.world.intVal && collision.relativeVelocity.sqrMagnitude > minimumRelativeVelocityMagnitude * minimumRelativeVelocityMagnitude)
		{
			stuck = true;
			ContactPoint contact = collision.GetContact(0);
			contactNormal = contact.normal;
			contactPosition = contact.point;
			transformPositionWhenContacted = base.transform.position;
			EffectManager.SpawnEffect(stickEffectPrefab, new EffectData
			{
				origin = contactPosition,
				rotation = Util.QuaternionSafeLookRotation(contactNormal)
			}, transmit: false);
			Util.PlaySound(stickSoundString, base.gameObject);
			rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
			rb.detectCollisions = false;
			rb.isKinematic = true;
			rb.velocity = Vector3.zero;
		}
	}
}
