using UnityEngine;

public class MoveCurve : MonoBehaviour
{
	public bool animateX;

	public bool animateY;

	public bool animateZ;

	public float curveScale = 1f;

	public AnimationCurve moveCurve;

	private float xValue;

	private float yValue;

	private float zValue;

	private void Start()
	{
	}

	private void Update()
	{
		if (animateX)
		{
			xValue = moveCurve.Evaluate(Time.time % (float)moveCurve.length) * curveScale;
		}
		else
		{
			xValue = base.transform.localPosition.x;
		}
		if (animateY)
		{
			yValue = moveCurve.Evaluate(Time.time % (float)moveCurve.length) * curveScale;
		}
		else
		{
			yValue = base.transform.localPosition.y;
		}
		if (animateZ)
		{
			zValue = moveCurve.Evaluate(Time.time % (float)moveCurve.length) * curveScale;
		}
		else
		{
			zValue = base.transform.localPosition.z;
		}
		base.transform.localPosition = new Vector3(xValue, yValue, zValue);
	}
}
