using RoR2;
using UnityEngine;

public class BobObject : MonoBehaviour
{
	public float bobDelay;

	public Vector3 bobDistance = Vector3.zero;

	private Vector3 initialPosition;

	private void Start()
	{
		if ((bool)base.transform.parent)
		{
			initialPosition = base.transform.localPosition;
		}
		else
		{
			initialPosition = base.transform.position;
		}
	}

	private void FixedUpdate()
	{
		if ((bool)Run.instance)
		{
			Vector3 vector = initialPosition + bobDistance * Mathf.Sin(Run.instance.fixedTime - bobDelay);
			if ((bool)base.transform.parent)
			{
				base.transform.localPosition = vector;
			}
			else
			{
				base.transform.position = vector;
			}
		}
	}
}
