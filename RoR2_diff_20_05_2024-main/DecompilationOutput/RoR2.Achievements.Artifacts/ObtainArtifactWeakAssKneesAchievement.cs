namespace RoR2.Achievements.Artifacts;

[RegisterAchievement("ObtainArtifactWeakAssKnees", "Artifacts.WeakAssKnees", null, 3u, null)]
public class ObtainArtifactWeakAssKneesAchievement : BaseObtainArtifactAchievement
{
	protected override ArtifactDef artifactDef => RoR2Content.Artifacts.weakAssKneesArtifactDef;
}
