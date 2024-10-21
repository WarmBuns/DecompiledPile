using UnityEngine;

public class RotateItem : MonoBehaviour
{
	public float spinSpeed = 30f;

	public float bobHeight = 0.3f;

	public Vector3 offsetVector = Vector3.zero;

	private float counter;

	private Vector3 initialPosition;

	private void Start()
	{
		initialPosition = base.transform.position;
	}

	private void Update()
	{
		counter += Time.deltaTime;
		base.transform.Rotate(new Vector3(0f, spinSpeed * Time.deltaTime, 0f), Space.World);
		if ((bool)base.transform.parent)
		{
			base.transform.localPosition = offsetVector + new Vector3(0f, 0f, Mathf.Sin(counter) * bobHeight);
		}
		else
		{
			base.transform.position = initialPosition + new Vector3(0f, Mathf.Sin(counter) * bobHeight, 0f);
		}
	}
}
