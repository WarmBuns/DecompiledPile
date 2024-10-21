using System;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.MagmaWorm;

public class SteerAtTarget : BaseSkillState
{
	private WormBodyPositionsDriver wormBodyPositionsDriver;

	private Vector3? targetPosition;

	private static readonly float fastTurnThreshold = Mathf.Cos(MathF.PI / 6f);

	private static readonly float slowTurnThreshold = Mathf.Cos(MathF.PI / 3f);

	private static readonly float fastTurnRate = 180f;

	private static readonly float slowTurnRate = 90f;

	public override void OnEnter()
	{
		base.OnEnter();
		wormBodyPositionsDriver = GetComponent<WormBodyPositionsDriver>();
		if (base.isAuthority)
		{
			Ray aimRay = GetAimRay();
			if (Util.CharacterRaycast(base.gameObject, aimRay, out var hitInfo, 1000f, LayerIndex.CommonMasks.bullet, QueryTriggerInteraction.UseGlobal))
			{
				targetPosition = hitInfo.point;
			}
		}
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (NetworkServer.active && targetPosition.HasValue)
		{
			Vector3 vector = targetPosition.Value - wormBodyPositionsDriver.chaserPosition;
			if (vector != Vector3.zero && wormBodyPositionsDriver.chaserVelocity != Vector3.zero)
			{
				Vector3 normalized = vector.normalized;
				Vector3 normalized2 = wormBodyPositionsDriver.chaserVelocity.normalized;
				float num = Vector3.Dot(normalized, normalized2);
				float num2 = 0f;
				if (num >= slowTurnThreshold)
				{
					num2 = slowTurnRate;
					if (num >= fastTurnThreshold)
					{
						num2 = fastTurnRate;
					}
				}
				if (num2 != 0f)
				{
					wormBodyPositionsDriver.chaserVelocity = Vector3.RotateTowards(wormBodyPositionsDriver.chaserVelocity, vector, MathF.PI / 180f * num2 * GetDeltaTime(), 0f);
				}
			}
		}
		if (base.isAuthority && !IsKeyDownAuthority())
		{
			outer.SetNextStateToMain();
		}
	}

	public override void OnSerialize(NetworkWriter writer)
	{
		base.OnSerialize(writer);
		if (targetPosition.HasValue)
		{
			writer.Write(value: true);
			writer.Write(targetPosition.Value);
		}
		else
		{
			writer.Write(value: false);
		}
	}

	public override void OnDeserialize(NetworkReader reader)
	{
		base.OnDeserialize(reader);
		if (reader.ReadBoolean())
		{
			targetPosition = reader.ReadVector3();
		}
		else
		{
			targetPosition = null;
		}
	}
}
