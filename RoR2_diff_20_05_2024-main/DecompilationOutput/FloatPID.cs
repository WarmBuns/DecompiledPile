using UnityEngine;

public class FloatPID : MonoBehaviour
{
	[Tooltip("PID Constants.")]
	public Vector3 PID = new Vector3(1f, 0f, 0f);

	public float gain = 1f;

	[HideInInspector]
	[Tooltip("The value we are currently at.")]
	public float inputFloat;

	[HideInInspector]
	[Tooltip("The value we want to be at.")]
	public float targetFloat;

	[HideInInspector]
	[Tooltip("Value output from PID controller; what we read.")]
	public float outputFloat;

	public float timeBetweenUpdates;

	private float timer;

	private float errorSum;

	private float deltaError;

	private float lastError;

	private float lastTimer;

	public bool automaticallyUpdate;

	private void Start()
	{
	}

	private void FixedUpdate()
	{
		timer += Time.fixedDeltaTime;
		if (automaticallyUpdate && timer > timeBetweenUpdates)
		{
			timer -= timeBetweenUpdates;
			outputFloat = UpdatePID();
		}
	}

	public float UpdatePID()
	{
		float num = timer - lastTimer;
		lastTimer = timer;
		float num2 = targetFloat - inputFloat;
		errorSum += num2 * num;
		deltaError = (num2 - lastError) / num;
		lastError = num2;
		return (num2 * PID.x + errorSum * PID.y + deltaError * PID.z) * gain;
	}
}
