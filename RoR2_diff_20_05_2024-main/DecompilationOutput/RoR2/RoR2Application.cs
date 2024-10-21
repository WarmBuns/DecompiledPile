using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Facepunch.Steamworks;
using HG;
using JetBrains.Annotations;
using KinematicCharacterController;
using Rewired;
using RoR2.ContentManagement;
using RoR2.ConVar;
using RoR2.EntitlementManagement;
using RoR2.Modding;
using RoR2.PostProcess;
using RoR2.UI;
using RoR2.UI.LogBook;
using RoR2.UI.MainMenu;
using RoR2.WwiseUtils;
using ThreeEyedGames;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zio.FileSystems;

namespace RoR2;

public class RoR2Application : MonoBehaviour
{
	public static readonly string messageForModders = "We don't officially support modding at this time but if you're going to mod the game please change this value to true if you're modding the game. This will disable some things like Prismatic Trials and put players into a separate matchmaking queue from vanilla users to protect their game experience.";

	public static bool isModded = false;

	public GameObject networkManagerPrefab;

	public GameObject platformManagerPrefab;

	public GameObject networkSessionPrefab;

	public GameObject pauseStopController;

	public PostProcessVolume postProcessSettingsController;

	public Canvas mainCanvas;

	public UnityEngine.UI.Image saveIconImage;

	public Behaviour[] BehavioursToEnableDuringStartup;

	public static Stopwatch _stopwatch = new Stopwatch();

	public static long _lastCheckTime;

	private int loadGameContentPercentage;

	public float KCSUpdateFPS = 15f;

	public static int numFixedUpdateCalls = 0;

	public const string gameName = "Risk of Rain 2";

	private const uint ror1AppId = 248820u;

	public const uint ror2AppId = 632360u;

	private const uint ror2DedicatedServerAppId = 1180760u;

	public const bool isDedicatedServer = false;

	public const uint appId = 632360u;

	public static string steamBuildId = "STEAM_UNINITIALIZED";

	private static string buildId;

	public static readonly int hardMaxPlayers = 16;

	public static readonly int maxPlayers = 4;

	public static readonly int maxLocalPlayers = 4;

	public static Type[] AssemblyTypes = null;

	private static IntConVar waitMsConVar = new IntConVar("wait_ms", ConVarFlags.None, "-1", "How many milliseconds to sleep between each frame. -1 for no sleeping between frames.");

	public static readonly TimerQueue timeTimers = new TimerQueue();

	public static readonly TimerQueue fixedTimeTimers = new TimerQueue();

	public static readonly TimerQueue unscaledTimeTimers = new TimerQueue();

	private ulong totalFixedUpdateCount;

	public static FileSystem cloudStorage;

	public static Func<bool> loadSteamworksClient;

	public static Action unloadSteamworksClient;

	private Stack<UnityEngine.ThreadPriority> PriorityStack = new Stack<UnityEngine.ThreadPriority>();

	public static Action onLoad;

	public static Action onLoadFinished;

	public static Action onStart;

	public static Action onShutDown;

	public static bool isInLocalMultiPlayer = false;

	public static readonly Xoroshiro128Plus rng = new Xoroshiro128Plus((ulong)DateTime.UtcNow.Ticks);

	public static long lastDuration => _stopwatch.ElapsedMilliseconds - _lastCheckTime;

	public static Stopwatch stopwatch
	{
		get
		{
			if (!_stopwatch.IsRunning)
			{
				_stopwatch.Start();
			}
			_lastCheckTime = _stopwatch.ElapsedMilliseconds;
			return _stopwatch;
		}
		private set
		{
		}
	}

	public static bool IsMainMenuInitialized { get; private set; } = false;

	public static bool IsEntitlementsUpdated { get; private set; } = false;

	public static bool IsAllUsersEntitlementsUpdated { get; set; } = false;

	public Client steamworksClient { get; private set; }

	public static RoR2Application instance { get; private set; }

	public static bool IsMainScene => SceneManager.GetActiveScene().name == "title";

	public static FileSystem fileSystem { get; private set; }

	public static bool loadFinished { get; private set; } = false;

	public static bool isLoading { get; private set; } = false;

	public static bool isInSinglePlayer
	{
		get
		{
			if (NetworkServer.active && NetworkServer.dontListen)
			{
				return !isInLocalMultiPlayer;
			}
			return false;
		}
	}

	public static bool isInMultiPlayer
	{
		get
		{
			if (NetworkClient.active)
			{
				if (NetworkServer.active && NetworkServer.dontListen)
				{
					return isInLocalMultiPlayer;
				}
				return true;
			}
			return false;
		}
	}

	public static event Action onUpdate;

	public static event Action onFixedUpdate;

	public static event Action onLateUpdate;

	public static event Action onNextUpdate;

	private static void AssignBuildId()
	{
		buildId = Application.version;
	}

	public static string GetBuildId()
	{
		return buildId;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void PrintSW(string message)
	{
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Print(object text)
	{
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void PrintError(object text)
	{
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Print(string text)
	{
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Print(string format, params object[] arguments)
	{
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void PrintWarning(object text)
	{
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void PrintException(Exception exception)
	{
	}

	private void Awake()
	{
		if (maxPlayers != 4 || (Application.genuineCheckAvailable && !Application.genuine))
		{
			isModded = true;
		}
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
		if ((bool)instance)
		{
			UnityEngine.Object.Destroy(base.gameObject);
			return;
		}
		instance = this;
		AssignBuildId();
		Print("buildId = " + buildId);
		if (AssemblyTypes == null)
		{
			AssemblyTypes = Assembly.GetExecutingAssembly().GetTypes();
		}
		if (!isLoading)
		{
			isLoading = true;
			StartCoroutine(OnLoad());
		}
		MainMenuController.OnMainMenuInitialised += OnMainMenuControllerInitialized;
		BaseUserEntitlementTracker<LocalUser>.OnUserEntitlementsUpdated = (Action)Delegate.Combine(BaseUserEntitlementTracker<LocalUser>.OnUserEntitlementsUpdated, new Action(OnEntitlementsUpdated));
		BaseUserEntitlementTracker<LocalUser>.OnAllUserEntitlementsUpdated = (Action)Delegate.Combine(BaseUserEntitlementTracker<LocalUser>.OnAllUserEntitlementsUpdated, new Action(OnAllUsersEntitlementsUpdated));
	}

	public static IEnumerator TransferToSceneAdditivelyAsync(string _sceneGuid)
	{
		Scene oldScene = SceneManager.GetActiveScene();
		AsyncOperationHandle<SceneInstance> handler = Addressables.LoadSceneAsync(_sceneGuid, LoadSceneMode.Additive, activateOnLoad: false);
		yield return handler;
		yield return handler.Result.ActivateAsync();
		SceneManager.SetActiveScene(handler.Result.Scene);
		SceneManager.UnloadSceneAsync(oldScene);
	}

	private void Start()
	{
		if (instance == this && onStart != null)
		{
			onStart();
			onStart = null;
		}
		KinematicCharacterMotor.updateTimerMax = 1f / KCSUpdateFPS;
	}

	public static void HardStopPlayInEditor(string windowTitle, string windowMessage)
	{
	}

	private void Update()
	{
		if (waitMsConVar.value > 0)
		{
			Thread.Sleep(waitMsConVar.value);
		}
		UpdateCursorState();
		RoR2Application.onUpdate?.Invoke();
		Interlocked.Exchange(ref RoR2Application.onNextUpdate, null)?.Invoke();
		timeTimers.Update(Time.deltaTime);
		unscaledTimeTimers.Update(Time.unscaledDeltaTime);
	}

	private void OnMainMenuControllerInitialized()
	{
		MainMenuController.OnMainMenuInitialised -= OnMainMenuControllerInitialized;
		IsMainMenuInitialized = true;
	}

	private void OnEntitlementsUpdated()
	{
		BaseUserEntitlementTracker<LocalUser>.OnUserEntitlementsUpdated = (Action)Delegate.Remove(BaseUserEntitlementTracker<LocalUser>.OnUserEntitlementsUpdated, new Action(OnEntitlementsUpdated));
		IsEntitlementsUpdated = true;
	}

	private void OnAllUsersEntitlementsUpdated()
	{
		IsAllUsersEntitlementsUpdated = true;
	}

	public static void UpdateCursorState()
	{
		if (!Application.isBatchMode && MPEventSystemManager.kbmEventSystem != null)
		{
			CursorLockMode cursorLockMode = ((!MPEventSystemManager.kbmEventSystem.isCursorVisible && !MPEventSystemManager.combinedEventSystem.isCursorVisible) ? CursorLockMode.Locked : CursorLockMode.Confined);
			if (cursorLockMode != Cursor.lockState)
			{
				Cursor.lockState = cursorLockMode;
			}
			Cursor.visible = false;
		}
	}

	private void FixedUpdate()
	{
		totalFixedUpdateCount++;
		numFixedUpdateCalls++;
		RoR2Application.onFixedUpdate?.Invoke();
		fixedTimeTimers.Update(Time.fixedDeltaTime);
	}

	private void LateUpdate()
	{
		numFixedUpdateCalls = 0;
		RoR2Application.onLateUpdate?.Invoke();
	}

	private IEnumerator OnLoad()
	{
		return InitializeGameRoutine();
	}

	private void EnableBehaviours()
	{
		int num = BehavioursToEnableDuringStartup.Length;
		for (int i = 0; i < num; i++)
		{
			BehavioursToEnableDuringStartup[i].enabled = true;
		}
	}

	public void PushThreadPriority(UnityEngine.ThreadPriority newPriority)
	{
		Print("Pushing background loading priority to {0}. was:{1}", newPriority.ToString(), Application.backgroundLoadingPriority.ToString());
		PriorityStack.Push(Application.backgroundLoadingPriority);
		PlatformSystems.SetLoadingPriority(newPriority);
	}

	public void PopThreadPriority()
	{
		if (PriorityStack.Count > 0)
		{
			UnityEngine.ThreadPriority backgroundLoadingPriority = PriorityStack.Pop();
			Print("Popping background loading priority to {0}. was:{1}", backgroundLoadingPriority.ToString(), Application.backgroundLoadingPriority.ToString());
			Application.backgroundLoadingPriority = backgroundLoadingPriority;
		}
		else
		{
			Print("No previous background loading priority, keeping this priority: {0}.", Application.backgroundLoadingPriority.ToString());
		}
	}

	private IEnumerator InitializeGameRoutine()
	{
		PushThreadPriority(UnityEngine.ThreadPriority.High);
		isLoading = true;
		UnitySystemConsoleRedirector.Redirect();
		PrintSW("After ConsoleRedirector");
		SceneManager.sceneLoaded += delegate(Scene scene, LoadSceneMode loadSceneMode)
		{
			Print("Loaded scene {0} loadSceneMode={1}", scene.name, loadSceneMode);
		};
		SceneManager.sceneUnloaded += delegate(Scene scene)
		{
			Print("Unloaded scene {0}", scene.name);
		};
		SceneManager.activeSceneChanged += delegate(Scene oldScene, Scene newScene)
		{
			Print("Active scene changed from {0} to {1}", oldScene.name, newScene.name);
		};
		while (SceneManager.GetActiveScene().name != "loadingbasic")
		{
			yield return new WaitForEndOfFrame();
		}
		PrintSW("After loadingbasic");
		SpriteAsNumberManager loadingPercentIndicatorLabel = LoadingScreenCanvas.Instance.percentage;
		int loadDisplayPercentage = 0;
		if ((bool)loadingPercentIndicatorLabel)
		{
			loadingPercentIndicatorLabel.SetPercentValue(0);
		}
		yield return new WaitForEndOfFrame();
		yield return new WaitForEndOfFrame();
		PlatformSystems.SetLoadingPriority(UnityEngine.ThreadPriority.High);
		PrintSW("Before EnableBehaviors");
		EnableBehaviours();
		PrintSW("After EnableBehaviors");
		yield return IncrementPercentageAndWaitFrame();
		WwiseIntegrationManager.Init();
		PrintSW("After WwiseIntegrationManager");
		yield return IncrementPercentageAndWaitFrame();
		PhysicalFileSystem physicalFileSystem = new PhysicalFileSystem();
		SubFileSystem subFileSystem = new SubFileSystem(physicalFileSystem, physicalFileSystem.ConvertPathFromInternal(Application.dataPath));
		Print("application data path is" + Application.dataPath);
		fileSystem = subFileSystem;
		cloudStorage = fileSystem;
		PlatformSystems.Init();
		PrintSW("After PlatformSystems.Init");
		yield return IncrementPercentageAndWaitFrame();
		if (!(loadSteamworksClient?.Invoke() ?? false))
		{
			Application.Quit();
			yield break;
		}
		RewiredIntegrationManager.Init();
		PrintSW("After RewiredIntegrationManager.Init");
		yield return IncrementPercentageAndWaitFrame();
		ModLoader.instance.FlushOutput(UnityEngine.Debug.Log);
		ModLoader.instance.SetGameDataType((Mod mod) => new RoR2Mod(mod));
		ModLoader.instance.PostLaunch();
		ModLoader.instance.FlushOutput(UnityEngine.Debug.Log);
		UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/UI/MPEventSystemManager"));
		GameObject networkManager = UnityEngine.Object.Instantiate(networkManagerPrefab);
		yield return IncrementPercentageAndWaitFrame();
		PlatformSystems.InitNetworkManagerSystem(networkManager);
		yield return IncrementPercentageAndWaitFrame();
		CharacterBody.Init();
		yield return IncrementPercentageAndWaitFrame();
		if (PlatformSystems.textDataManager != null)
		{
			while (!PlatformSystems.textDataManager.InitializedConfigFiles)
			{
				Print("Text config stuff still happening...");
				yield return null;
			}
		}
		PrintSW("After PlatformSystems");
		GameObject obj = new GameObject("Console");
		obj.AddComponent<SetDontDestroyOnLoad>();
		obj.AddComponent<Console>();
		while (!Console.loadingIsComplete)
		{
			yield return null;
		}
		PrintSW("After Console");
		StartCoroutine(InitDuringStartupAttribute.Execute());
		while (!InitDuringStartupAttribute.loadingIsComplete)
		{
			yield return null;
		}
		yield return IncrementPercentageAndWaitFrame();
		PrintSW("After InitDuringStartupAttribute");
		bool hasStartupError = false;
		try
		{
			AkSoundEngine.IsInitialized();
		}
		catch (DllNotFoundException)
		{
			IssueStartupError(new SimpleDialogBox.TokenParamsPair("STARTUP_FAILURE_DIALOG_TITLE"), new SimpleDialogBox.TokenParamsPair("MSVCR_2015_BAD_INSTALL_DIALOG_BODY"), new(string, Action)[1] { ("MSVCR_2015_BAD_INSTALL_DIALOG_DOWNLOAD_BUTTON", OpenVCR2015DownloadPage) });
		}
		QuickStopwatch.Begin("Loading Language");
		Language.collectLanguageRootFolders += delegate(List<string> list)
		{
			list.Add(System.IO.Path.Combine(Application.streamingAssetsPath, "Language"));
		};
		string[] platformStrings = PlatformSystems.platformStrings;
		foreach (string i2 in platformStrings)
		{
			Language.collectLanguageRootFolders += delegate(List<string> list)
			{
				list.Add(System.IO.Path.Combine(Application.streamingAssetsPath, "LanguageOverrides", i2));
			};
		}
		QuickStopwatch.Lap("Collected Language Root Folders");
		yield return Language.Init();
		QuickStopwatch.Lap("Language Init Complete");
		QuickStopwatch.End();
		IncrementPercentageAndWaitFrame();
		PrintSW("After Language");
		MusicController.Init();
		DecaliciousRenderer.InitializeReferencesAsync();
		TypewriteTextController.PreLoadIntroTextFontAsync();
		yield return LegacyShaderAPI.Preload(IncrementPercentageAndWaitFrame());
		SobelCommandBuffer.PreloadShaderReference();
		RoR2Content.Init();
		PrintSW("After RoR2Content.Init");
		yield return null;
		Coroutine loadContentCoroutine = StartCoroutine(LoadGameContent(loadingPercentIndicatorLabel, loadDisplayPercentage));
		PrintSW("Before LoadGameContent");
		while ((float)loadGameContentPercentage < 25f)
		{
			yield return null;
		}
		yield return CameraRigController.InitializeReferences();
		if (!hasStartupError)
		{
			while (Console.instance == null)
			{
				Print("Console not initialized yet...");
				yield return null;
			}
			PrintSW("After splash load start");
			yield return StartCoroutine(LoadHelper.LoadSceneAdditivelyAsync("fe13e00695117284fb0d1b25422a0529"));
			PrintSW("After splash load end");
		}
		yield return LoadHelper.StartLoadSceneAdditiveAsync("330d792ae1727574e969e68ce8e966d2");
		yield return loadContentCoroutine;
		yield return new WaitForEndOfFrame();
		PrintSW("After LoadGameContent");
		if (platformManagerPrefab != null)
		{
			UnityEngine.Object.Instantiate(platformManagerPrefab);
		}
		PrintSW("After platformManager");
		PlatformSystems.InitPlatformManagerObject(base.gameObject);
		PrintSW("After InitPlatformManagerObject");
		GameObject platformVoiceManagerObject = PlatformSystems.GetPlatformVoiceManagerObject();
		if (platformVoiceManagerObject != null)
		{
			PrintError("RoR2Application - VoiceChatManager created?: " + UnityEngine.Object.Instantiate(platformVoiceManagerObject) == null);
		}
		PrintSW("After voiceChatManagerPrefab");
		yield return SystemInitializerAttribute.ExecuteCoroutine();
		PrintSW("After SystemInitializerAttribute");
		yield return AchievementManager.DoInit();
		PrintSW("After AchievementManager");
		SystemInitializerAttribute.RegisterPreInitializedType(typeof(AchievementManager));
		yield return LogBookController.Init();
		PrintSW("After LogBookController.Init()");
		SystemInitializerAttribute.RegisterPreInitializedType(typeof(LogBookController));
		yield return EntityStateCatalog.Init();
		PrintSW("After EntityStateCatalogInit");
		LocalUserManager.Init();
		PrintSW("After LocalUserManager.Init");
		PlatformSystems.saveSystem.LoadUserProfiles();
		if (onLoad != null)
		{
			onLoad();
			onLoad = null;
			PrintSW("After OnLoad event");
		}
		isLoading = false;
		loadFinished = true;
		PopThreadPriority();
		WaitForEndOfFrame IncrementPercentageAndWaitFrame()
		{
			if (loadingPercentIndicatorLabel != null)
			{
				loadingPercentIndicatorLabel.SetPercentValue(++loadDisplayPercentage);
			}
			return new WaitForEndOfFrame();
		}
		void IssueStartupError(SimpleDialogBox.TokenParamsPair headerToken, SimpleDialogBox.TokenParamsPair descriptionToken, (string token, Action action)[] buttons)
		{
			if (!hasStartupError)
			{
				hasStartupError = true;
				LocalUserManager.ClearUsers();
				NetworkManager.singleton.ServerChangeScene("title");
				SimpleDialogBox simpleDialogBox = SimpleDialogBox.Create();
				simpleDialogBox.headerToken = headerToken;
				simpleDialogBox.descriptionToken = descriptionToken;
				simpleDialogBox.rootObject.transform.SetParent(mainCanvas.transform);
				OnDestroyCallback.AddCallback(simpleDialogBox.rootObject, delegate
				{
					Console.instance.SubmitCmd(null, "quit");
				});
				if (buttons != null)
				{
					for (int j = 0; j < buttons.Length; j++)
					{
						(string, Action) tuple = buttons[j];
						simpleDialogBox.AddActionButton(tuple.Item2.Invoke, tuple.Item1, true);
					}
				}
				simpleDialogBox.AddCancelButton("PAUSE_QUIT_TO_DESKTOP");
			}
		}
		static void OpenVCR2015DownloadPage()
		{
			Application.OpenURL("https://www.microsoft.com/en-us/download/details.aspx?id=48145");
		}
	}

	private IEnumerator LoadGameContent(SpriteAsNumberManager loadingPercentIndicatorLabel, int startPercentage)
	{
		float remainingPercentage = 100f - (float)startPercentage;
		TimeSpan maxLoadTimePerFrame = TimeSpan.FromSeconds(1.0 / 60.0);
		ContentManager.collectContentPackProviders += delegate(ContentManager.AddContentPackProviderDelegate addContentPackProvider)
		{
			addContentPackProvider(new RoR2Content());
			addContentPackProvider(new JunkContent());
			addContentPackProvider(new DLC1Content());
			addContentPackProvider(new CU8Content());
			addContentPackProvider(new DLC2Content());
			foreach (IContentPackProvider item3 in from mod in ModLoader.instance.allEnabledModsReadOnly
				select mod.GetGameData<RoR2Mod>().contentPackProvider into contentPackProvider
				where contentPackProvider != null
				select contentPackProvider)
			{
				addContentPackProvider(item3);
			}
		};
		ReadableProgress<float> contentLoadProgressReceiver = new ReadableProgress<float>();
		IEnumerator item = ContentManager.LoadContentPacks(contentLoadProgressReceiver);
		Stopwatch totalLoadStopwatch = new Stopwatch();
		totalLoadStopwatch.Start();
		Stopwatch thisFrameLoadStopwatch = new Stopwatch();
		int previousProgressPercent = 0;
		Stack<IEnumerator> coroutineStack = new Stack<IEnumerator>();
		coroutineStack.Push(item);
		while (coroutineStack.Count > 0)
		{
			thisFrameLoadStopwatch.Restart();
			int num = 0;
			do
			{
				num++;
				IEnumerator enumerator2 = coroutineStack.Peek();
				if (enumerator2.Current is IEnumerator item2)
				{
					coroutineStack.Push(item2);
					continue;
				}
				while (!enumerator2.MoveNext())
				{
					coroutineStack.Pop();
					if (coroutineStack.Count == 0)
					{
						break;
					}
					enumerator2 = coroutineStack.Peek();
				}
			}
			while (coroutineStack.Count > 0 && thisFrameLoadStopwatch.Elapsed < maxLoadTimePerFrame);
			thisFrameLoadStopwatch.Stop();
			loadGameContentPercentage = Mathf.FloorToInt(contentLoadProgressReceiver.value * 100f);
			int num2 = Math.Min(99, startPercentage + Mathf.FloorToInt(contentLoadProgressReceiver.value * remainingPercentage));
			if (previousProgressPercent != num2)
			{
				previousProgressPercent = num2;
				if ((bool)loadingPercentIndicatorLabel)
				{
					loadingPercentIndicatorLabel.SetPercentValue(num2);
				}
			}
			yield return new WaitForEndOfFrame();
		}
		yield return SceneCatalog.Init();
		SystemInitializerAttribute.RegisterPreInitializedType(typeof(SceneCatalog));
		Print("Game content load completed in {0}ms.", totalLoadStopwatch.ElapsedMilliseconds);
		while (!SoundbankLoader.doneLoading || !RoR2Content.initialized)
		{
			yield return null;
		}
		while (!LegacyResourcesAPI.PendingLoadingAssetsOrTimeout())
		{
			yield return null;
		}
		Print("async game objects loaded in {0}ms.", totalLoadStopwatch.ElapsedMilliseconds);
		onLoadFinished?.Invoke();
	}

	private void OnDestroy()
	{
		if (instance == this && PlatformSystems.EgsToggleConVar.value != 1)
		{
			unloadSteamworksClient?.Invoke();
		}
	}

	private void OnApplicationQuit()
	{
		if ((bool)Console.instance)
		{
			Console.instance.SaveArchiveConVars();
		}
		onShutDown?.Invoke();
		unloadSteamworksClient?.Invoke();
		UnitySystemConsoleRedirector.Disengage();
	}

	[ConCommand(commandName = "quit", flags = ConVarFlags.None, helpText = "Close the application.")]
	private static void CCQuit(ConCommandArgs args)
	{
		Application.Quit();
	}

	[ConCommand(commandName = "locCmd", flags = ConVarFlags.None, helpText = "Redirects a command to be run for a certain local user.")]
	public static void CCRunCmdForLocalUser(ConCommandArgs args)
	{
		int argInt = args.GetArgInt(0);
		if (LocalUserManager.readOnlyLocalUsersList.Count > argInt)
		{
			string argString = args.GetArgString(1);
			Console.instance.SubmitCmd(LocalUserManager.readOnlyLocalUsersList[argInt].currentNetworkUser, argString);
		}
	}

	[NotNull]
	public static string GetBestUserName()
	{
		string text = null;
		text = text ?? Client.Instance?.Username;
		if (LocalUserManager.readOnlyLocalUsersList.Count > 0)
		{
			text = text ?? LocalUserManager.readOnlyLocalUsersList[0].userProfile.name;
		}
		return text ?? "???";
	}

	[ConCommand(commandName = "app_info", flags = ConVarFlags.None, helpText = "Get information about the application, including build and version info.")]
	private static void CCAppInfo(ConCommandArgs args)
	{
		StringBuilder stringBuilder = HG.StringBuilderPool.RentStringBuilder();
		stringBuilder.Append("====================").AppendLine();
		stringBuilder.Append("= Application Info =").AppendLine();
		stringBuilder.Append("====================").AppendLine();
		stringBuilder.Append("> Product: ").AppendLine();
		stringBuilder.Append(">   Product Name: ").Append(Application.productName).AppendLine();
		stringBuilder.Append(">   Company Name: ").Append(Application.companyName).AppendLine();
		stringBuilder.Append("> Build: ").AppendLine();
		stringBuilder.Append(">   Platform: ").Append(Application.platform).AppendLine();
		stringBuilder.Append(">   Version: ").Append(Application.version).AppendLine();
		stringBuilder.Append(">   Unity Version: ").Append(Application.unityVersion).AppendLine();
		stringBuilder.Append(">   Build GUID: ").Append(Application.buildGUID).AppendLine();
		stringBuilder.Append(">   Build Type: ").Append("Production").AppendLine();
		stringBuilder.Append(">   Is Dedicated Server: ").Append(value: false).AppendLine();
		stringBuilder.Append("> Environment: ").AppendLine();
		stringBuilder.Append(">   Command Line: ").Append(Environment.CommandLine).AppendLine();
		stringBuilder.Append(">   Console Log Path: ").Append(Application.consoleLogPath).AppendLine();
		Client client = Client.Instance;
		if (client != null && client.IsValid)
		{
			stringBuilder.Append("> Steamworks Client: ").AppendLine();
			stringBuilder.Append(">   App ID: ").AppendUint(client.AppId).AppendLine();
			stringBuilder.Append(">   Build ID: ").AppendInt(client.BuildId).AppendLine();
			stringBuilder.Append(">   Branch: ").Append(client.BetaName).AppendLine();
		}
		Server server = Server.Instance;
		if (server != null && server.IsValid)
		{
			stringBuilder.Append("> Steamworks Game Server: ").AppendLine();
			stringBuilder.Append(">   App ID: ").AppendUint(server.AppId).AppendLine();
		}
		args.Log(stringBuilder.ToString());
		stringBuilder = HG.StringBuilderPool.ReturnStringBuilder(stringBuilder);
	}

	public static void SetIsInLocalMultiplayer(bool isLocalMultiplayer)
	{
		isInLocalMultiPlayer = isLocalMultiplayer;
	}

	public static void SetMapsEnabled(bool enabled)
	{
		foreach (Player player in ReInput.players.Players)
		{
			player.controllers.maps.SetAllMapsEnabled(enabled);
		}
	}

	public static void DebugPrintRewired()
	{
		Print("---Rewired Start---");
		Print("Rewired found " + ReInput.controllers.joystickCount + " joysticks attached.");
		for (int i = 0; i < ReInput.controllers.joystickCount; i++)
		{
			Joystick joystick = ReInput.controllers.Joysticks[i];
			Print("[" + i + "] Joystick: " + joystick.name + " \n Hardware GUID: " + joystick.hardwareTypeGuid.ToString() + " \n Hardware Name: " + joystick.hardwareName + " \n Is Recognized: " + ((joystick.hardwareTypeGuid != Guid.Empty) ? "Yes" : "No") + " \n Is Assigned: " + (ReInput.controllers.IsControllerAssigned(joystick.type, joystick) ? "Yes" : "No"));
		}
		foreach (Player player in ReInput.players.Players)
		{
			Print("PlayerId = " + player.id + " is assigned " + player.controllers.joystickCount + " joysticks.");
			foreach (Joystick joystick2 in player.controllers.Joysticks)
			{
				Print("Joystick: " + joystick2.name + "\nIs Recognized: " + ((joystick2.hardwareTypeGuid != Guid.Empty) ? "Yes" : "No"));
				foreach (ControllerMap map in player.controllers.maps.GetMaps(joystick2.type, joystick2.id))
				{
					string text = ReInput.mapping.GetMapCategory(map.categoryId).name;
					string text2 = ReInput.mapping.GetJoystickLayout(map.layoutId).name;
					Print("Controller Map:\nCategory = " + text + "\nLayout = " + text2 + "\nenabled = " + map.enabled);
					ActionElementMap[] elementMaps = map.GetElementMaps();
					foreach (ActionElementMap actionElementMap in elementMaps)
					{
						InputAction action = ReInput.mapping.GetAction(actionElementMap.actionId);
						if (action != null)
						{
							Print("Action \"" + action.name + "\" is bound to \"" + actionElementMap.elementIdentifierName + "\"");
						}
					}
				}
			}
		}
		Print("---Rewired End---");
	}

	private static void AssignJoystickToAvailablePlayer(Controller controller)
	{
		IList<Player> players = ReInput.players.Players;
		for (int i = 0; i < players.Count; i++)
		{
			Player player = players[i];
			if (player.name != "PlayerMain" && player.controllers.joystickCount == 0 && !player.controllers.hasKeyboard && !player.controllers.hasMouse)
			{
				player.controllers.AddController(controller, removeFromOtherPlayers: false);
				break;
			}
		}
	}

	private static void AssignNewController(ControllerStatusChangedEventArgs args)
	{
		AssignNewController(ReInput.controllers.GetController(args.controllerType, args.controllerId));
	}

	public static void ClearControllers()
	{
		ReInput.players.GetPlayer("PlayerMain").controllers.ClearAllControllers();
	}

	private static void AssignNewController(Controller controller)
	{
		ReInput.players.GetPlayer("PlayerMain").controllers.AddController(controller, removeFromOtherPlayers: false);
		if (controller.type == ControllerType.Joystick)
		{
			AssignJoystickToAvailablePlayer(controller);
		}
	}
}
