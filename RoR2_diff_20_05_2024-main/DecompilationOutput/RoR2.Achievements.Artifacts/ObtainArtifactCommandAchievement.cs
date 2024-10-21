namespace RoR2.Achievements.Artifacts;

[RegisterAchievement("ObtainArtifactCommand", "Artifacts.Command", null, 3u, null)]
public class ObtainArtifactCommandAchievement : BaseObtainArtifactAchievement
{
	protected override ArtifactDef artifactDef => RoR2Content.Artifacts.commandArtifactDef;
}
