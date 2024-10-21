namespace RoR2.Achievements.Artifacts;

[RegisterAchievement("ObtainArtifactEnigma", "Artifacts.Enigma", null, 3u, null)]
public class ObtainArtifactEnigmaAchievement : BaseObtainArtifactAchievement
{
	protected override ArtifactDef artifactDef => RoR2Content.Artifacts.enigmaArtifactDef;
}
