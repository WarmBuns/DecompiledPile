using System;
using System.Text;
using HG;
using RoR2.ConVar;
using RoR2.UI;
using RoR2.UI.MainMenu;

namespace RoR2;

public abstract class PCLobbyManager : LobbyManager
{
	protected class SteamLobbyTypeConVar : BaseConVar
	{
		public static readonly SteamLobbyTypeConVar instance = new SteamLobbyTypeConVar("steam_lobby_type", ConVarFlags.Engine, null, "The type of the current Steamworks lobby. Cannot be set if not the owner of a lobby.");

		public SteamLobbyTypeConVar(string name, ConVarFlags flags, string defaultValue, string helpText)
			: base(name, flags, defaultValue, helpText)
		{
		}

		public override void SetString(string newValue)
		{
			(PlatformSystems.lobbyManager as PCLobbyManager).SetLobbyTypeConVarString(newValue);
		}

		public override string GetString()
		{
			return (PlatformSystems.lobbyManager as PCLobbyManager).GetLobbyTypeConVarString();
		}

		public void GetEnumValueAbstract<T>(string str, ref T dest) where T : struct, Enum
		{
			if (Enum.TryParse<T>(str, out var result))
			{
				dest = result;
				return;
			}
			StringBuilder stringBuilder = HG.StringBuilderPool.RentStringBuilder();
			stringBuilder.Append("Provided value \"").Append(str).Append("\"")
				.Append(" is not a recognized option. Recognized options: { ");
			bool flag = false;
			string[] names = Enum.GetNames(typeof(T));
			foreach (string value in names)
			{
				if (flag)
				{
					stringBuilder.Append(", ");
				}
				else
				{
					flag = true;
				}
				stringBuilder.Append("\"").Append(value).Append("\"");
			}
			stringBuilder.Append(" }");
			string message = stringBuilder.ToString();
			HG.StringBuilderPool.ReturnStringBuilder(stringBuilder);
			throw new ConCommandException(message);
		}
	}

	public abstract void SetLobbyTypeConVarString(string newValue);

	public abstract string GetLobbyTypeConVarString();

	public abstract void JoinLobby(ConCommandArgs lobbyID);

	public abstract void LobbyCreate(ConCommandArgs args);

	public abstract void LobbyCreateIfNone(ConCommandArgs args);

	public abstract void LobbyLeave(ConCommandArgs args);

	public abstract void LobbyAssignOwner(ConCommandArgs args);

	public abstract void LobbyInvite(ConCommandArgs args);

	public abstract void LobbyOpenInviteOverlay(ConCommandArgs args);

	public abstract void LobbyCopyToClipboard(ConCommandArgs args);

	public abstract void LobbyPrintData(ConCommandArgs args);

	public abstract void DisplayId(ConCommandArgs args);

	public abstract void DisplayLobbyId(ConCommandArgs args);

	public abstract void LobbyPrintMembers(ConCommandArgs args);

	public abstract void ClearLobbies(ConCommandArgs args);

	public abstract void LobbyUpdatePlayerCount(ConCommandArgs args);

	public abstract void LobbyForceUpdateData(ConCommandArgs args);

	public abstract void LobbyPrintList(ConCommandArgs args);

	public abstract bool CheckLobbyIdValidity(string lobbyID);

	public static void ShowEnableCrossPlayPopup(bool isLobbyCrossplay)
	{
		SimpleDialogBox dialogBox = SimpleDialogBox.Create();
		Action activateCrossplayAndRestartFunction = delegate
		{
			if ((bool)dialogBox)
			{
				ActivateCrossPlayAndRestart();
			}
		};
		dialogBox.headerToken = new SimpleDialogBox.TokenParamsPair
		{
			token = "EOS_CANNOT_JOIN_STEAM_LOBBY_HEADER",
			formatParams = Array.Empty<object>()
		};
		if (isLobbyCrossplay)
		{
			dialogBox.descriptionToken = new SimpleDialogBox.TokenParamsPair
			{
				token = "EOS_INVALID_LOBBY_REQUIRES_CROSSPLAY_DESCRIPTION",
				formatParams = Array.Empty<object>()
			};
		}
		else
		{
			dialogBox.descriptionToken = new SimpleDialogBox.TokenParamsPair
			{
				token = "EOS_INVALID_LOBBY_NO_CROSSPLAY_DESCRIPTION",
				formatParams = Array.Empty<object>()
			};
		}
		dialogBox.AddActionButton(delegate
		{
			activateCrossplayAndRestartFunction();
		}, "EOS_INVALID_LOBBY_CROSSPLAY_GO_TO_SETTINGS", true);
		dialogBox.AddCancelButton(CommonLanguageTokens.cancel);
		static void ActivateCrossPlayAndRestart()
		{
			MainMenuController.instance.SetDesiredMenuScreen(MainMenuController.instance.settingsMenuScreen);
		}
	}

	[ConCommand(commandName = "steam_lobby_join")]
	private static void CCSteamLobbyJoin(ConCommandArgs args)
	{
		(PlatformSystems.lobbyManager as PCLobbyManager).JoinLobby(args);
	}

	[ConCommand(commandName = "steam_lobby_create")]
	private static void CCSteamLobbyCreate(ConCommandArgs args)
	{
		(PlatformSystems.lobbyManager as PCLobbyManager).LobbyCreate(args);
	}

	[ConCommand(commandName = "steam_lobby_create_if_none")]
	private static void CCSteamLobbyCreateIfNone(ConCommandArgs args)
	{
		(PlatformSystems.lobbyManager as PCLobbyManager).LobbyCreateIfNone(args);
	}

	[ConCommand(commandName = "steam_lobby_leave")]
	private static void CCSteamLobbyLeave(ConCommandArgs args)
	{
		(PlatformSystems.lobbyManager as PCLobbyManager).LobbyLeave(args);
	}

	[ConCommand(commandName = "steam_lobby_assign_owner")]
	private static void CCSteamLobbyAssignOwner(ConCommandArgs args)
	{
		(PlatformSystems.lobbyManager as PCLobbyManager).LobbyAssignOwner(args);
	}

	[ConCommand(commandName = "steam_lobby_invite", flags = ConVarFlags.None, helpText = "Invites the player with the specified steam id to the current lobby.")]
	private static void CCSteamLobbyInvite(ConCommandArgs args)
	{
		(PlatformSystems.lobbyManager as PCLobbyManager).LobbyInvite(args);
	}

	[ConCommand(commandName = "steam_lobby_open_invite_overlay", flags = ConVarFlags.None, helpText = "Opens the steam overlay to the friend invite dialog.")]
	private static void CCSteamLobbyOpenInviteOverlay(ConCommandArgs args)
	{
		(PlatformSystems.lobbyManager as PCLobbyManager).LobbyOpenInviteOverlay(args);
	}

	[ConCommand(commandName = "lobby_open_invite_overlay", flags = ConVarFlags.None, helpText = "Opens the platform overlay to the friend invite dialog.")]
	private static void CCLobbyOpenInviteOverlay(ConCommandArgs args)
	{
		(PlatformSystems.lobbyManager as PCLobbyManager).LobbyOpenInviteOverlay(args);
	}

	[ConCommand(commandName = "steam_lobby_copy_to_clipboard", flags = ConVarFlags.None, helpText = "Copies the currently active lobby to the clipboard if applicable.")]
	private static void CCSteamLobbyCopyToClipboard(ConCommandArgs args)
	{
		(PlatformSystems.lobbyManager as PCLobbyManager).LobbyCopyToClipboard(args);
	}

	private static void CCSteamLobbyPrintData(ConCommandArgs args)
	{
		(PlatformSystems.lobbyManager as PCLobbyManager).LobbyPrintData(args);
	}

	[ConCommand(commandName = "steam_id", flags = ConVarFlags.None, helpText = "Displays your steam id.")]
	private static void CCSteamId(ConCommandArgs args)
	{
		(PlatformSystems.lobbyManager as PCLobbyManager).DisplayId(args);
	}

	[ConCommand(commandName = "steam_lobby_id", flags = ConVarFlags.None, helpText = "Displays the steam id of the current lobby.")]
	private static void CCSteamLobbyId(ConCommandArgs args)
	{
		(PlatformSystems.lobbyManager as PCLobbyManager).DisplayLobbyId(args);
	}

	[ConCommand(commandName = "steam_lobby_print_members", flags = ConVarFlags.None, helpText = "Displays the members current lobby.")]
	private static void CCSteamLobbyPrintMembers(ConCommandArgs args)
	{
		(PlatformSystems.lobbyManager as PCLobbyManager).LobbyPrintMembers(args);
	}

	[ConCommand(commandName = "steam_lobby_print_list", flags = ConVarFlags.None, helpText = "Displays a list of lobbies from the last search.")]
	private static void CCSteamLobbyPrintList(ConCommandArgs args)
	{
		(PlatformSystems.lobbyManager as PCLobbyManager).LobbyPrintList(args);
	}

	[ConCommand(commandName = "steam_lobby_update_player_count", flags = ConVarFlags.None, helpText = "Forces a refresh of the steam lobby player count.")]
	private static void CCSteamLobbyUpdatePlayerCount(ConCommandArgs args)
	{
		(PlatformSystems.lobbyManager as PCLobbyManager).LobbyUpdatePlayerCount(args);
	}

	[ConCommand(commandName = "dump_lobbies", flags = ConVarFlags.None, helpText = "")]
	private static void DumpLobbies(ConCommandArgs args)
	{
		(PlatformSystems.lobbyManager as PCLobbyManager).ClearLobbies(args);
	}
}
