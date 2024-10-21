using System;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Artifacts;

public static class TeamDeathArtifactManager
{
	private static GameObject forceSpectatePrefab;

	private static bool inTeamKill;

	private static ArtifactDef myArtifactDef => RoR2Content.Artifacts.teamDeathArtifactDef;

	[SystemInitializer(new Type[] { typeof(ArtifactCatalog) })]
	private static void Init()
	{
		RunArtifactManager.onArtifactEnabledGlobal += OnArtifactEnabled;
		RunArtifactManager.onArtifactDisabledGlobal += OnArtifactDisabled;
		LegacyResourcesAPI.LoadAsyncCallback("Prefabs/NetworkedObjects/ForceSpectate", delegate(GameObject operationResult)
		{
			forceSpectatePrefab = operationResult;
		});
	}

	private static void OnArtifactEnabled(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
	{
		if (!(artifactDef != myArtifactDef))
		{
			GlobalEventManager.onCharacterDeathGlobal += OnServerCharacterDeathGlobal;
		}
	}

	private static void OnArtifactDisabled(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
	{
		if (!(artifactDef != myArtifactDef))
		{
			GlobalEventManager.onCharacterDeathGlobal -= OnServerCharacterDeathGlobal;
		}
	}

	private static void OnServerCharacterDeathGlobal(DamageReport damageReport)
	{
		TeamIndex teamIndex;
		NetworkUser victimNetworkUser;
		if (!inTeamKill && (bool)damageReport.victimMaster && (bool)damageReport.victimMaster.playerCharacterMasterController)
		{
			if (damageReport.victimMaster.inventory.GetItemCount(RoR2Content.Items.ExtraLife) <= 0 && damageReport.victimMaster.inventory.GetItemCount(DLC1Content.Items.ExtraLifeVoid) <= 0 && !damageReport.victimBody.HasBuff(DLC2Content.Buffs.ExtraLifeBuff) && damageReport.victimBody.equipmentSlot.equipmentIndex != DLC2Content.Equipment.HealAndRevive.equipmentIndex)
			{
				GameObject gameObject = UnityEngine.Object.Instantiate(forceSpectatePrefab);
				gameObject.GetComponent<ForceSpectate>().Networktarget = damageReport.victimBody.gameObject;
				NetworkServer.Spawn(gameObject);
				teamIndex = damageReport.victimTeamIndex;
				PlayerCharacterMasterController playerCharacterMasterController = damageReport.victimMaster.playerCharacterMasterController;
				victimNetworkUser = (playerCharacterMasterController ? playerCharacterMasterController.networkUser : null);
				RoR2Application.onNextUpdate += KillTeam;
			}
		}
		void KillTeam()
		{
			inTeamKill = true;
			Chat.SendBroadcastChat(new SubjectChatMessage
			{
				baseToken = "ARTIFACT_TEAMDEATH_DEATHMESSAGE",
				subjectAsNetworkUser = victimNetworkUser
			});
			ReadOnlyCollection<CharacterMaster> readOnlyInstancesList = CharacterMaster.readOnlyInstancesList;
			for (int num = readOnlyInstancesList.Count - 1; num >= 0; num--)
			{
				if (readOnlyInstancesList[num].teamIndex == teamIndex)
				{
					readOnlyInstancesList[num].TrueKill();
				}
			}
			inTeamKill = false;
		}
	}
}
