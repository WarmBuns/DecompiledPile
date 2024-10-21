using System.Collections.Generic;
using Assets.RoR2.Scripts.Platform;
using HG;

namespace RoR2.Achievements;

[RegisterAchievement("CaptainVisitSeveralStages", "Skills.Captain.CaptainSupplyDropEquipmentRestock", "CompleteMainEnding", 3u, null)]
public class CaptainVisitSeveralStagesAchievement : BaseAchievement
{
	private static readonly int requirement = 10;

	private List<SceneDef> visitedScenes;

	protected override BodyIndex LookUpRequiredBodyIndex()
	{
		return BodyCatalog.FindBodyIndex("CaptainBody");
	}

	protected override void OnBodyRequirementMet()
	{
		base.OnBodyRequirementMet();
		visitedScenes = CollectionPool<SceneDef, List<SceneDef>>.RentCollection();
		SceneCatalog.onMostRecentSceneDefChanged += HandleMostRecentSceneDefChanged;
	}

	protected override void OnBodyRequirementBroken()
	{
		SceneCatalog.onMostRecentSceneDefChanged -= HandleMostRecentSceneDefChanged;
		visitedScenes = CollectionPool<SceneDef, List<SceneDef>>.ReturnCollection(visitedScenes);
		base.OnBodyRequirementBroken();
	}

	public override void TryToCompleteActivity()
	{
		if (base.localUser.id == LocalUserManager.GetFirstLocalUser().id && shouldGrant)
		{
			BaseActivitySelector baseActivitySelector = new BaseActivitySelector();
			baseActivitySelector.activityAchievementID = "CaptainVisitSeveralStages";
			PlatformSystems.activityManager.TryToCompleteActivity(baseActivitySelector);
		}
	}

	private void HandleMostRecentSceneDefChanged(SceneDef newSceneDef)
	{
		if (!visitedScenes.Contains(newSceneDef))
		{
			visitedScenes.Add(newSceneDef);
		}
		if (visitedScenes.Count >= requirement)
		{
			Grant();
			TryToCompleteActivity();
		}
	}
}
