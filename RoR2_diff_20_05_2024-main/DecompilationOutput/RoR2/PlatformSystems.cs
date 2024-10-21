using System;
using System.Linq;
using Assets.RoR2.Scripts.Platform;
using RoR2.ConVar;
using RoR2.EntitlementManagement;
using RoR2.Networking;
using UnityEngine;

namespace RoR2;

public static class PlatformSystems
{
	public static SaveSystem saveSystem;

	public static UserManager userManager;

	public static AchievementSystem achievementSystem;

	public static LobbyManager lobbyManager;

	public static TextDataManager textDataManager;

	public static PlatformManager platformManager;

	public static NetworkManagerSystem networkManager;

	public static ActivityManager activityManager;

	public static IUserEntitementsResolverNetworkAndLocal entitlementsSystem;

	public static object statManager;

	public static object friendsManager;

	public static bool crossPlayEnabledOnStartup = false;

	public static bool useNGSS = true;

	public static string[] platformStrings = new string[0];

	public static PlayerPrefsIntConVar EgsToggleConVar = new PlayerPrefsIntConVar("egsToggle", ConVarFlags.Engine, "0", "If EGS is used. If false, use Steam.");

	public const float loadOperationsStartedWeight = 0.25f;

	public const float loadCompleteProgressWeight = 0.75f;

	public static bool ShouldUseEpicOnlineSystems
	{
		get
		{
			return crossPlayEnabledOnStartup;
		}
		set
		{
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
	public static void InitDataAfterAssembliesLoaded()
	{
	}

	public static void Init()
	{
		platformStrings = new string[2] { "steam", "pc" };
		SteamworksClientManager.Init();
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		if (commandLineArgs.Contains("--disableCrossplay"))
		{
			EgsToggleConVar.value = 0;
		}
		else if (commandLineArgs.Contains("--enableCrossplay"))
		{
			EgsToggleConVar.value = 1;
		}
		if (EgsToggleConVar.value == 1)
		{
			try
			{
				crossPlayEnabledOnStartup = true;
				platformManager = new EOSPlatformManager();
				userManager = new UserManagerEOS();
				(userManager as UserManagerEOS).InitializeUserManager();
				textDataManager = new StreamingAssetsTextDataManager();
				lobbyManager = new EOSLobbyManager();
				(lobbyManager as EOSLobbyManager).Init();
				new EOSLoginManager().TryLogin();
			}
			catch
			{
				EgsToggleConVar.value = 0;
				Application.Quit();
			}
		}
		else
		{
			lobbyManager = new SteamworksLobbyManager();
			userManager = new SteamUserManager();
			(userManager as SteamUserManager)?.InitializeUserManager();
			textDataManager = new StreamingAssetsTextDataManager();
		}
		SteamworksRichPresenceManager.Init();
		saveSystem = new SaveSystemSteam();
		achievementSystem = new AchievementSystemSteam();
		entitlementsSystem = new SteamworksEntitlementResolver();
		activityManager = new ActivityManager();
		if (platformManager == null)
		{
			platformManager = new PlatformManager();
		}
		activityManager.Initialize();
		saveSystem.InitializeSaveSystem();
		UserProfile.GenerateSaveFieldFunctions();
		RoR2Application.onUpdate += saveSystem.StaticUpdate;
		if (entitlementsSystem != null)
		{
			EntitlementManager.collectLocalUserEntitlementResolvers += delegate(Action<IUserEntitlementResolver<LocalUser>> add)
			{
				add(entitlementsSystem);
			};
			EntitlementManager.collectNetworkUserEntitlementResolvers += delegate(Action<IUserEntitlementResolver<NetworkUser>> add)
			{
				add(entitlementsSystem);
			};
		}
		RoR2Application.onShutDown = (Action)Delegate.Combine(RoR2Application.onShutDown, new Action(Shutdown));
	}

	private static void Shutdown()
	{
		saveSystem.Shutdown();
		lobbyManager.Shutdown();
	}

	private static T BuildMonoSingleton<T>() where T : MonoBehaviour
	{
		GameObject gameObject = new GameObject("T");
		T result = gameObject.AddComponent<T>();
		UnityEngine.Object.DontDestroyOnLoad(gameObject);
		return result;
	}

	public static void InitNetworkManagerSystem(GameObject networkManagerPrefabObject)
	{
		if (!(networkManagerPrefabObject.GetComponent<NetworkManagerSystem>() != null))
		{
			if (EgsToggleConVar.value == 1)
			{
				networkManager = networkManagerPrefabObject.AddComponent<NetworkManagerSystemEOS>();
			}
			else
			{
				networkManager = networkManagerPrefabObject.AddComponent<NetworkManagerSystemSteam>();
			}
			NetworkManagerConfiguration component = networkManagerPrefabObject.GetComponent<NetworkManagerConfiguration>();
			if (component == null || networkManager == null)
			{
				Debug.LogError("Missing NetworkManagerConfiguration on NetworkManagerPrefab or platform NetworkManagerSystem not found");
			}
			else
			{
				networkManager.Init(component);
			}
		}
	}

	public static void InitPlatformManagerObject(GameObject platformManagerPrefabObject)
	{
	}

	public static GameObject GetPlatformVoiceManagerObject()
	{
		return null;
	}

	public static void setupRewired(GameObject rewiredObj)
	{
	}

	public static void SetLoadingPriority(ThreadPriority newPriority)
	{
		Application.backgroundLoadingPriority = newPriority;
	}
}
