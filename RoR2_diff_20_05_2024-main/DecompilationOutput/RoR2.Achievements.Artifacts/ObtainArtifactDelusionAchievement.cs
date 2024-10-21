namespace RoR2.Achievements.Artifacts;

[RegisterAchievement("ObtainArtifactDelusion", "Artifacts.Delusion", null, 2u, null)]
public class ObtainArtifactDelusionAchievement : BaseObtainArtifactAchievement
{
	protected override ArtifactDef artifactDef => CU8Content.Artifacts.Delusion;
}
