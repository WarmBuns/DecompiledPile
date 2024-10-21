namespace RoR2.Achievements.Artifacts;

[RegisterAchievement("ObtainArtifactEliteOnly", "Artifacts.EliteOnly", null, 3u, null)]
public class ObtainArtifactAchievement : BaseObtainArtifactAchievement
{
	protected override ArtifactDef artifactDef => RoR2Content.Artifacts.eliteOnlyArtifactDef;
}
