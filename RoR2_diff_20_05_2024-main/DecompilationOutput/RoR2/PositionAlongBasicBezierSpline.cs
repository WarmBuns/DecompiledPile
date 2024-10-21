using UnityEngine;

namespace RoR2;

[ExecuteAlways]
public class PositionAlongBasicBezierSpline : MonoBehaviour
{
	public BasicBezierSpline curve;

	[Range(0f, 1f)]
	public float normalizedPositionAlongCurve;

	private void Update()
	{
		if ((bool)curve)
		{
			base.transform.position = curve.Evaluate(normalizedPositionAlongCurve);
		}
	}
}
