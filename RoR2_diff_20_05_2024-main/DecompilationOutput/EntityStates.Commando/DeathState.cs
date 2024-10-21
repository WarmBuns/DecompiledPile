using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Commando;

public class DeathState : GenericCharacterDeath
{
	private Vector3 previousPosition;

	private float upSpeedVelocity;

	private float upSpeed;

	private Animator modelAnimator;

	protected override bool shouldAutoDestroy => false;

	public override void OnEnter()
	{
		base.OnEnter();
		Vector3 force = Vector3.up * 3f;
		if ((bool)base.characterMotor)
		{
			force += base.characterMotor.velocity;
			base.characterMotor.enabled = false;
		}
		if ((bool)base.cachedModelTransform)
		{
			RagdollController component = base.cachedModelTransform.GetComponent<RagdollController>();
			if ((bool)component)
			{
				component.BeginRagdoll(force);
			}
		}
	}

	protected override void PlayDeathAnimation(float crossfadeDuration = 0.1f)
	{
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (NetworkServer.active && base.fixedAge > 4f)
		{
			EntityState.Destroy(base.gameObject);
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Death;
	}
}
