namespace RoR2.Achievements.Artifacts;

[RegisterAchievement("ObtainArtifactWispOnDeath", "Artifacts.WispOnDeath", null, 3u, null)]
public class ObtainArtifactWispOnDeathAchievement : BaseObtainArtifactAchievement
{
	protected override ArtifactDef artifactDef => RoR2Content.Artifacts.wispOnDeath;
}
