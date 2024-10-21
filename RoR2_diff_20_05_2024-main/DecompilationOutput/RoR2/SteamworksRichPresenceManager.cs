using System;
using System.Collections.Generic;
using Facepunch.Steamworks;
using JetBrains.Annotations;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace RoR2;

internal static class SteamworksRichPresenceManager
{
	private abstract class BaseRichPresenceField
	{
		private static readonly Queue<BaseRichPresenceField> dirtyFields = new Queue<BaseRichPresenceField>();

		private bool isDirty;

		[CanBeNull]
		private string currentValue;

		private bool installed;

		protected abstract string key { get; }

		public static void ProcessDirtyFields()
		{
			while (dirtyFields.Count > 0)
			{
				dirtyFields.Dequeue().UpdateIfNecessary();
			}
		}

		[CanBeNull]
		protected abstract string RebuildValue();

		protected virtual void OnChanged()
		{
		}

		public void SetDirty()
		{
			if (!isDirty)
			{
				isDirty = true;
				dirtyFields.Enqueue(this);
			}
		}

		private void UpdateIfNecessary()
		{
			if (installed)
			{
				isDirty = false;
				string text = RebuildValue();
				if (text != currentValue)
				{
					currentValue = text;
					SetKeyValue(key, currentValue);
					OnChanged();
				}
			}
		}

		protected virtual void OnInstall()
		{
		}

		protected virtual void OnUninstall()
		{
		}

		public void Install()
		{
			if (!installed)
			{
				OnInstall();
				SetDirty();
				installed = true;
			}
		}

		public void Uninstall()
		{
			if (installed)
			{
				OnUninstall();
				installed = false;
				SetKeyValue(key, null);
			}
		}

		protected void SetDirtyableValue<T>(ref T field, T value) where T : struct, IEquatable<T>
		{
			if (!field.Equals(value))
			{
				field = value;
				SetDirty();
			}
		}

		protected void SetDirtyableReference<T>(ref T field, T value) where T : class
		{
			if (field != value)
			{
				field = value;
				SetDirty();
			}
		}
	}

	private sealed class DifficultyField : BaseRichPresenceField
	{
		protected override string key => "difficulty";

		protected override string RebuildValue()
		{
			if (!Run.instance)
			{
				return null;
			}
			if (DifficultyCatalog.GetDifficultyDef(Run.instance.selectedDifficulty).countsAsHardMode)
			{
				return "Hard";
			}
			return Run.instance.selectedDifficulty switch
			{
				DifficultyIndex.Easy => "Easy", 
				DifficultyIndex.Normal => "Normal", 
				DifficultyIndex.Hard => "Hard", 
				_ => null, 
			};
		}

		private void SetDirty(Run run)
		{
			SetDirty();
		}

		protected override void OnInstall()
		{
			base.OnInstall();
			Run.onRunStartGlobal += SetDirty;
			Run.onRunDestroyGlobal += SetDirty;
		}

		protected override void OnUninstall()
		{
			Run.onRunStartGlobal -= SetDirty;
			Run.onRunDestroyGlobal -= SetDirty;
			base.OnUninstall();
		}
	}

	private sealed class GameModeField : BaseRichPresenceField
	{
		protected override string key => "gamemode";

		protected override string RebuildValue()
		{
			if (!Run.instance)
			{
				return null;
			}
			return GameModeCatalog.FindGameModePrefabComponent(Run.instance.name)?.name;
		}

		private void SetDirty(Run run)
		{
			SetDirty();
		}

		protected override void OnInstall()
		{
			base.OnInstall();
			Run.onRunStartGlobal += SetDirty;
			Run.onRunDestroyGlobal += SetDirty;
		}

		protected override void OnUninstall()
		{
			Run.onRunStartGlobal -= SetDirty;
			Run.onRunDestroyGlobal -= SetDirty;
			base.OnUninstall();
		}
	}

	private sealed class ParticipationField : BaseRichPresenceField
	{
		private enum ParticipationType
		{
			None,
			Alive,
			Dead,
			Spectator
		}

		private ParticipationType participationType;

		private LocalUser trackedUser;

		private CharacterMaster currentMaster;

		protected override string key => "participation_type";

		private void SetParticipationType(ParticipationType newParticipationType)
		{
			if (participationType != newParticipationType)
			{
				participationType = newParticipationType;
				SetDirty();
			}
		}

		protected override string RebuildValue()
		{
			return participationType switch
			{
				ParticipationType.Alive => "Alive", 
				ParticipationType.Dead => "Dead", 
				ParticipationType.Spectator => "Spectator", 
				_ => null, 
			};
		}

		protected override void OnInstall()
		{
			base.OnInstall();
			LocalUserManager.onUserSignIn += OnLocalUserDiscovered;
			LocalUserManager.onUserSignOut += OnLocalUserLost;
			Run.onRunStartGlobal += OnRunStart;
			Run.onRunDestroyGlobal += OnRunDestroy;
		}

		protected override void OnUninstall()
		{
			LocalUserManager.onUserSignIn -= OnLocalUserDiscovered;
			LocalUserManager.onUserSignOut -= OnLocalUserLost;
			Run.onRunStartGlobal -= OnRunStart;
			Run.onRunDestroyGlobal -= OnRunDestroy;
			SetCurrentMaster(null);
		}

		private void SetTrackedUser(LocalUser newTrackedUser)
		{
			if (trackedUser != null)
			{
				trackedUser.onMasterChanged -= OnMasterChanged;
			}
			trackedUser = newTrackedUser;
			if (trackedUser != null)
			{
				trackedUser.onMasterChanged += OnMasterChanged;
			}
		}

		private void OnLocalUserDiscovered(LocalUser localUser)
		{
			if (trackedUser == null)
			{
				SetTrackedUser(localUser);
			}
		}

		private void OnLocalUserLost(LocalUser localUser)
		{
			if (trackedUser == localUser)
			{
				SetTrackedUser(null);
			}
		}

		private void OnRunStart(Run run)
		{
			if (trackedUser != null && !trackedUser.cachedMasterObject)
			{
				SetParticipationType(ParticipationType.Spectator);
			}
		}

		private void OnRunDestroy(Run run)
		{
			if (trackedUser != null)
			{
				SetParticipationType(ParticipationType.None);
			}
		}

		private void OnMasterChanged()
		{
			PlayerCharacterMasterController cachedMasterController = trackedUser.cachedMasterController;
			SetCurrentMaster(cachedMasterController ? cachedMasterController.master : null);
		}

		private void SetCurrentMaster(CharacterMaster newMaster)
		{
			if ((object)currentMaster != null)
			{
				currentMaster.onBodyDeath.RemoveListener(OnBodyDeath);
				currentMaster.onBodyStart -= OnBodyStart;
			}
			currentMaster = newMaster;
			if ((object)currentMaster != null)
			{
				currentMaster.onBodyDeath.AddListener(OnBodyDeath);
				currentMaster.onBodyStart += OnBodyStart;
			}
		}

		private void OnBodyDeath()
		{
			SetParticipationType(ParticipationType.Dead);
		}

		private void OnBodyStart(CharacterBody body)
		{
			SetParticipationType(ParticipationType.Alive);
		}
	}

	private sealed class MinutesField : BaseRichPresenceField
	{
		private uint minutes;

		protected override string key => "minutes";

		protected override string RebuildValue()
		{
			return TextSerialization.ToStringInvariant(minutes);
		}

		private void FixedUpdate()
		{
			uint value = 0u;
			if ((bool)Run.instance)
			{
				value = (uint)Mathf.FloorToInt(Run.instance.GetRunStopwatch() / 60f);
			}
			SetDirtyableValue(ref minutes, value);
		}

		protected override void OnInstall()
		{
			base.OnInstall();
			RoR2Application.onFixedUpdate += FixedUpdate;
		}

		protected override void OnUninstall()
		{
			RoR2Application.onFixedUpdate -= FixedUpdate;
			base.OnUninstall();
		}
	}

	private sealed class SteamPlayerGroupField : BaseRichPresenceField
	{
		private PlatformID lobbyId = PlatformID.nil;

		private PlatformID hostId = PlatformID.nil;

		private PlatformID groupId = PlatformID.nil;

		private SteamPlayerGroupSizeField groupSizeField;

		protected override string key => "steam_player_group";

		private void SetLobbyId(PlatformID newLobbyId)
		{
			if (lobbyId != newLobbyId)
			{
				lobbyId = newLobbyId;
				UpdateGroupID();
			}
		}

		private void SetHostId(PlatformID newHostId)
		{
			if (hostId != newHostId)
			{
				hostId = newHostId;
				UpdateGroupID();
			}
		}

		private void SetGroupId(PlatformID newGroupId)
		{
			if (groupId != newGroupId)
			{
				groupId = newGroupId;
				SetDirty();
			}
		}

		private void UpdateGroupID()
		{
			if (hostId != PlatformID.nil)
			{
				SetGroupId(hostId);
				if (!(groupSizeField is SteamPlayerGroupSizeFieldGame))
				{
					groupSizeField?.Uninstall();
					groupSizeField = new SteamPlayerGroupSizeFieldGame();
					groupSizeField.Install();
				}
			}
			else
			{
				SetGroupId(lobbyId);
				if (!(groupSizeField is SteamPlayerGroupSizeFieldLobby))
				{
					groupSizeField?.Uninstall();
					groupSizeField = new SteamPlayerGroupSizeFieldLobby();
					groupSizeField.Install();
				}
			}
		}

		protected override void OnInstall()
		{
			base.OnInstall();
			NetworkManagerSystem.onClientConnectGlobal += OnClientConnectGlobal;
			NetworkManagerSystem.onClientDisconnectGlobal += OnClientDisconnectGlobal;
			NetworkManagerSystem.onStartServerGlobal += OnStartServerGlobal;
			NetworkManagerSystem.onStopServerGlobal += OnStopServerGlobal;
			LobbyManager lobbyManager = PlatformSystems.lobbyManager;
			lobbyManager.onLobbyChanged = (Action)Delegate.Combine(lobbyManager.onLobbyChanged, new Action(OnLobbyChanged));
		}

		protected override void OnUninstall()
		{
			NetworkManagerSystem.onClientConnectGlobal -= OnClientConnectGlobal;
			NetworkManagerSystem.onClientDisconnectGlobal -= OnClientDisconnectGlobal;
			NetworkManagerSystem.onStartServerGlobal -= OnStartServerGlobal;
			NetworkManagerSystem.onStopServerGlobal -= OnStopServerGlobal;
			LobbyManager lobbyManager = PlatformSystems.lobbyManager;
			lobbyManager.onLobbyChanged = (Action)Delegate.Remove(lobbyManager.onLobbyChanged, new Action(OnLobbyChanged));
			groupSizeField?.Uninstall();
			groupSizeField = null;
			base.OnUninstall();
		}

		protected override string RebuildValue()
		{
			if (groupId == PlatformID.nil)
			{
				return null;
			}
			return TextSerialization.ToStringInvariant(groupId.ID);
		}

		private void OnClientConnectGlobal(NetworkConnection conn)
		{
			if (conn is SteamNetworkConnection steamNetworkConnection)
			{
				hostId = steamNetworkConnection.steamId;
			}
		}

		private void OnClientDisconnectGlobal(NetworkConnection conn)
		{
			hostId = PlatformID.nil;
		}

		private void OnStartServerGlobal()
		{
			hostId = NetworkManagerSystem.singleton.serverP2PId;
		}

		private void OnStopServerGlobal()
		{
			hostId = PlatformID.nil;
		}

		private void OnLobbyChanged()
		{
			SetLobbyId(new PlatformID(Client.Instance.Lobby.CurrentLobby));
		}
	}

	private abstract class SteamPlayerGroupSizeField : BaseRichPresenceField
	{
		protected int groupSize;

		protected override string key => "steam_player_group_size";

		protected override string RebuildValue()
		{
			return TextSerialization.ToStringInvariant(groupSize);
		}
	}

	private sealed class SteamPlayerGroupSizeFieldLobby : SteamPlayerGroupSizeField
	{
		protected override void OnInstall()
		{
			base.OnInstall();
			LobbyManager lobbyManager = PlatformSystems.lobbyManager;
			lobbyManager.onPlayerCountUpdated = (Action)Delegate.Combine(lobbyManager.onPlayerCountUpdated, new Action(UpdateGroupSize));
			UpdateGroupSize();
		}

		protected override void OnUninstall()
		{
			LobbyManager lobbyManager = PlatformSystems.lobbyManager;
			lobbyManager.onPlayerCountUpdated = (Action)Delegate.Remove(lobbyManager.onPlayerCountUpdated, new Action(UpdateGroupSize));
			base.OnUninstall();
		}

		private void UpdateGroupSize()
		{
			SetDirtyableValue(ref groupSize, PlatformSystems.lobbyManager.calculatedTotalPlayerCount);
		}
	}

	private sealed class SteamPlayerGroupSizeFieldGame : SteamPlayerGroupSizeField
	{
		protected override void OnInstall()
		{
			base.OnInstall();
			NetworkUser.onNetworkUserDiscovered += OnNetworkUserDiscovered;
			NetworkUser.onNetworkUserLost += OnNetworkUserLost;
			UpdateGroupSize();
		}

		protected override void OnUninstall()
		{
			NetworkUser.onNetworkUserDiscovered -= OnNetworkUserDiscovered;
			NetworkUser.onNetworkUserLost -= OnNetworkUserLost;
			base.OnUninstall();
		}

		private void UpdateGroupSize()
		{
			SetDirtyableValue(ref groupSize, NetworkUser.readOnlyInstancesList.Count);
		}

		private void OnNetworkUserLost(NetworkUser networkuser)
		{
			UpdateGroupSize();
		}

		private void OnNetworkUserDiscovered(NetworkUser networkUser)
		{
			UpdateGroupSize();
		}
	}

	private sealed class SteamDisplayField : BaseRichPresenceField
	{
		protected override string key => "steam_display";

		protected override string RebuildValue()
		{
			Scene activeScene = SceneManager.GetActiveScene();
			if ((bool)Run.instance)
			{
				if ((bool)GameOverController.instance)
				{
					return "#Display_GameOver";
				}
				return "#Display_InGame";
			}
			if ((bool)NetworkSession.instance)
			{
				return "#Display_PreGame";
			}
			if (SteamLobbyFinder.running)
			{
				return "#Display_Quickplay";
			}
			if (PlatformSystems.lobbyManager.isInLobby)
			{
				return "#Display_InLobby";
			}
			if (activeScene.name == "logbook")
			{
				return "#Display_Logbook";
			}
			return "#Display_MainMenu";
		}

		protected override void OnInstall()
		{
			base.OnInstall();
			RoR2Application.onUpdate += base.SetDirty;
		}

		protected override void OnUninstall()
		{
			RoR2Application.onUpdate -= base.SetDirty;
			base.OnUninstall();
		}
	}

	private const string rpDifficulty = "difficulty";

	private const string rpGameMode = "gamemode";

	private const string rpParticipationType = "participation_type";

	private const string rpMinutes = "minutes";

	private const string rpSteamPlayerGroupSize = "steam_player_group_size";

	private const string rpSteamPlayerGroup = "steam_player_group";

	private const string rpSteamDisplay = "steam_display";

	private static void SetKeyValue([NotNull] string key, [CanBeNull] string value)
	{
		if (Client.Instance != null && Client.Instance.User != null)
		{
			Client.Instance.User.SetRichPresence(key, value);
		}
	}

	public static void Init()
	{
		new DifficultyField().Install();
		new GameModeField().Install();
		new ParticipationField().Install();
		new MinutesField().Install();
		new SteamPlayerGroupField().Install();
		new SteamDisplayField().Install();
		RoR2Application.onUpdate += BaseRichPresenceField.ProcessDirtyFields;
	}
}
