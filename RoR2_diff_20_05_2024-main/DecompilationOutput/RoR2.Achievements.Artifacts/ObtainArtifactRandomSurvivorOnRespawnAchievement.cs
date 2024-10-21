namespace RoR2.Achievements.Artifacts;

[RegisterAchievement("ObtainArtifactRandomSurvivorOnRespawn", "Artifacts.RandomSurvivorOnRespawn", null, 3u, null)]
public class ObtainArtifactRandomSurvivorOnRespawnAchievement : BaseObtainArtifactAchievement
{
	protected override ArtifactDef artifactDef => RoR2Content.Artifacts.randomSurvivorOnRespawnArtifactDef;
}
