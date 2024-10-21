using System;
using System.Collections.Generic;
using System.Globalization;
using RoR2.EntitlementManagement;
using RoR2.ExpansionManagement;
using RoR2.Networking;
using UnityEngine;

namespace RoR2.UI;

[RequireComponent(typeof(RectTransform))]
public class HostGamePanelController : MonoBehaviour
{
	public CarouselController gameModePicker;

	public CarouselController maxPlayersPicker;

	public GameObject capacityWarningLabel;

	private void Awake()
	{
		BuildGameModeChoices();
		SetDefaultHostNameIfEmpty();
		BuildMaxPlayerChoices();
	}

	private void OnEnable()
	{
		PlatformOnEnable();
	}

	private void OnDisable()
	{
		PlatformOnDisable();
	}

	private void Update()
	{
		capacityWarningLabel.SetActive(NetworkManagerSystem.SvMaxPlayersConVar.instance.intValue > RoR2Application.maxPlayers);
	}

	private void BuildGameModeChoices()
	{
		List<CarouselController.Choice> list = new List<CarouselController.Choice>(GameModeCatalog.gameModeCount);
		for (GameModeIndex gameModeIndex = (GameModeIndex)0; (int)gameModeIndex < GameModeCatalog.gameModeCount; gameModeIndex++)
		{
			Run gameModePrefabComponent = GameModeCatalog.GetGameModePrefabComponent(gameModeIndex);
			ExpansionRequirementComponent component = gameModePrefabComponent.GetComponent<ExpansionRequirementComponent>();
			if (gameModePrefabComponent.userPickable && (!component || !component.requiredExpansion || EntitlementManager.localUserEntitlementTracker.AnyUserHasEntitlement(component.requiredExpansion.requiredEntitlement)))
			{
				list.Add(new CarouselController.Choice
				{
					suboptionDisplayToken = gameModePrefabComponent.nameToken,
					convarValue = gameModePrefabComponent.name
				});
			}
		}
		gameModePicker.choices = list.ToArray();
		gameModePicker.gameObject.SetActive(list.Count > 1);
		string @string = Console.instance.FindConVar("gamemode").GetString();
		bool flag = false;
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].convarValue == @string)
			{
				flag = true;
				break;
			}
		}
		if (list.Count == 1 || !flag)
		{
			Debug.LogFormat("Invalid gamemode {0} detected. Reverting to ClassicRun.", @string);
			gameModePicker.SubmitSetting(list[0].convarValue);
		}
	}

	private void BuildMaxPlayerChoices()
	{
		int num = 2;
		int num2 = RoR2Application.hardMaxPlayers;
		int? num3 = PlatformGetCurrentLobbyCapacity();
		if (num3.HasValue)
		{
			num = Math.Max(2, num3.Value);
			num2 = num;
		}
		List<CarouselController.Choice> list = new List<CarouselController.Choice>(num2 - num + 1);
		for (int i = num; i <= num2; i++)
		{
			string convarValue = TextSerialization.ToStringInvariant(i);
			list.Add(new CarouselController.Choice
			{
				suboptionDisplayToken = i.ToString(CultureInfo.CurrentCulture),
				convarValue = convarValue
			});
		}
		maxPlayersPicker.choices = list.ToArray();
	}

	private void SetDefaultHostNameIfEmpty()
	{
		NetworkManagerSystem.SvHostNameConVar instance = NetworkManagerSystem.SvHostNameConVar.instance;
		if (string.IsNullOrEmpty(instance.GetString()))
		{
			instance.SetString(Language.GetStringFormatted("HOSTGAMEPANEL_DEFAULT_SERVER_NAME_FORMAT", RoR2Application.GetBestUserName()));
		}
	}

	private void PlatformOnEnable()
	{
		LobbyManager lobbyManager = PlatformSystems.lobbyManager;
		lobbyManager.onLobbyChanged = (Action)Delegate.Combine(lobbyManager.onLobbyChanged, new Action(OnLobbyChanged));
		LobbyManager lobbyManager2 = PlatformSystems.lobbyManager;
		lobbyManager2.onLobbyDataUpdated = (Action)Delegate.Combine(lobbyManager2.onLobbyDataUpdated, new Action(OnLobbyDataUpdated));
	}

	private void PlatformOnDisable()
	{
		LobbyManager lobbyManager = PlatformSystems.lobbyManager;
		lobbyManager.onLobbyDataUpdated = (Action)Delegate.Remove(lobbyManager.onLobbyDataUpdated, new Action(OnLobbyDataUpdated));
		LobbyManager lobbyManager2 = PlatformSystems.lobbyManager;
		lobbyManager2.onLobbyChanged = (Action)Delegate.Remove(lobbyManager2.onLobbyChanged, new Action(OnLobbyChanged));
	}

	private void OnLobbyDataUpdated()
	{
		BuildMaxPlayerChoices();
	}

	private void OnLobbyChanged()
	{
		BuildMaxPlayerChoices();
	}

	private int? PlatformGetCurrentLobbyCapacity()
	{
		return PlatformSystems.lobbyManager.newestLobbyData?.totalMaxPlayers;
	}
}
