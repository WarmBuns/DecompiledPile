using System;
using System.Collections.Generic;
using RoR2.ExpansionManagement;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class BazaarController : MonoBehaviour
{
	[Serializable]
	public struct SeerSceneOverride
	{
		[SerializeField]
		public SceneDef sceneDef;

		[SerializeField]
		public ExpansionDef requiredExpasion;

		[Range(0f, 1f)]
		[SerializeField]
		public float overrideChance;

		[SerializeField]
		public int minStagesCleared;

		[SerializeField]
		public string bannedEventFlag;
	}

	public GameObject shopkeeper;

	public TextMeshPro shopkeeperChat;

	public float shopkeeperTrackDistance = 250f;

	public float shopkeeperTrackAngle = 120f;

	[Tooltip("Any PurchaseInteraction objects here will have their activation state set based on whether or not the specified unlockable is available.")]
	public PurchaseInteraction[] unlockableTerminals;

	public SeerStationController[] seerStations;

	public SeerSceneOverride[] seerSceneOverrides;

	private InputBankTest shopkeeperInputBank;

	private CharacterBody shopkeeperTargetBody;

	private Xoroshiro128Plus rng;

	public static BazaarController instance { get; private set; }

	private void Awake()
	{
		instance = SingletonHelper.Assign(instance, this);
	}

	private void Start()
	{
		if (NetworkServer.active)
		{
			OnStartServer();
		}
	}

	private void OnStartServer()
	{
		rng = new Xoroshiro128Plus(Run.instance.stageRng.nextUlong);
		SetUpSeerStations();
	}

	private void SetUpSeerStations()
	{
		SceneDef nextStageScene = Run.instance.nextStageScene;
		List<SceneDef> list = new List<SceneDef>();
		if (nextStageScene != null)
		{
			int stageOrder = nextStageScene.stageOrder;
			foreach (SceneDef allSceneDef in SceneCatalog.allSceneDefs)
			{
				if (allSceneDef.stageOrder == stageOrder && (allSceneDef.requiredExpansion == null || Run.instance.IsExpansionEnabled(allSceneDef.requiredExpansion)) && IsUnlockedBeforeLooping(allSceneDef))
				{
					list.Add(allSceneDef);
				}
			}
		}
		WeightedSelection<SceneDef> weightedSelection = new WeightedSelection<SceneDef>();
		float num = 0f;
		SeerSceneOverride[] array = seerSceneOverrides;
		for (int i = 0; i < array.Length; i++)
		{
			SeerSceneOverride seerSceneOverride = array[i];
			bool num2 = Run.instance.stageClearCount >= seerSceneOverride.minStagesCleared;
			bool flag = seerSceneOverride.requiredExpasion == null || Run.instance.IsExpansionEnabled(seerSceneOverride.requiredExpasion);
			bool flag2 = string.IsNullOrEmpty(seerSceneOverride.bannedEventFlag) || !Run.instance.GetEventFlag(seerSceneOverride.bannedEventFlag);
			if (num2 && flag && flag2)
			{
				weightedSelection.AddChoice(seerSceneOverride.sceneDef, seerSceneOverride.overrideChance);
				num += seerSceneOverride.overrideChance;
			}
		}
		SeerStationController[] array2 = seerStations;
		foreach (SeerStationController seerStationController in array2)
		{
			if (list.Count == 0)
			{
				seerStationController.GetComponent<PurchaseInteraction>().SetAvailable(newAvailable: false);
				continue;
			}
			Util.ShuffleList(list, rng);
			int index = list.Count - 1;
			SceneDef sceneDef = list[index];
			if (rng.nextNormalizedFloat < num)
			{
				sceneDef = weightedSelection.Evaluate(rng.nextNormalizedFloat);
			}
			if (IsSceneAlreadySelected(sceneDef))
			{
				sceneDef = list[index];
			}
			list.RemoveAt(index);
			seerStationController.SetTargetScene(sceneDef);
		}
	}

	private bool IsUnlockedBeforeLooping(SceneDef sceneDef)
	{
		if (Run.instance.stageClearCount < 4 && sceneDef.isLockedBeforeLooping)
		{
			return false;
		}
		return true;
	}

	private bool IsSceneAlreadySelected(SceneDef sceneDef)
	{
		for (int i = 0; i < seerStations.Length; i++)
		{
			if (sceneDef.sceneDefIndex == (SceneIndex)seerStations[i].ReturnTargetSceneDefIndex())
			{
				return true;
			}
		}
		return false;
	}

	private void OnDestroy()
	{
		instance = SingletonHelper.Unassign(instance, this);
	}

	public void CommentOnAnnoy()
	{
		int maxExclusive = 6;
		if (Util.CheckRoll(20f))
		{
			Chat.SendBroadcastChat(new Chat.NpcChatMessage
			{
				sender = shopkeeper,
				baseToken = "NEWT_ANNOY_" + UnityEngine.Random.Range(1, maxExclusive)
			});
		}
	}

	public void CommentOnEnter()
	{
	}

	public void CommentOnLeaving()
	{
	}

	public void CommentOnLunarPurchase()
	{
		int maxExclusive = 8;
		if (Util.CheckRoll(20f))
		{
			Chat.SendBroadcastChat(new Chat.NpcChatMessage
			{
				sender = shopkeeper,
				baseToken = "NEWT_LUNAR_PURCHASE_" + UnityEngine.Random.Range(1, maxExclusive)
			});
		}
	}

	public void CommentOnBlueprintPurchase()
	{
	}

	public void CommentOnDronePurchase()
	{
	}

	public void CommentOnUpgrade()
	{
		int maxExclusive = 3;
		if (Util.CheckRoll(100f))
		{
			Chat.SendBroadcastChat(new Chat.NpcChatMessage
			{
				sender = shopkeeper,
				baseToken = "NEWT_UPGRADE_" + UnityEngine.Random.Range(1, maxExclusive)
			});
		}
	}

	private void Update()
	{
		if (!shopkeeper)
		{
			return;
		}
		if (!shopkeeperInputBank)
		{
			shopkeeperInputBank = shopkeeper.GetComponent<InputBankTest>();
			return;
		}
		Ray aimRay = new Ray(shopkeeperInputBank.aimOrigin, shopkeeper.transform.forward);
		shopkeeperTargetBody = Util.GetEnemyEasyTarget(shopkeeper.GetComponent<CharacterBody>(), aimRay, shopkeeperTrackDistance, shopkeeperTrackAngle);
		if ((bool)shopkeeperTargetBody)
		{
			Vector3 direction = shopkeeperTargetBody.mainHurtBox.transform.position - aimRay.origin;
			aimRay.direction = direction;
		}
		shopkeeperInputBank.aimDirection = aimRay.direction;
	}
}
