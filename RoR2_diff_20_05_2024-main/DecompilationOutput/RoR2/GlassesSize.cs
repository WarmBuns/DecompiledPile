using UnityEngine;

namespace RoR2;

[ExecuteAlways]
public class GlassesSize : MonoBehaviour
{
	public Transform glassesModelBase;

	public Transform glassesBridgeLeft;

	public Transform glassesBridgeRight;

	public float bridgeOffsetScale;

	public Vector3 offsetVector = Vector3.right;

	private void Start()
	{
	}

	private void Update()
	{
		UpdateGlasses();
	}

	private void UpdateGlasses()
	{
		Vector3 localScale = base.transform.localScale;
		float num = Mathf.Max(localScale.y, localScale.z);
		Vector3 localScale2 = new Vector3(1f / localScale.x * num, 1f / localScale.y * num, 1f / localScale.z * num);
		if ((bool)glassesModelBase)
		{
			glassesModelBase.transform.localScale = localScale2;
		}
		if ((bool)glassesBridgeLeft && (bool)glassesBridgeRight)
		{
			float num2 = (localScale.x / num - 1f) * bridgeOffsetScale;
			glassesBridgeLeft.transform.localPosition = offsetVector * (0f - num2);
			glassesBridgeRight.transform.localPosition = offsetVector * num2;
		}
	}
}
