namespace RoR2.Achievements.Artifacts;

[RegisterAchievement("ObtainArtifactRebirth", "Artifacts.Rebirth", null, 2u, null)]
public class ObtainArtifactRebirthAchievement : BaseObtainArtifactAchievement
{
	protected override ArtifactDef artifactDef => DLC2Content.Artifacts.Rebirth;
}
