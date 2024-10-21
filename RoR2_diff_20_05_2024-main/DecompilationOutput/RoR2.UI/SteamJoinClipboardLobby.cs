using System;
using System.Globalization;
using Facepunch.Steamworks;
using TMPro;
using UnityEngine;

namespace RoR2.UI;

public class SteamJoinClipboardLobby : MonoBehaviour
{
	public TextMeshProUGUI buttonText;

	public TextMeshProUGUI resultTextComponent;

	public MPButton mpButton;

	private PlatformID clipboardLobbyID;

	private const float resultTextDuration = 4f;

	protected float resultTextTimer;

	private static SteamJoinClipboardLobby instance;

	public bool validClipboardLobbyID { get; private set; }

	private void OnEnable()
	{
		SingletonHelper.Assign(ref instance, this);
	}

	private void OnDisable()
	{
		SingletonHelper.Unassign(ref instance, this);
	}

	[SystemInitializer(new Type[] { })]
	public static void Init()
	{
		LobbyManager lobbyManager = PlatformSystems.lobbyManager;
		lobbyManager.onLobbyJoined = (Action<bool>)Delegate.Combine(lobbyManager.onLobbyJoined, new Action<bool>(OnLobbyJoined));
	}

	private static void OnLobbyJoined(bool success)
	{
		if ((bool)instance && (bool)instance.resultTextComponent)
		{
			instance.resultTextTimer = 4f;
			instance.resultTextComponent.enabled = true;
			instance.resultTextComponent.SetText(Language.GetString(success ? "STEAM_JOIN_LOBBY_CLIPBOARD_SUCCESS" : "STEAM_JOIN_LOBBY_CLIPBOARD_FAIL"));
		}
	}

	private static bool IsLobbyIdValid(PlatformID lobbyId)
	{
		return lobbyId != PlatformID.nil;
	}

	private static PlatformID FetchClipboardLobbyId()
	{
		if (PlatformSystems.EgsToggleConVar.value == 1)
		{
			return FetchClipboardLobbyIdEGS();
		}
		return FetchClipboardLobbyIdSTEAM();
	}

	private static PlatformID FetchClipboardLobbyIdSTEAM()
	{
		if (PlatformID.TryParse(GUIUtility.systemCopyBuffer, out var result))
		{
			PlatformID platformID = new PlatformID(Client.Instance?.Lobby.CurrentLobby ?? PlatformID.nil.ID);
			if (result.isLobby && platformID != result)
			{
				return result;
			}
		}
		return PlatformID.nil;
	}

	private static PlatformID FetchClipboardLobbyIdEGS()
	{
		string systemCopyBuffer = GUIUtility.systemCopyBuffer;
		if ((PlatformSystems.lobbyManager as EOSLobbyManager).CurrentLobbyId != systemCopyBuffer && systemCopyBuffer != string.Empty)
		{
			return new PlatformID(systemCopyBuffer);
		}
		return PlatformID.nil;
	}

	private void OnApplicationFocus(bool hasFocus)
	{
		if (!hasFocus)
		{
			return;
		}
		clipboardLobbyID = FetchClipboardLobbyId();
		validClipboardLobbyID = IsLobbyIdValid(clipboardLobbyID);
		if (mpButton != null)
		{
			mpButton.interactable = validClipboardLobbyID;
		}
		if (resultTextComponent != null)
		{
			if (resultTextTimer > 0f)
			{
				resultTextTimer -= Time.fixedDeltaTime;
				resultTextComponent.enabled = true;
			}
			else
			{
				resultTextComponent.enabled = false;
			}
		}
	}

	public void TryToJoinClipboardLobby()
	{
		clipboardLobbyID = FetchClipboardLobbyId();
		Console.instance.SubmitCmd(null, string.Format(CultureInfo.InvariantCulture, "steam_lobby_join {0}", clipboardLobbyID.ToString()), recordSubmit: true);
	}
}
