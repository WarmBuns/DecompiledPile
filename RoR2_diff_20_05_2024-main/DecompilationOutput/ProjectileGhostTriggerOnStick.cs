using UnityEngine;
using UnityEngine.Events;

public class ProjectileGhostTriggerOnStick : MonoBehaviour
{
	private Vector3 lastPosition;

	private Quaternion lastRotation;

	private bool begunMoving;

	private bool finishedMoving;

	[SerializeField]
	[Tooltip("If the object is rotating, will we consider that movement?")]
	private bool trackRotationAsMovement;

	[SerializeField]
	private UnityEvent OnStick;

	private void OnEnable()
	{
		begunMoving = false;
		finishedMoving = false;
		lastPosition = base.transform.position;
		lastRotation = base.transform.rotation;
	}

	private void Update()
	{
		Vector3 position = base.transform.position;
		Quaternion rotation = base.transform.rotation;
		if (!begunMoving && position != lastPosition && (!trackRotationAsMovement || (trackRotationAsMovement && rotation != lastRotation)))
		{
			begunMoving = true;
		}
		else if (begunMoving && !finishedMoving && position == lastPosition && (!trackRotationAsMovement || (trackRotationAsMovement && rotation == lastRotation)))
		{
			RunStuckMethod();
			finishedMoving = true;
		}
		lastPosition = position;
		lastRotation = rotation;
	}

	public void RunStuckMethod()
	{
		OnStick.Invoke();
	}
}
