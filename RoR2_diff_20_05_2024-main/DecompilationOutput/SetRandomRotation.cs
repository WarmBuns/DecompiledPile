using UnityEngine;

public class SetRandomRotation : MonoBehaviour
{
	public bool setRandomXRotation;

	public bool setRandomYRotation;

	public bool setRandomZRotation;

	private void Start()
	{
		Vector3 localEulerAngles = base.transform.localEulerAngles;
		if (setRandomXRotation)
		{
			float x = Random.Range(0f, 359f);
			localEulerAngles.x = x;
		}
		if (setRandomYRotation)
		{
			float y = Random.Range(0f, 359f);
			localEulerAngles.y = y;
		}
		if (setRandomZRotation)
		{
			float z = Random.Range(0f, 359f);
			localEulerAngles.z = z;
		}
		base.transform.localEulerAngles = localEulerAngles;
	}

	private void Update()
	{
	}
}
