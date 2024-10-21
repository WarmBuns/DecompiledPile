using RoR2;
using UnityEngine;

namespace EntityStates.Headstompers;

public class HeadstompersIdle : BaseHeadstompersState
{
	private float inputStopwatch;

	public static float inputConfirmationDelay = 0.1f;

	private bool jumpBoostOk;

	public static GameObject jumpEffect;

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority)
		{
			FixedUpdateAuthority();
		}
	}

	private void FixedUpdateAuthority()
	{
		inputStopwatch = (base.slamButtonDown ? (inputStopwatch + GetDeltaTime()) : 0f);
		if (base.isGrounded)
		{
			jumpBoostOk = true;
		}
		else if (jumpBoostOk && base.jumpButtonDown && (bool)bodyMotor)
		{
			Vector3 velocity = bodyMotor.velocity;
			if (velocity.y > 0f)
			{
				velocity.y *= 2f;
				bodyMotor.velocity = velocity;
				jumpBoostOk = false;
			}
			EffectManager.SimpleImpactEffect(jumpEffect, bodyGameObject.transform.position, Vector3.up, transmit: true);
		}
		if (inputStopwatch >= inputConfirmationDelay && !base.isGrounded)
		{
			outer.SetNextState(new HeadstompersCharge());
		}
	}
}
