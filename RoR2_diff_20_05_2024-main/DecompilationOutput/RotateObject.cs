using UnityEngine;

public class RotateObject : MonoBehaviour
{
	public Vector3 rotationSpeed;

	private void Start()
	{
	}

	private void Update()
	{
		base.transform.Rotate(rotationSpeed * Time.deltaTime);
	}
}
