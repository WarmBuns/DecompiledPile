using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Artifacts;

public static class DevotionArtifactManager
{
	private static ArtifactDef myArtifact;

	private static GameObject devotionArtifactManagerPrefab;

	public static DirectorCard lumerianEgg;

	[SystemInitializer(new Type[] { typeof(ArtifactCatalog) })]
	private static void Init()
	{
		LegacyResourcesAPI.LoadAsyncCallback("Prefabs/DevotionArtifactInfo", delegate(GameObject operationResult)
		{
			devotionArtifactManagerPrefab = operationResult;
		});
		myArtifact = CU8Content.Artifacts.Devotion;
		RunArtifactManager.onArtifactEnabledGlobal += OnArtifactEnabled;
		RunArtifactManager.onArtifactDisabledGlobal += OnArtifactDisabled;
	}

	private static void OnArtifactEnabled(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
	{
		if (NetworkServer.active && !(artifactDef != myArtifact))
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

	private static void CalcCardWeight(DirectorCard card, ref float weight)
	{
		InteractableSpawnCard interactableSpawnCard = card.spawnCard as InteractableSpawnCard;
		if (interactableSpawnCard != null)
		{
			weight *= interactableSpawnCard.weightScalarWhenSacrificeArtifactEnabled;
		}
	}

	private static void OnGenerateInteractableCardSelection(SceneDirector sceneDirector, DirectorCardCategorySelection dccs)
	{
	}
}
