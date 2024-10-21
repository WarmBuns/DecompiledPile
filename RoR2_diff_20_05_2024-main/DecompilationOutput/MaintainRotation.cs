using UnityEngine;

[ExecuteAlways]
public class MaintainRotation : MonoBehaviour
{
	public Vector3 eulerAngles;

	private void Start()
	{
	}

	private void LateUpdate()
	{
		base.transform.eulerAngles = eulerAngles;
	}
}
