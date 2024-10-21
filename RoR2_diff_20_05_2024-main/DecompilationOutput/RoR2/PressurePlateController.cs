using UnityEngine;
using UnityEngine.Events;

namespace RoR2;

public class PressurePlateController : MonoBehaviour
{
	public bool enableOverlapSphere = true;

	public float overlapSphereRadius;

	public float overlapSphereFrequency;

	public string switchDownSoundString;

	public string switchUpSoundString;

	public UnityEvent OnSwitchDown;

	public UnityEvent OnSwitchUp;

	public Collider pingCollider;

	public AnimationCurve switchVisualPositionFromUpToDown;

	public AnimationCurve switchVisualPositionFromDownToUp;

	public Transform switchVisualTransform;

	private float overlapSphereStopwatch;

	private float animationStopwatch;

	private bool switchDown;

	private void Start()
	{
	}

	private void FixedUpdate()
	{
		if (enableOverlapSphere)
		{
			overlapSphereStopwatch += Time.fixedDeltaTime;
			if (overlapSphereStopwatch >= 1f / overlapSphereFrequency)
			{
				overlapSphereStopwatch -= 1f / overlapSphereFrequency;
				bool @switch = HGPhysics.DoesOverlapSphere(base.transform.position, overlapSphereRadius, (int)LayerIndex.CommonMasks.characterBodiesOrDefault | (int)LayerIndex.CommonMasks.fakeActorLayers);
				SetSwitch(@switch);
			}
		}
	}

	public void EnableOverlapSphere(bool input)
	{
		enableOverlapSphere = input;
	}

	public void SetSwitch(bool switchIsDown)
	{
		if (switchIsDown != switchDown)
		{
			if (switchIsDown)
			{
				animationStopwatch = 0f;
				Util.PlaySound(switchDownSoundString, base.gameObject);
				OnSwitchDown?.Invoke();
			}
			else
			{
				animationStopwatch = 0f;
				Util.PlaySound(switchUpSoundString, base.gameObject);
				OnSwitchUp?.Invoke();
			}
			switchDown = switchIsDown;
		}
	}

	private void Update()
	{
		animationStopwatch += Time.deltaTime;
		if ((bool)switchVisualTransform)
		{
			Vector3 localPosition = switchVisualTransform.transform.localPosition;
			if (switchDown)
			{
				localPosition.z = switchVisualPositionFromUpToDown.Evaluate(animationStopwatch);
			}
			else
			{
				localPosition.z = switchVisualPositionFromDownToUp.Evaluate(animationStopwatch);
			}
			switchVisualTransform.localPosition = localPosition;
		}
	}

	private void OnDrawGizmos()
	{
		Gizmos.DrawWireSphere(base.transform.position, overlapSphereRadius);
	}
}
