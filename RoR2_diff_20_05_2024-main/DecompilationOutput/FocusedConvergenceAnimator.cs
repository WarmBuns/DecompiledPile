using UnityEngine;

public class FocusedConvergenceAnimator : MonoBehaviour
{
	public bool animateX;

	public bool animateY;

	public bool animateZ;

	private AnimationCurve animCurve;

	private float xScale;

	private float yScale;

	private float zScale;

	public float scaleTime = 4f;

	private float tempScaleTimer = 4f;

	public float rotateTime = 4f;

	private float tempRotateTimer = 4f;

	private float scaleMax = 1.2f;

	private float scaleMin = 0.8f;

	private bool isScaling;

	private float xRotate;

	private float xRotateTop;

	private float yRotate;

	private float yRotateTop;

	private float zRotate;

	private float zRotateTop;

	private void Start()
	{
		tempScaleTimer = 0f;
		tempRotateTimer = 0f;
		animCurve = AnimationCurve.EaseInOut(0f, scaleMin, 1f, scaleMax);
	}

	private void FixedUpdate()
	{
		if (isScaling)
		{
			tempScaleTimer += Time.deltaTime;
			if (tempScaleTimer <= scaleTime * 0.333f)
			{
				float t = tempScaleTimer / (scaleTime * 0.333f);
				xScale = Mathf.Lerp(1f, scaleMax, t);
				yScale = Mathf.Lerp(1f, scaleMax, t);
				zScale = Mathf.Lerp(1f, scaleMax, t);
			}
			if (tempScaleTimer > scaleTime * 0.333f && tempScaleTimer <= scaleTime * 0.666f)
			{
				float t = (tempScaleTimer - scaleTime * 0.333f) / (scaleTime * 0.333f);
				xScale = Mathf.Lerp(scaleMax, scaleMin, t);
				yScale = Mathf.Lerp(scaleMax, scaleMin, t);
				zScale = Mathf.Lerp(scaleMax, scaleMin, t);
			}
			if (tempScaleTimer > scaleTime * 0.666f)
			{
				float t = (tempScaleTimer - scaleTime * 0.666f) / (scaleTime * 0.333f);
				xScale = Mathf.Lerp(scaleMin, 1f, t);
				yScale = Mathf.Lerp(scaleMin, 1f, t);
				zScale = Mathf.Lerp(scaleMin, 1f, t);
			}
			if (tempScaleTimer >= scaleTime)
			{
				isScaling = false;
				tempRotateTimer = 0f;
				xRotateTop = Random.Range(0f, 10f);
				yRotateTop = Random.Range(0f, 10f);
				zRotateTop = Random.Range(0f, 10f);
			}
			else
			{
				base.transform.localScale = new Vector3(xScale, yScale, zScale);
			}
		}
		else
		{
			tempRotateTimer += Time.deltaTime;
			if (tempRotateTimer <= rotateTime * 0.5f)
			{
				float t2 = tempRotateTimer / (rotateTime * 0.5f);
				xRotate = Mathf.Lerp(0f, xRotateTop, t2);
				yRotate = Mathf.Lerp(0f, yRotateTop, t2);
				zRotate = Mathf.Lerp(0f, zRotateTop, t2);
			}
			else
			{
				float t2 = (tempRotateTimer - rotateTime * 0.5f) / (rotateTime * 0.5f);
				xRotate = Mathf.Lerp(xRotateTop, 0f, t2);
				yRotate = Mathf.Lerp(yRotateTop, 0f, t2);
				zRotate = Mathf.Lerp(zRotateTop, 0f, t2);
			}
			if (tempRotateTimer >= rotateTime)
			{
				isScaling = true;
				tempScaleTimer = 0f;
			}
			else
			{
				base.transform.Rotate(new Vector3(xRotate, yRotate, zRotate));
			}
		}
	}
}
