using UnityEngine;

namespace RoR2;

[RequireComponent(typeof(BezierCurveLine))]
public class TetherVfx : MonoBehaviour
{
	public AnimateShaderAlpha fadeOut;

	[Tooltip("The transform to position at the target.")]
	public Transform tetherEndTransform;

	[Tooltip("The transform to position the end to.")]
	public Transform tetherTargetTransform;

	private BezierCurveLine curve;

	private void Start()
	{
		curve = GetComponent<BezierCurveLine>();
	}

	public void Update()
	{
		if ((bool)tetherTargetTransform && (bool)tetherEndTransform)
		{
			tetherEndTransform.position = tetherTargetTransform.position;
		}
		if ((bool)curve && (bool)tetherTargetTransform)
		{
			curve.p1 = tetherTargetTransform.position;
		}
	}

	public void Terminate()
	{
		if ((bool)fadeOut)
		{
			fadeOut.enabled = true;
		}
		else
		{
			Object.Destroy(base.gameObject);
		}
	}
}
