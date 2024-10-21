namespace RoR2.Achievements.Artifacts;

[RegisterAchievement("ObtainArtifactDevotion", "Artifacts.Devotion", null, 2u, null)]
public class ObtainArtifactDevotionAchievement : BaseObtainArtifactAchievement
{
	protected override ArtifactDef artifactDef => CU8Content.Artifacts.Devotion;
}
