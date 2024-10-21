namespace RoR2;

public static class FriendlyFireManager
{
	public enum FriendlyFireMode
	{
		Off,
		FriendlyFire,
		FreeForAll
	}

	public static FriendlyFireMode friendlyFireMode = FriendlyFireMode.Off;

	public static float friendlyFireDamageScale { get; private set; } = 0.5f;

	public static bool ShouldSplashHitProceed(HealthComponent victim, TeamIndex attackerTeamIndex)
	{
		if (victim.body.teamComponent.teamIndex == attackerTeamIndex && friendlyFireMode == FriendlyFireMode.Off)
		{
			return attackerTeamIndex == TeamIndex.None;
		}
		return true;
	}

	public static bool ShouldDirectHitProceed(HealthComponent victim, TeamIndex attackerTeamIndex)
	{
		if (victim.body.teamComponent.teamIndex == attackerTeamIndex && friendlyFireMode == FriendlyFireMode.Off)
		{
			return attackerTeamIndex == TeamIndex.None;
		}
		return true;
	}

	public static bool ShouldSeekingProceed(HealthComponent victim, TeamIndex attackerTeamIndex)
	{
		if (victim.body.teamComponent.teamIndex == attackerTeamIndex && friendlyFireMode != FriendlyFireMode.FreeForAll)
		{
			return attackerTeamIndex == TeamIndex.None;
		}
		return true;
	}
}
