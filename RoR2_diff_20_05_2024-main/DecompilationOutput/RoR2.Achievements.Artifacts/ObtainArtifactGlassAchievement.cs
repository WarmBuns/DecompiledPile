namespace RoR2.Achievements.Artifacts;

[RegisterAchievement("ObtainArtifactGlass", "Artifacts.Glass", null, 3u, null)]
public class ObtainArtifactGlassAchievement : BaseObtainArtifactAchievement
{
	protected override ArtifactDef artifactDef => RoR2Content.Artifacts.glassArtifactDef;
}
