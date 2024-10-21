using HG;
using JetBrains.Annotations;
using UnityEngine;

namespace RoR2;

public static class TeamCatalog
{
	private static TeamDef[] teamDefs;

	static TeamCatalog()
	{
		teamDefs = new TeamDef[5];
		Register(TeamIndex.Neutral, new TeamDef
		{
			nameToken = "TEAM_NEUTRAL_NAME",
			softCharacterLimit = 40,
			friendlyFireScaling = 1f
		});
		Register(TeamIndex.Player, new TeamDef
		{
			nameToken = "TEAM_PLAYER_NAME",
			softCharacterLimit = 20,
			friendlyFireScaling = 0.5f,
			levelUpEffect = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/LevelUpEffect"),
			levelUpSound = "Play_UI_levelUp_player"
		});
		Register(TeamIndex.Monster, new TeamDef
		{
			nameToken = "TEAM_MONSTER_NAME",
			softCharacterLimit = 40,
			friendlyFireScaling = 2f,
			levelUpEffect = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/LevelUpEffectEnemy"),
			levelUpSound = "Play_UI_levelUp_enemy"
		});
		Register(TeamIndex.Lunar, new TeamDef
		{
			nameToken = "TEAM_LUNAR_NAME",
			softCharacterLimit = 40,
			friendlyFireScaling = 2f,
			levelUpEffect = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/LevelUpEffectEnemy"),
			levelUpSound = "Play_UI_levelUp_enemy"
		});
		Register(TeamIndex.Void, new TeamDef
		{
			nameToken = "TEAM_VOID_NAME",
			softCharacterLimit = 40,
			friendlyFireScaling = 2f,
			levelUpEffect = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/LevelUpEffectEnemy"),
			levelUpSound = "Play_UI_levelUp_enemy"
		});
	}

	private static void Register(TeamIndex teamIndex, TeamDef teamDef)
	{
		teamDefs[(int)teamIndex] = teamDef;
	}

	[CanBeNull]
	public static TeamDef GetTeamDef(TeamIndex teamIndex)
	{
		return ArrayUtils.GetSafe(teamDefs, (int)teamIndex);
	}
}
