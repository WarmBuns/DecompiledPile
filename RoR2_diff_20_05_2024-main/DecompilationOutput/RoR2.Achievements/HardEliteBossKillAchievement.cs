using System.Collections.Generic;
using UnityEngine;

namespace RoR2.Achievements;

[RegisterAchievement("HardEliteBossKill", "Items.KillEliteFrenzy", null, 3u, typeof(EliteBossKillServerAchievement))]
internal class HardEliteBossKillAchievement : BaseAchievement
{
	private class EliteBossKillServerAchievement : BaseServerAchievement
	{
		private static readonly List<EliteBossKillServerAchievement> instancesList = new List<EliteBossKillServerAchievement>();

		public override void OnInstall()
		{
			base.OnInstall();
			instancesList.Add(this);
			if (instancesList.Count == 1)
			{
				GlobalEventManager.onCharacterDeathGlobal += OnCharacterDeath;
			}
		}

		public override void OnUninstall()
		{
			if (instancesList.Count == 1)
			{
				GlobalEventManager.onCharacterDeathGlobal -= OnCharacterDeath;
			}
			instancesList.Remove(this);
			base.OnUninstall();
		}

		private static void OnCharacterDeath(DamageReport damageReport)
		{
			if (!damageReport.victim)
			{
				return;
			}
			CharacterBody body = damageReport.victim.body;
			if (!body || !body.isChampion || !body.isElite)
			{
				return;
			}
			foreach (EliteBossKillServerAchievement instances in instancesList)
			{
				GameObject masterObject = instances.serverAchievementTracker.networkUser.masterObject;
				if (!masterObject)
				{
					continue;
				}
				CharacterMaster component = masterObject.GetComponent<CharacterMaster>();
				if ((bool)component)
				{
					CharacterBody body2 = component.GetBody();
					if ((bool)body2 && (bool)body2.healthComponent && body2.healthComponent.alive)
					{
						instances.Grant();
					}
				}
			}
		}
	}

	public override void OnInstall()
	{
		base.OnInstall();
		NetworkUser.OnPostNetworkUserStart += OnPostNetworkUserStart;
		Run.onRunStartGlobal += OnRunStart;
	}

	public override void OnUninstall()
	{
		NetworkUser.OnPostNetworkUserStart -= OnPostNetworkUserStart;
		Run.onRunStartGlobal -= OnRunStart;
		base.OnUninstall();
	}

	private void UpdateTracking()
	{
		SetServerTracked((bool)Run.instance && (DifficultyCatalog.GetDifficultyDef(Run.instance.selectedDifficulty)?.countsAsHardMode ?? false));
	}

	private void OnPostNetworkUserStart(NetworkUser networkUser)
	{
		UpdateTracking();
	}

	private void OnRunStart(Run run)
	{
		UpdateTracking();
	}
}
