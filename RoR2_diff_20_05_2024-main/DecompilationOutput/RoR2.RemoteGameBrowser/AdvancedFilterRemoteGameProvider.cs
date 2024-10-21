using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace RoR2.RemoteGameBrowser;

public class AdvancedFilterRemoteGameProvider : BaseAsyncRemoteGameProvider
{
	public new struct SearchFilters : IEquatable<SearchFilters>
	{
		public bool allowPassword;

		public int requiredSlots;

		public int maxPing;

		public int minMaxPlayers;

		public int maxMaxPlayers;

		public bool allowDifficultyEasy;

		public bool allowDifficultyNormal;

		public bool allowDifficultyHard;

		public bool showGamesWithRuleVoting;

		public bool showGamesWithoutRuleVoting;

		public bool allowInProgressGames;

		public bool Equals(SearchFilters other)
		{
			if (allowPassword == other.allowPassword && requiredSlots == other.requiredSlots && maxPing == other.maxPing && minMaxPlayers == other.minMaxPlayers && maxMaxPlayers == other.maxMaxPlayers && allowDifficultyEasy == other.allowDifficultyEasy && allowDifficultyNormal == other.allowDifficultyNormal && allowDifficultyHard == other.allowDifficultyHard && showGamesWithRuleVoting == other.showGamesWithRuleVoting && showGamesWithoutRuleVoting == other.showGamesWithoutRuleVoting)
			{
				return allowInProgressGames == other.allowInProgressGames;
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			if (obj is SearchFilters other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (((((((((((((((((((allowPassword.GetHashCode() * 397) ^ requiredSlots) * 397) ^ maxPing) * 397) ^ minMaxPlayers) * 397) ^ maxMaxPlayers) * 397) ^ allowDifficultyEasy.GetHashCode()) * 397) ^ allowDifficultyNormal.GetHashCode()) * 397) ^ allowDifficultyHard.GetHashCode()) * 397) ^ showGamesWithRuleVoting.GetHashCode()) * 397) ^ showGamesWithoutRuleVoting.GetHashCode()) * 397) ^ allowInProgressGames.GetHashCode();
		}
	}

	private IRemoteGameProvider src;

	private new SearchFilters searchFilters;

	public new SearchFilters GetSearchFilters()
	{
		return searchFilters;
	}

	public void SetSearchFilters(SearchFilters newSearchFilters)
	{
		if (!searchFilters.Equals(newSearchFilters))
		{
			searchFilters = newSearchFilters;
			SetDirty();
		}
	}

	protected override Task<RemoteGameInfo[]> CreateTask(CancellationToken cancellationToken)
	{
		RemoteGameInfo[] srcInfo = src.GetKnownGames();
		SearchFilters capturedSearchFilters = searchFilters;
		return new Task<RemoteGameInfo[]>(delegate
		{
			SearchFilters localSearchFilters = capturedSearchFilters;
			bool[] array = new bool[srcInfo.Length];
			int num = 0;
			for (int i = 0; i < srcInfo.Length; i++)
			{
				if (PassesFilters(in srcInfo[i]))
				{
					array[i] = true;
					num++;
				}
			}
			RemoteGameInfo[] array2 = new RemoteGameInfo[num];
			int j = 0;
			int k = 0;
			for (; j < array2.Length; j++)
			{
				for (; !array[k]; k++)
				{
				}
				array2[j] = srcInfo[k++];
			}
			return array2;
			bool PassesFilters(in RemoteGameInfo remoteGameInfo)
			{
				if (!localSearchFilters.allowPassword && (remoteGameInfo.hasPassword ?? false))
				{
					return false;
				}
				if (localSearchFilters.requiredSlots > 0 && remoteGameInfo.availableSlots < localSearchFilters.requiredSlots)
				{
					return false;
				}
				if (localSearchFilters.maxPing > 0 && remoteGameInfo.ping.HasValue && remoteGameInfo.ping.Value > localSearchFilters.maxPing)
				{
					return false;
				}
				if (localSearchFilters.minMaxPlayers > remoteGameInfo.lesserMaxPlayers)
				{
					return false;
				}
				if (localSearchFilters.maxMaxPlayers < remoteGameInfo.greaterMaxPlayers)
				{
					return false;
				}
				DifficultyDef difficultyDef = DifficultyCatalog.GetDifficultyDef(remoteGameInfo.currentDifficultyIndex ?? DifficultyIndex.Invalid);
				if (difficultyDef == null)
				{
					return false;
				}
				if (!localSearchFilters.allowDifficultyEasy && remoteGameInfo.currentDifficultyIndex == DifficultyIndex.Easy)
				{
					return false;
				}
				if (!localSearchFilters.allowDifficultyNormal && remoteGameInfo.currentDifficultyIndex == DifficultyIndex.Normal)
				{
					return false;
				}
				if (!localSearchFilters.allowDifficultyHard && difficultyDef.countsAsHardMode)
				{
					return false;
				}
				if (!localSearchFilters.showGamesWithRuleVoting && remoteGameInfo.HasTag("rv1"))
				{
					return false;
				}
				if (!localSearchFilters.showGamesWithoutRuleVoting && remoteGameInfo.HasTag("rv0"))
				{
					return false;
				}
				if (!localSearchFilters.allowInProgressGames && !string.IsNullOrEmpty(remoteGameInfo.currentSceneName) && !(remoteGameInfo.currentSceneName == "lobby"))
				{
					return false;
				}
				return true;
			}
		});
	}

	public override bool RequestRefresh()
	{
		return src.RequestRefresh();
	}

	public AdvancedFilterRemoteGameProvider([NotNull] IRemoteGameProvider src)
	{
		this.src = src;
		this.src.onNewInfoAvailable += base.SetDirty;
	}

	public override void Dispose()
	{
		src.onNewInfoAvailable -= base.SetDirty;
		base.Dispose();
	}

	public override bool IsBusy()
	{
		if (!base.IsBusy())
		{
			return src?.IsBusy() ?? false;
		}
		return true;
	}
}
