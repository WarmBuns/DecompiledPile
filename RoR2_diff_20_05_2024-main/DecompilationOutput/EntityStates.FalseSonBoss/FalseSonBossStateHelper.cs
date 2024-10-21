using RoR2;
using UnityEngine;

namespace EntityStates.FalseSonBoss;

public static class FalseSonBossStateHelper
{
	private static int hitLimit = 2;

	private static float coolDownDuration = 2f;

	private static int hitCount = 0;

	private static float coolDownTimeStamp = -3f;

	private static float maxDistanceSquared = 225f;

	private static bool CheckPlayerOrMinionsForProximity(PlayerCharacterMasterController playerController, Vector3 playerPosition, Vector3 aimDirection, Vector3 bossPosition)
	{
		Vector3 vectorToCharacter;
		if (CharacterIsInFrontOfUs(playerPosition))
		{
			return true;
		}
		MinionOwnership.MinionGroup minionGroup = MinionOwnership.MinionGroup.FindGroup(playerController.master.netId);
		if (minionGroup == null || minionGroup.memberCount < 1)
		{
			return false;
		}
		MinionOwnership[] members = minionGroup.members;
		foreach (MinionOwnership minionOwnership in members)
		{
			if (!(minionOwnership == null))
			{
				Vector3 value = (minionOwnership.GetComponent<CharacterMaster>()?.GetBody()?.corePosition).Value;
				if (value != Vector3.zero && CharacterIsInFrontOfUs(value))
				{
					return true;
				}
			}
		}
		return false;
		bool CharacterIsInFrontOfUs(Vector3 position)
		{
			vectorToCharacter = position - bossPosition;
			if (Vector3.SqrMagnitude(vectorToCharacter) > maxDistanceSquared)
			{
				return false;
			}
			if (Vector3.Dot(vectorToCharacter.normalized, aimDirection) <= 0f)
			{
				return false;
			}
			return true;
		}
	}

	public static bool TrySwitchToMeleeSwing(ref bool stateAborted, Transform bossTransform, Vector3 aimDirection, GenericSkill previousSkill, EntityStateMachine outer)
	{
		if (bossTransform == null)
		{
			return false;
		}
		if (PlatformSystems.networkManager.serverFixedTime - coolDownTimeStamp < coolDownDuration)
		{
			return false;
		}
		stateAborted = false;
		foreach (PlayerCharacterMasterController instance in PlayerCharacterMasterController.instances)
		{
			if (!(instance == null))
			{
				CharacterBody body = instance.master.GetBody();
				if (!(body == null) && CheckPlayerOrMinionsForProximity(instance, body.corePosition, aimDirection, bossTransform.position))
				{
					stateAborted = true;
					break;
				}
			}
		}
		if (stateAborted)
		{
			outer.SetNextState(new SwatAwayPlayersWindup());
			if ((bool)previousSkill)
			{
				previousSkill.Reset();
			}
			hitCount++;
			if (hitCount >= hitLimit)
			{
				coolDownTimeStamp = PlatformSystems.networkManager.serverFixedTime;
				hitCount = 0;
			}
		}
		return stateAborted;
	}
}
