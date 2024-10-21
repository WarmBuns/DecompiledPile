using RoR2;
using UnityEngine.Networking;

namespace EntityStates.Missions.Arena.NullWard;

public class Active : NullWardBaseState
{
	public static string soundEntryEvent;

	public static string soundLoopStartEvent;

	public static string soundLoopEndEvent;

	private static Run.FixedTimeStamp startTime;

	private HoldoutZoneController holdoutZoneController;

	public override void OnEnter()
	{
		base.OnEnter();
		holdoutZoneController = GetComponent<HoldoutZoneController>();
		holdoutZoneController.enabled = true;
		holdoutZoneController.baseRadius = NullWardBaseState.wardRadiusOn;
		purchaseInteraction.SetAvailable(newAvailable: false);
		base.arenaMissionController.rewardSpawnPosition = childLocator.FindChild("RewardSpawn").gameObject;
		base.arenaMissionController.monsterSpawnPosition = childLocator.FindChild("MonsterSpawn").gameObject;
		childLocator.FindChild("ActiveEffect").gameObject.SetActive(value: true);
		if (NetworkServer.active)
		{
			base.arenaMissionController.BeginRound();
		}
		if (base.isAuthority)
		{
			startTime = Run.FixedTimeStamp.now;
		}
		Util.PlaySound(soundEntryEvent, base.gameObject);
		Util.PlaySound(soundLoopStartEvent, base.gameObject);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		sphereZone.Networkradius = holdoutZoneController.currentRadius;
		if (base.isAuthority && holdoutZoneController.charge >= 1f)
		{
			outer.SetNextState(new Complete());
		}
	}

	public override void OnExit()
	{
		if ((bool)holdoutZoneController)
		{
			holdoutZoneController.enabled = false;
		}
		Util.PlaySound(soundLoopEndEvent, base.gameObject);
		childLocator.FindChild("ActiveEffect").gameObject.SetActive(value: false);
		childLocator.FindChild("WardOnEffect").gameObject.SetActive(value: false);
		base.OnExit();
	}

	public override void OnSerialize(NetworkWriter writer)
	{
		base.OnSerialize(writer);
		writer.Write(startTime);
	}

	public override void OnDeserialize(NetworkReader reader)
	{
		base.OnDeserialize(reader);
		startTime = reader.ReadFixedTimeStamp();
	}
}
