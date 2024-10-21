using UnityEngine;

public class ScaledCamera : MonoBehaviour
{
	public float scale = 1f;

	private bool foundCamera;

	private Vector3 offset;

	private void Start()
	{
	}

	private void LateUpdate()
	{
		Camera main = Camera.main;
		if (main != null)
		{
			if (!foundCamera)
			{
				foundCamera = true;
				offset = main.transform.position - base.transform.position;
			}
			base.transform.eulerAngles = main.transform.eulerAngles;
			base.transform.position = main.transform.position / scale - offset;
		}
	}
}
