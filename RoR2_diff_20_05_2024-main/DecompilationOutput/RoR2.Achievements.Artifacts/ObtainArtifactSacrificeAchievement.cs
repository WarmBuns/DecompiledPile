namespace RoR2.Achievements.Artifacts;

[RegisterAchievement("ObtainArtifactSacrifice", "Artifacts.Sacrifice", null, 3u, null)]
public class ObtainArtifactSacrificeAchievement : BaseObtainArtifactAchievement
{
	protected override ArtifactDef artifactDef => RoR2Content.Artifacts.sacrificeArtifactDef;
}
