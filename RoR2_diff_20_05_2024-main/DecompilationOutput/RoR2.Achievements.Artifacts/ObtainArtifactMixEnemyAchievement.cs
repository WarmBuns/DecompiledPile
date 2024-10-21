namespace RoR2.Achievements.Artifacts;

[RegisterAchievement("ObtainArtifactMixEnemy", "Artifacts.MixEnemy", null, 3u, null)]
public class ObtainArtifactMixEnemyAchievement : BaseObtainArtifactAchievement
{
	protected override ArtifactDef artifactDef => RoR2Content.Artifacts.mixEnemyArtifactDef;
}
