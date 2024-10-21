namespace RoR2.Achievements.Artifacts;

[RegisterAchievement("ObtainArtifactMonsterTeamGainsItems", "Artifacts.MonsterTeamGainsItems", null, 3u, null)]
public class ObtainArtifactMonsterTeamGainsItemsAchievement : BaseObtainArtifactAchievement
{
	protected override ArtifactDef artifactDef => RoR2Content.Artifacts.monsterTeamGainsItemsArtifactDef;
}
