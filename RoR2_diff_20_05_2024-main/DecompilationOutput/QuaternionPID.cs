using UnityEngine;
using UnityEngine.Serialization;

public class QuaternionPID : MonoBehaviour
{
	[FormerlySerializedAs("name")]
	[Tooltip("Just a field for user naming. Doesn't do anything.")]
	public string customName;

	[Tooltip("PID Constants.")]
	public Vector3 PID = new Vector3(1f, 0f, 0f);

	[Tooltip("The quaternion we are currently at.")]
	public Quaternion inputQuat = Quaternion.identity;

	[Tooltip("The quaternion we want to be at.")]
	public Quaternion targetQuat = Quaternion.identity;

	[HideInInspector]
	[Tooltip("Vector output from PID controller; what we read.")]
	public Vector3 outputVector = Vector3.zero;

	public float gain = 1f;

	private Vector3 errorSum = Vector3.zero;

	private Vector3 deltaError = Vector3.zero;

	private Vector3 lastError = Vector3.zero;

	private float lastUpdateTime;

	private void Start()
	{
		gain *= 60f * Time.fixedDeltaTime;
		lastUpdateTime = Time.time;
	}

	public Vector3 UpdatePID()
	{
		float time = Time.time;
		float num = time - lastUpdateTime;
		lastUpdateTime = time;
		if (num != 0f)
		{
			Quaternion quaternion = targetQuat * Quaternion.Inverse(inputQuat);
			if (quaternion.w < 0f)
			{
				quaternion.x *= -1f;
				quaternion.y *= -1f;
				quaternion.z *= -1f;
				quaternion.w *= -1f;
			}
			Vector3 vector = default(Vector3);
			vector.x = quaternion.x;
			vector.y = quaternion.y;
			vector.z = quaternion.z;
			errorSum += vector * num;
			deltaError = (vector - lastError) / num;
			lastError = vector;
			outputVector = vector * PID.x + errorSum * PID.y + deltaError * PID.z;
			return outputVector * gain;
		}
		return Vector3.zero;
	}
}
