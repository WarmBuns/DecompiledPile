namespace RoR2.Achievements.Artifacts;

[RegisterAchievement("ObtainArtifactBomb", "Artifacts.Bomb", null, 3u, null)]
public class ObtainArtifactBombAchievement : BaseObtainArtifactAchievement
{
	protected override ArtifactDef artifactDef => RoR2Content.Artifacts.bombArtifactDef;
}
