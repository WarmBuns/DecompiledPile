using UnityEngine;
using UnityEngine.Serialization;

public class VectorPID : MonoBehaviour
{
	[FormerlySerializedAs("name")]
	[Tooltip("Just a field for user naming. Doesn't do anything.")]
	public string customName;

	[Tooltip("PID Constants.")]
	public Vector3 PID = new Vector3(1f, 0f, 0f);

	[HideInInspector]
	[Tooltip("The vector we are currently at.")]
	public Vector3 inputVector = Vector3.zero;

	[HideInInspector]
	[Tooltip("The vector we want to be at.")]
	public Vector3 targetVector = Vector3.zero;

	[HideInInspector]
	[Tooltip("Vector output from PID controller; what we read.")]
	public Vector3 outputVector = Vector3.zero;

	[Tooltip("This is an euler angle, so we need to wrap correctly")]
	public bool isAngle;

	public float gain = 1f;

	private Vector3 errorSum = Vector3.zero;

	private Vector3 deltaError = Vector3.zero;

	private Vector3 lastError = Vector3.zero;

	private float lastTimer;

	private float timer;

	private void Start()
	{
	}

	private void FixedUpdate()
	{
		timer += Time.fixedDeltaTime;
	}

	public Vector3 UpdatePID()
	{
		float num = timer - lastTimer;
		lastTimer = timer;
		if (num != 0f)
		{
			Vector3 vector;
			if (isAngle)
			{
				vector = Vector3.zero;
				vector.x = Mathf.DeltaAngle(inputVector.x, targetVector.x);
				vector.y = Mathf.DeltaAngle(inputVector.y, targetVector.y);
				vector.z = Mathf.DeltaAngle(inputVector.z, targetVector.z);
			}
			else
			{
				vector = targetVector - inputVector;
			}
			errorSum += vector * num;
			deltaError = (vector - lastError) / num;
			lastError = vector;
			outputVector = vector * PID.x + errorSum * PID.y + deltaError * PID.z;
			return outputVector * gain;
		}
		return Vector3.zero;
	}
}
