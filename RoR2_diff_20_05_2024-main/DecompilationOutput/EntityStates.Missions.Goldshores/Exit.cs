using RoR2;
using RoR2.ExpansionManagement;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Missions.Goldshores;

public class Exit : EntityState
{
	public override void OnEnter()
	{
		base.OnEnter();
		if (NetworkServer.active)
		{
			GameObject gameObject = DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(LegacyResourcesAPI.Load<SpawnCard>("SpawnCards/InteractableSpawnCard/iscGoldshoresPortal"), new DirectorPlacementRule
			{
				maxDistance = float.PositiveInfinity,
				minDistance = 10f,
				placementMode = DirectorPlacementRule.PlacementMode.NearestNode,
				position = base.transform.position,
				spawnOnTarget = GoldshoresMissionController.instance.bossSpawnPosition
			}, Run.instance.stageRng));
			if ((bool)gameObject)
			{
				Chat.SendBroadcastChat(new Chat.SimpleChatMessage
				{
					baseToken = "PORTAL_GOLDSHORES_OPEN"
				});
				gameObject.GetComponent<SceneExitController>().useRunNextStageScene = true;
			}
			if ((string.IsNullOrEmpty("FalseSonBossComplete") || !Run.instance.GetEventFlag("FalseSonBossComplete")) && IsValidStormTier() && (bool)DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(LegacyResourcesAPI.Load<SpawnCard>("SpawnCards/InteractableSpawnCard/iscColossusPortal"), new DirectorPlacementRule
			{
				maxDistance = 30f,
				minDistance = 10f,
				placementMode = DirectorPlacementRule.PlacementMode.NearestNode,
				position = base.transform.position
			}, Run.instance.stageRng)))
			{
				Chat.SendBroadcastChat(new Chat.SimpleChatMessage
				{
					baseToken = "PORTAL_STORM_OPEN"
				});
			}
			for (int num = CombatDirector.instancesList.Count - 1; num >= 0; num--)
			{
				CombatDirector.instancesList[num].enabled = false;
			}
		}
	}

	private bool IsValidStormTier()
	{
		Run instance = Run.instance;
		int stageOrder = instance.nextStageScene.stageOrder;
		ExpansionDef requiredExpansion = GoldshoresMissionController.instance.requiredExpansion;
		if (instance.IsExpansionEnabled(requiredExpansion) && (stageOrder == 2 || stageOrder == 3 || stageOrder == 4))
		{
			return true;
		}
		return false;
	}

	public override void OnExit()
	{
		base.OnExit();
	}
}
