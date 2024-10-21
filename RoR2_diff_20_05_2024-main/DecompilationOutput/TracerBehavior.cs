using UnityEngine;

public class TracerBehavior : MonoBehaviour
{
	public float speed = 1f;

	public Vector3[] positions;

	private Vector3 direction;

	private void Start()
	{
		direction = Vector3.Normalize(positions[1] - positions[0]);
	}

	private void Update()
	{
		if (Vector3.Distance(base.transform.position, positions[1]) > speed * Time.deltaTime)
		{
			base.transform.position += direction * speed * Time.deltaTime;
		}
	}
}
