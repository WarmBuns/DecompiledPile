using RoR2;
using UnityEngine.Networking;

namespace EntityStates.LaserTurbine;

public class RechargeState : LaserTurbineBaseState
{
	public static float baseDuration = 60f;

	public static int killChargesRequired = 4;

	public static int killChargeDuration = 4;

	public Run.FixedTimeStamp startTime { get; private set; }

	public Run.FixedTimeStamp readyTime { get; private set; }

	public override void OnEnter()
	{
		base.OnEnter();
		if (base.isAuthority)
		{
			startTime = Run.FixedTimeStamp.now;
			readyTime = startTime + baseDuration;
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && base.ownerBody.GetBuffCount(RoR2Content.Buffs.LaserTurbineKillCharge) >= killChargesRequired)
		{
			if (NetworkServer.active)
			{
				base.laserTurbineController.ExpendCharge();
			}
			outer.SetNextState(new ReadyState());
		}
	}

	public override void OnSerialize(NetworkWriter writer)
	{
		base.OnSerialize(writer);
		writer.Write(startTime);
		writer.Write(readyTime);
	}

	public override void OnDeserialize(NetworkReader reader)
	{
		base.OnDeserialize(reader);
		startTime = reader.ReadFixedTimeStamp();
		readyTime = reader.ReadFixedTimeStamp();
	}
}
