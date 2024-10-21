using UnityEngine;

namespace RoR2;

public class HoverEngineDisplay : MonoBehaviour
{
	public HoverEngine hoverEngine;

	[Tooltip("The local pitch at zero engine strength")]
	public float minPitch = -20f;

	[Tooltip("The local pitch at max engine strength")]
	public float maxPitch = 60f;

	public float smoothTime = 0.2f;

	public float forceScale = 1f;

	private float smoothVelocity;

	private void FixedUpdate()
	{
		Vector3 localEulerAngles = base.transform.localEulerAngles;
		float x = Mathf.SmoothDampAngle(target: Mathf.LerpAngle(t: Mathf.Clamp01(hoverEngine.forceStrength / hoverEngine.hoverForce * forceScale), a: minPitch, b: maxPitch), current: localEulerAngles.x, currentVelocity: ref smoothVelocity, smoothTime: smoothTime);
		base.transform.localRotation = Quaternion.Euler(x, 0f, 0f);
	}
}
