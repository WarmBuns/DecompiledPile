using UnityEngine;
using UnityEngine.Events;

namespace RoR2;

public class JumpVolume : MonoBehaviour
{
	public Transform targetElevationTransform;

	public Vector3 jumpVelocity;

	public float time;

	public string jumpSoundString;

	public UnityEvent onJump;

	public void OnTriggerStay(Collider other)
	{
		CharacterMotor component = other.GetComponent<CharacterMotor>();
		if ((bool)component && component.hasEffectiveAuthority && !component.doNotTriggerJumpVolumes)
		{
			onJump.Invoke();
			if (!component.disableAirControlUntilCollision)
			{
				Util.PlaySound(jumpSoundString, base.gameObject);
			}
			component.velocity = jumpVelocity;
			component.disableAirControlUntilCollision = true;
			component.Motor.ForceUnground();
		}
	}

	private void OnDrawGizmos()
	{
		int num = 20;
		float num2 = time / (float)num;
		Vector3 vector = base.transform.position;
		_ = base.transform.position;
		Vector3 vector2 = jumpVelocity;
		Gizmos.color = Color.yellow;
		for (int i = 0; i <= num; i++)
		{
			Vector3 vector3 = vector + vector2 * num2;
			vector2 += Physics.gravity * num2;
			Gizmos.DrawLine(vector3, vector);
			vector = vector3;
		}
	}
}
