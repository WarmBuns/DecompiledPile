namespace RoR2.Achievements.Artifacts;

[RegisterAchievement("ObtainArtifactSwarms", "Artifacts.Swarms", null, 3u, null)]
public class ObtainArtifactSwarmsAchievement : BaseObtainArtifactAchievement
{
	protected override ArtifactDef artifactDef => RoR2Content.Artifacts.swarmsArtifactDef;
}
