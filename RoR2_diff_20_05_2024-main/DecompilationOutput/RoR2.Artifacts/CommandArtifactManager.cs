using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Artifacts;

public static class CommandArtifactManager
{
	public static GameObject commandCubePrefab;

	public static bool IsCommandArtifactEnabled => RunArtifactManager.instance.IsArtifactEnabled(myArtifact);

	private static ArtifactDef myArtifact => RoR2Content.Artifacts.commandArtifactDef;

	[SystemInitializer(new Type[] { typeof(ArtifactCatalog) })]
	private static void Init()
	{
		LegacyResourcesAPI.LoadAsyncCallback("Prefabs/NetworkedObjects/CommandCube", delegate(GameObject operationResult)
		{
			commandCubePrefab = operationResult;
		});
		RunArtifactManager.onArtifactEnabledGlobal += OnArtifactEnabled;
		RunArtifactManager.onArtifactDisabledGlobal += OnArtifactDisabled;
	}

	private static void OnArtifactEnabled(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
	{
		if (!(artifactDef != myArtifact) && NetworkServer.active)
		{
			SceneDirector.onGenerateInteractableCardSelection += OnGenerateInteractableCardSelection;
		}
	}

	private static void OnArtifactDisabled(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
	{
		if (!(artifactDef != myArtifact))
		{
			SceneDirector.onGenerateInteractableCardSelection -= OnGenerateInteractableCardSelection;
		}
	}

	private static void OnGenerateInteractableCardSelection(SceneDirector sceneDirector, DirectorCardCategorySelection dccs)
	{
		dccs.RemoveCardsThatFailFilter(DoesNotOfferChoice);
		static bool DoesNotOfferChoice(DirectorCard card)
		{
			return !OffersChoice(card);
		}
		static bool OffersChoice(DirectorCard card)
		{
			GameObject prefab = card.spawnCard.prefab;
			if (!prefab.GetComponent<ShopTerminalBehavior>() && !prefab.GetComponent<MultiShopController>())
			{
				return prefab.GetComponent<ScrapperController>();
			}
			return true;
		}
	}
}
