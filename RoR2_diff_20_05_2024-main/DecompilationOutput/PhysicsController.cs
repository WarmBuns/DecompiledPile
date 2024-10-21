using UnityEngine;

public class PhysicsController : MonoBehaviour
{
	public Vector3 centerOfMass = Vector3.zero;

	private Rigidbody carRigidbody;

	public Transform cameraTransform;

	public Vector3 PID = new Vector3(1f, 0f, 0f);

	public bool turnOnInput;

	private Vector3 errorSum = Vector3.zero;

	private Vector3 deltaError = Vector3.zero;

	private Vector3 lastError = Vector3.zero;

	private Vector3 desiredHeading;

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawSphere(base.transform.TransformPoint(centerOfMass), 0.5f);
	}

	private void Awake()
	{
		carRigidbody = GetComponent<Rigidbody>();
	}

	private void Update()
	{
	}

	private void FixedUpdate()
	{
		if (!turnOnInput || Input.GetAxis("Vertical") > 0f || Input.GetAxis("Vertical") > 0f)
		{
			desiredHeading = cameraTransform.forward;
			desiredHeading = Vector3.Project(desiredHeading, base.transform.forward);
			desiredHeading = cameraTransform.forward - desiredHeading;
			Debug.DrawRay(base.transform.position, desiredHeading * 15f, Color.magenta);
		}
		Vector3 vector = -base.transform.up;
		Debug.DrawRay(base.transform.position, vector * 15f, Color.blue);
		Vector3 vector2 = Vector3.Cross(vector, desiredHeading);
		Debug.DrawRay(base.transform.position, vector2 * 15f, Color.red);
		vector2.x = 0f;
		vector2.z = 0f;
		errorSum += vector2 * Time.fixedDeltaTime;
		deltaError = (vector2 - lastError) / Time.fixedDeltaTime;
		lastError = vector2;
		carRigidbody.AddTorque(vector2 * PID.x + errorSum * PID.y + deltaError * PID.z, ForceMode.Acceleration);
	}
}
