using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Assets.RoR2.Scripts.Platform;
using Facepunch.Steamworks;
using JetBrains.Annotations;
using Rewired;
using RoR2.Stats;
using UnityEngine;
using Zio;

namespace RoR2;

public class UserProfile
{
	public struct TutorialProgression
	{
		public uint showCount;

		public bool shouldShow;
	}

	public bool isClaimed;

	public bool canSave;

	public string fileName;

	public ulong saveID;

	public bool isInUse;

	public IFileSystem fileSystem;

	public UPath filePath = UPath.Empty;

	[SaveField]
	public string name;

	[SaveField]
	public uint coins;

	[SaveField]
	public uint totalCollectedCoins;

	[SaveField]
	public string version = "2";

	[SaveField]
	public float screenShakeScale = 1f;

	[SaveField]
	public float directionalDamageIndicatorScale;

	[SaveField]
	public bool increasedDifficultyBarFX;

	[SaveField]
	public bool useInspectFeature;

	[SaveField]
	public bool useInspectAutopause;

	[SaveField]
	public bool useToggleScoreboard;

	[SaveField]
	public bool useTeleporterParticleScaling;

	[SaveField]
	public bool useTeleporterDiscoveryIndicator;

	[SaveField(explicitSetupMethod = "SetupKeyboardMap")]
	public KeyboardMap keyboardMap = new KeyboardMap(DefaultControllerMaps.defaultKeyboardMap);

	[SaveField(explicitSetupMethod = "SetupMouseMap")]
	public MouseMap mouseMap = new MouseMap(DefaultControllerMaps.defaultMouseMap);

	[SaveField(explicitSetupMethod = "SetupJoystickMap")]
	public JoystickMap joystickMap = new JoystickMap(DefaultControllerMaps.DefaultJoystickMap);

	[SaveField(explicitSetupMethod = "SetupJoystickHardwareMap")]
	public Dictionary<Guid, JoystickMap> HardwareJoystickMaps2 = new Dictionary<Guid, JoystickMap>();

	[SaveField]
	public float mouseLookSensitivity = 0.25f;

	[SaveField]
	public float mouseLookScaleX = 1f;

	[SaveField]
	public float mouseLookScaleY = 1f;

	[SaveField]
	public bool mouseLookInvertX;

	[SaveField]
	public bool mouseLookInvertY;

	[SaveField]
	public float stickLookSensitivity = 4f;

	[SaveField]
	public float stickLookScaleX = 1f;

	[SaveField]
	public float stickLookScaleY = 1f;

	[SaveField]
	public int hudSizeMode;

	[SaveField]
	public bool gyroEnabled;

	[SaveField]
	public float gyroLookSensitivity = 1f;

	[SaveField]
	public float gyroLookScaleX = 1f;

	[SaveField]
	public float gyroLookScaleY = 0.65f;

	[SaveField]
	public bool gyroLookInvertX;

	[SaveField]
	public bool gyroLookInvertY;

	[SaveField]
	public bool gyroDisabledDuringSprint = true;

	[SaveField]
	public bool gyroDisabledDuringSprint_Huntress;

	[SaveField]
	public bool gyroDisabledDuringSprint_Melee;

	[SaveField]
	public bool toggleArtificerHover = true;

	[SaveField]
	public bool toggleSeekerSojourn = true;

	[SaveField]
	public bool stickLookInvertX;

	[SaveField]
	public bool stickLookInvertY;

	[SaveField]
	public float gamepadVibrationScale = 1f;

	[SaveField]
	public bool aimAssistEnabled = true;

	[SaveField]
	public float aimAssistSensitivity = 6f;

	[SaveField]
	public bool disableItemDisplays;

	[SaveField]
	public float inputAimAssistSensitivity = 4f;

	[SaveField]
	public float mouseTrajectoryAimAssistSensitivity;

	[SaveField]
	public float gamePadTrajectoryAimAssistSensitivity = 4f;

	public bool saveRequestPending;

	private static string[] saveFieldNames;

	public static SaveFieldAttribute[] saveFields;

	private static readonly Dictionary<string, SaveFieldAttribute> nameToSaveFieldMap = new Dictionary<string, SaveFieldAttribute>();

	public static UserProfile defaultProfile;

	[SaveField(explicitSetupMethod = "SetupTokenList")]
	public List<string> viewedUnlockablesList = new List<string>();

	[SaveField(explicitSetupMethod = "SetupPickupsSet")]
	private readonly bool[] discoveredPickups = PickupCatalog.GetPerPickupBuffer<bool>();

	public Action<PickupIndex> onPickupDiscovered;

	public Action onStatsReceived;

	[SaveField(explicitSetupMethod = "SetupTokenList")]
	private List<string> achievementsList = new List<string>();

	[SaveField(explicitSetupMethod = "SetupTokenList")]
	private List<string> unviewedAchievementsList = new List<string>();

	public StatSheet statSheet = StatSheet.New();

	private const uint maxShowCount = 3u;

	public TutorialProgression tutorialDifficulty;

	public TutorialProgression tutorialSprint;

	public TutorialProgression tutorialEquipment;

	[SaveField]
	private SurvivorDef survivorPreference = SurvivorCatalog.defaultSurvivor;

	public readonly Loadout loadout = new Loadout();

	[SaveField]
	public uint totalLoginSeconds;

	[SaveField]
	public uint totalRunSeconds;

	[SaveField]
	public uint totalAliveSeconds;

	[SaveField]
	public uint totalRunCount;

	private bool ownsPortrait;

	[SaveField]
	public bool chefAchievementIngredient1;

	[SaveField]
	public bool chefAchievementIngredient2;

	[SaveField]
	public bool chefAchievementIngredient3;

	[SaveField]
	public string RebirthItem;

	private const string defaultProfileContents = "<UserProfile>\r\n  <name>Survivor</name>\r\n  <mouseLookSensitivity>0.2</mouseLookSensitivity>\r\n  <mouseLookScaleX>1</mouseLookScaleX>\r\n  <mouseLookScaleY>1</mouseLookScaleY>\r\n  <stickLookSensitivity>5</stickLookSensitivity>\r\n  <stickLookScaleX>1</stickLookScaleX>\r\n  <stickLookScaleY>1</stickLookScaleY>\r\n <inputAimAssistSensitivity>4</inputAimAssistSensitivity>\r\n <mouseTrajectoryAimAssistSensitivity>0</mouseTrajectoryAimAssistSensitivity>\r\n <gamePadTrajectoryAimAssistSensitivity>4</gamePadTrajectoryAimAssistSensitivity>\r\n</UserProfile>";

	[SaveField(defaultValue = "", explicitSetupMethod = "SetupTokenList", fieldName = "viewedViewables")]
	private readonly List<string> viewedViewables = new List<string>();

	public bool isCorrupted { get; set; }

	public bool hasUnviewedAchievement => unviewedAchievementsList.Count > 0;

	public bool loggedIn { get; set; }

	public Texture portraitTexture { get; private set; }

	public event Action onSurvivorPreferenceChanged;

	public static event Action<UserProfile> onSurvivorPreferenceChangedGlobal;

	public event Action onLoadoutChanged;

	public static event Action<UserProfile> onLoadoutChangedGlobal;

	public static event Action<UserProfile, UnlockableDef> onUnlockableGranted;

	public static event Action<UserProfile> onUserProfileViewedViewablesChanged;

	public void FillDefaultJoystickMaps()
	{
		Dictionary<Guid, JoystickMap> defaultJoystickMaps = DefaultControllerMaps.GetDefaultJoystickMaps();
		if (defaultJoystickMaps == null)
		{
			return;
		}
		foreach (KeyValuePair<Guid, JoystickMap> item in defaultJoystickMaps)
		{
			DefaultControllerMaps.RewiredDebugLog($"FillDefaultJoystickMaps> Guid {item.Key} --- {item.Value.ToXmlString()}");
			HardwareJoystickMaps2.Add(item.Key, item.Value);
			LocalUser.DumpControllerMap(item.Value);
		}
	}

	public JoystickMap GetJoystickMap(Guid inControllerGuid)
	{
		if (HardwareJoystickMaps2.Count == 0)
		{
			FillDefaultJoystickMaps();
		}
		if (HardwareJoystickMaps2.ContainsKey(inControllerGuid))
		{
			DefaultControllerMaps.RewiredDebugLog($"***** GetJoystickMap: return controller for guid {inControllerGuid}");
			return new JoystickMap(HardwareJoystickMaps2[inControllerGuid]);
		}
		if (inControllerGuid == DefaultControllerMaps.xboneControllerGuid && HardwareJoystickMaps2.ContainsKey(DefaultControllerMaps.xbox360ControllerGuid))
		{
			DefaultControllerMaps.RewiredDebugWarn("***** GetJoystickMap: Failed to find guid {0}... returning Xbox360 mapping (XInput detected)", inControllerGuid);
			return new JoystickMap(HardwareJoystickMaps2[DefaultControllerMaps.xbox360ControllerGuid]);
		}
		if (inControllerGuid == DefaultControllerMaps.xbox360ControllerGuid && HardwareJoystickMaps2.ContainsKey(DefaultControllerMaps.xboneControllerGuid))
		{
			DefaultControllerMaps.RewiredDebugWarn("***** GetJoystickMap: Failed to find guid {0}... returning Xbone mapping (XInput detected)", inControllerGuid);
			return new JoystickMap(HardwareJoystickMaps2[DefaultControllerMaps.xboneControllerGuid]);
		}
		if (HardwareJoystickMaps2.ContainsKey(PlatformManager.DefaultControllerGuid))
		{
			DefaultControllerMaps.RewiredDebugWarn("***** GetJoystickMap: Failed to find guid {0}... returning default", inControllerGuid);
			return new JoystickMap(HardwareJoystickMaps2[PlatformManager.DefaultControllerGuid]);
		}
		return null;
	}

	public void SwitchJoystickMap(Guid inNewControllerGuid)
	{
		DefaultControllerMaps.RewiredDebugLog($"***** SwitchJoystickMap: From {this.joystickMap.hardwareGuid} to {inNewControllerGuid}");
		JoystickMap joystickMap = GetJoystickMap(inNewControllerGuid);
		if (joystickMap != null)
		{
			this.joystickMap = joystickMap;
		}
	}

	public void UpdateJoystickMap(Guid inControllerGuid, JoystickMap inJoystickMap)
	{
		if (!HardwareJoystickMaps2.ContainsKey(inControllerGuid))
		{
			DefaultControllerMaps.RewiredDebugWarn($"***** UpdateJoystickMap: {inControllerGuid} not found!");
			return;
		}
		HardwareJoystickMaps2[inControllerGuid] = inJoystickMap;
		if (inControllerGuid == joystickMap.hardwareGuid)
		{
			SwitchJoystickMap(inControllerGuid);
		}
	}

	public void UpdateKeyboardMap(KeyboardMap inKeyboardMap)
	{
		keyboardMap = inKeyboardMap;
	}

	public void UpdateMouseMap(MouseMap inMouseMap)
	{
		mouseMap = inMouseMap;
	}

	public static void GenerateSaveFieldFunctions()
	{
		nameToSaveFieldMap.Clear();
		FieldInfo[] fields = typeof(UserProfile).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (FieldInfo fieldInfo in fields)
		{
			SaveFieldAttribute customAttribute = CustomAttributeExtensions.GetCustomAttribute<SaveFieldAttribute>(fieldInfo);
			if (customAttribute != null)
			{
				customAttribute.Setup(fieldInfo);
				nameToSaveFieldMap[fieldInfo.Name] = customAttribute;
			}
		}
		saveFieldNames = nameToSaveFieldMap.Keys.ToArray();
		Array.Sort(saveFieldNames);
		saveFields = saveFieldNames.Select((string name) => nameToSaveFieldMap[name]).ToArray();
	}

	public void SetSaveFieldString([NotNull] string fieldName, [NotNull] string value)
	{
		if (nameToSaveFieldMap.TryGetValue(fieldName, out var value2))
		{
			value2.setter(this, value);
			return;
		}
		Debug.LogErrorFormat("Save field {0} is not defined.", fieldName);
	}

	public string GetSaveFieldString([NotNull] string fieldName)
	{
		if (nameToSaveFieldMap.TryGetValue(fieldName, out var value))
		{
			return value.getter(this);
		}
		Debug.LogErrorFormat("Save field {0} is not defined.", fieldName);
		return string.Empty;
	}

	public static XDocument ToXml(UserProfile userProfile)
	{
		return XmlUtility.ToXml(userProfile);
	}

	public static UserProfile FromXml(XDocument doc)
	{
		return XmlUtility.FromXml(doc);
	}

	public bool HasUnlockable([NotNull] string unlockableToken)
	{
		UnlockableDef unlockableDef = UnlockableCatalog.GetUnlockableDef(unlockableToken);
		if (!(unlockableDef == null))
		{
			return HasUnlockable(unlockableDef);
		}
		return true;
	}

	public bool HasUnlockable([CanBeNull] UnlockableDef unlockableDef)
	{
		return statSheet.HasUnlockable(unlockableDef);
	}

	public void AddUnlockToken(string unlockableToken)
	{
		UnlockableDef unlockableDef = UnlockableCatalog.GetUnlockableDef(unlockableToken);
		if (unlockableDef != null)
		{
			GrantUnlockable(unlockableDef);
		}
	}

	public void GrantUnlockable(UnlockableDef unlockableDef)
	{
		if (!statSheet.HasUnlockable(unlockableDef))
		{
			statSheet.AddUnlockable(unlockableDef);
			Debug.LogFormat("{0} unlocked {1}", name, unlockableDef.nameToken);
			RequestEventualSave();
			UserProfile.onUnlockableGranted?.Invoke(this, unlockableDef);
		}
	}

	public void RequestEventualSave()
	{
		if (canSave)
		{
			saveRequestPending = true;
		}
	}

	public void RevokeUnlockable(UnlockableDef unlockableDef)
	{
		if (statSheet.HasUnlockable(unlockableDef))
		{
			statSheet.RemoveUnlockable(unlockableDef.index);
			Debug.LogFormat("{0} had {1} revoked", name, unlockableDef.nameToken);
			RequestEventualSave();
		}
	}

	public bool HasSurvivorUnlocked(SurvivorIndex survivorIndex)
	{
		SurvivorDef survivorDef = SurvivorCatalog.GetSurvivorDef(survivorIndex);
		if (survivorDef == null)
		{
			return false;
		}
		if (!survivorDef.unlockableDef)
		{
			return true;
		}
		return HasUnlockable(survivorDef.unlockableDef);
	}

	public bool HasDiscoveredPickup(PickupIndex pickupIndex)
	{
		if (pickupIndex.isValid)
		{
			return discoveredPickups[pickupIndex.value];
		}
		return false;
	}

	public void DiscoverPickup(PickupIndex pickupIndex)
	{
		SetPickupDiscovered(pickupIndex, newDiscovered: true);
	}

	private void SetPickupDiscovered(PickupIndex pickupIndex, bool newDiscovered)
	{
		if (!pickupIndex.isValid)
		{
			return;
		}
		ref bool reference = ref discoveredPickups[pickupIndex.value];
		if (reference != newDiscovered)
		{
			reference = newDiscovered;
			if (newDiscovered)
			{
				onPickupDiscovered?.Invoke(pickupIndex);
				RequestEventualSave();
			}
		}
	}

	[ConCommand(commandName = "user_profile_set_pickup_discovered", flags = ConVarFlags.Cheat, helpText = "Sets the pickup discovery state for the sender's profile.")]
	private static void CCUserProfileSetPickupDiscovered(ConCommandArgs args)
	{
		UserProfile userProfile = args.GetSenderLocalUser().userProfile;
		IEnumerable<PickupIndex> enumerable = null;
		enumerable = (IEnumerable<PickupIndex>)((!(args.TryGetArgString(0) == "all")) ? new PickupIndex[1] { args.GetArgPickupIndex(0) } : ((object)PickupCatalog.allPickupIndices));
		bool argBool = args.GetArgBool(1);
		foreach (PickupIndex item in enumerable)
		{
			userProfile.SetPickupDiscovered(item, argBool);
		}
	}

	public bool HasAchievement(string achievementName)
	{
		return achievementsList.Contains(achievementName);
	}

	public bool CanSeeAchievement(string achievementName)
	{
		if (HasAchievement(achievementName))
		{
			return true;
		}
		AchievementDef achievementDef = AchievementManager.GetAchievementDef(achievementName);
		if (achievementDef != null)
		{
			if (string.IsNullOrEmpty(achievementDef.prerequisiteAchievementIdentifier))
			{
				return true;
			}
			return HasAchievement(achievementDef.prerequisiteAchievementIdentifier);
		}
		return false;
	}

	public void AddAchievement(string achievementName, bool isExternal)
	{
		achievementsList.Add(achievementName);
		unviewedAchievementsList.Add(achievementName);
		if (isExternal)
		{
			PlatformSystems.achievementSystem.AddAchievement(achievementName);
		}
		RequestEventualSave();
	}

	public void RevokeAchievement(string achievementName)
	{
		achievementsList.Remove(achievementName);
		unviewedAchievementsList.Remove(achievementName);
		RequestEventualSave();
	}

	public string PopNextUnviewedAchievementName()
	{
		if (unviewedAchievementsList.Count == 0)
		{
			return null;
		}
		string result = unviewedAchievementsList[0];
		unviewedAchievementsList.RemoveAt(0);
		return result;
	}

	public void ClearAllAchievementNotifications()
	{
		unviewedAchievementsList.Clear();
	}

	public void ApplyDeltaStatSheet(StatSheet deltaStatSheet)
	{
		int i = 0;
		for (int unlockableCount = deltaStatSheet.GetUnlockableCount(); i < unlockableCount; i++)
		{
			GrantUnlockable(deltaStatSheet.GetUnlockable(i));
		}
		for (int j = 0; j < deltaStatSheet.fields.Length; j++)
		{
			if (deltaStatSheet.fields[j].hasChanged)
			{
				deltaStatSheet.fields[j].statDef.onChangeCallback?.Invoke();
			}
		}
		statSheet.ApplyDelta(deltaStatSheet);
		onStatsReceived?.Invoke();
	}

	[ConCommand(commandName = "user_profile_stats_stress_test", flags = ConVarFlags.Cheat, helpText = "Sets the stats of the sender's user profile to the maximum their datatypes support for stress-testing purposes.")]
	private static void CCUserProfileStatsStressTest(ConCommandArgs args)
	{
		LocalUser senderLocalUser = args.GetSenderLocalUser();
		senderLocalUser.userProfile.statSheet.SetAllFieldsToMaxValue();
		PlatformSystems.saveSystem.Save(senderLocalUser.userProfile, blocking: true);
	}

	private void ResetShouldShowTutorial(ref TutorialProgression tutorialProgression)
	{
		tutorialProgression.shouldShow = tutorialProgression.showCount < 3;
	}

	private void RebuildTutorialProgressions()
	{
		ResetShouldShowTutorial(ref tutorialDifficulty);
		ResetShouldShowTutorial(ref tutorialSprint);
		ResetShouldShowTutorial(ref tutorialEquipment);
	}

	[NotNull]
	public SurvivorDef GetSurvivorPreference()
	{
		return survivorPreference;
	}

	public void SetSurvivorPreference([NotNull] SurvivorDef newSurvivorPreference)
	{
		if (!newSurvivorPreference)
		{
			throw new ArgumentException("Provided object is null or invalid", "newSurvivorPreference");
		}
		if ((object)survivorPreference != newSurvivorPreference)
		{
			survivorPreference = newSurvivorPreference;
			this.onSurvivorPreferenceChanged?.Invoke();
			UserProfile.onSurvivorPreferenceChangedGlobal?.Invoke(this);
		}
	}

	public void OnLoadoutChanged()
	{
		this.onLoadoutChanged?.Invoke();
		UserProfile.onLoadoutChangedGlobal?.Invoke(this);
		RequestEventualSave();
	}

	public void CopyLoadout(Loadout dest)
	{
		loadout.Copy(dest);
	}

	public void SetLoadout(Loadout newLoadout)
	{
		if (!loadout.ValueEquals(newLoadout))
		{
			newLoadout.Copy(loadout);
			OnLoadoutChanged();
		}
	}

	[ConCommand(commandName = "loadout_set_skill_variant", flags = (ConVarFlags.ExecuteOnServer | ConVarFlags.Cheat), helpText = "loadout_set_skill_variant [body_name] [skill_slot_index] [skill_variant_index]\nSets the skill variant for the sender's user profile.")]
	private static void CCLoadoutSetSkillVariant(ConCommandArgs args)
	{
		BodyIndex argBodyIndex = args.GetArgBodyIndex(0);
		int argInt = args.GetArgInt(1);
		int argInt2 = args.GetArgInt(2);
		UserProfile userProfile = args.GetSenderLocalUser().userProfile;
		Loadout loadout = new Loadout();
		userProfile.loadout.Copy(loadout);
		loadout.bodyLoadoutManager.SetSkillVariant(argBodyIndex, argInt, (uint)argInt2);
		userProfile.SetLoadout(loadout);
		if ((bool)args.senderMaster)
		{
			args.senderMaster.SetLoadoutServer(loadout);
		}
		if ((bool)args.senderBody)
		{
			args.senderBody.SetLoadoutServer(loadout);
		}
	}

	public void OnLogin()
	{
		if (loggedIn)
		{
			Debug.LogErrorFormat("Profile {0} is already logged in!", fileName);
			return;
		}
		loggedIn = true;
		PlatformSystems.saveSystem.loggedInProfiles.Add(this);
		RebuildTutorialProgressions();
		foreach (string achievements in achievementsList)
		{
			PlatformSystems.achievementSystem.AddAchievement(achievements);
			UserProfile userProfile = LocalUserManager.GetFirstLocalUser().userProfile;
			if (userProfile != null && userProfile == this)
			{
				BaseActivitySelector baseActivitySelector = new BaseActivitySelector();
				baseActivitySelector.activityAchievementID = achievements;
				PlatformSystems.activityManager.TryToCompleteActivity(baseActivitySelector);
			}
		}
		LoadPortrait();
	}

	public void OnLogout()
	{
		if (!loggedIn)
		{
			Debug.LogErrorFormat("Profile {0} is already logged out!", fileName);
			return;
		}
		UnloadPortrait();
		loggedIn = false;
		if (canSave)
		{
			PlatformSystems.saveSystem.Save(this, blocking: true);
		}
		PlatformSystems.saveSystem.loggedInProfiles.Remove(this);
	}

	private void LoadPortrait()
	{
		Texture2D newPortraitTexture = new Texture2D(184, 184, TextureFormat.ARGB32, mipChain: false, linear: false);
		portraitTexture = newPortraitTexture;
		ownsPortrait = true;
		Client.Instance.Friends.GetAvatar(Friends.AvatarSize.Large, Client.Instance.SteamId, LoadSteamImage);
		void LoadSteamImage(Image image)
		{
			if (image?.Data != null && (bool)newPortraitTexture && !(newPortraitTexture != portraitTexture))
			{
				byte[] data = image.Data;
				Color32[] array = new Color32[data.Length / 4];
				int width = image.Width;
				int height = image.Height;
				newPortraitTexture.Reinitialize(width, height);
				for (int i = 0; i < height; i++)
				{
					int num = height - 1 - i;
					for (int j = 0; j < width; j++)
					{
						int num2 = (i * width + j) * 4;
						array[num * width + j] = new Color32(data[num2], data[num2 + 1], data[num2 + 2], data[num2 + 3]);
					}
				}
				newPortraitTexture.SetPixels32(array);
				newPortraitTexture.Apply();
			}
		}
	}

	public void ReloadPortrait()
	{
		RoR2Application.onUpdate -= ReloadPortrait;
		if (loggedIn)
		{
			LoadPortrait();
		}
	}

	private void UnloadPortrait()
	{
		if (ownsPortrait)
		{
			UnityEngine.Object.Destroy(portraitTexture);
			portraitTexture = null;
			ownsPortrait = false;
		}
	}

	[ConCommand(commandName = "revert_chef_achievement_tracking_bools", flags = ConVarFlags.Cheat, helpText = "Reverts the bools associated with the chef achievement ingredient tracking.")]
	private static void CCRevertChefAchievementTrackingBools(ConCommandArgs args)
	{
		LocalUser senderLocalUser = args.GetSenderLocalUser();
		senderLocalUser.userProfile.chefAchievementIngredient1 = false;
		senderLocalUser.userProfile.chefAchievementIngredient2 = false;
		senderLocalUser.userProfile.chefAchievementIngredient3 = false;
	}

	[ConCommand(commandName = "empty_rebirth_items", flags = ConVarFlags.Cheat, helpText = "Empties rebirth items stored in user's profile.")]
	private static void CCEmptyRebirthItems(ConCommandArgs args)
	{
		args.GetSenderLocalUser().userProfile.RebirthItem = null;
	}

	[ConCommand(commandName = "give_rebirth_reward", flags = ConVarFlags.Cheat, helpText = "gives rebirth items")]
	private static void CCGiveRebirth(ConCommandArgs args)
	{
		LocalUser senderLocalUser = args.GetSenderLocalUser();
		if (args.userArgs.Count > 0)
		{
			PickupIndex pickupIndex = PickupCatalog.FindPickupIndex(args.GetArgItemIndex(0));
			if (!(pickupIndex != PickupIndex.none))
			{
				return;
			}
			senderLocalUser.userProfile.RebirthItem = PickupCatalog.GetPickupDef(pickupIndex).internalName;
		}
		else
		{
			senderLocalUser.userProfile.RebirthItem = PickupCatalog.GetPickupDef(new PickupIndex(UnityEngine.Random.Range(0, ItemCatalog.itemCount))).internalName;
		}
		senderLocalUser.userProfile.AddUnlockToken("Artifacts.Rebirth");
	}

	public static void LoadDefaultProfile()
	{
		defaultProfile = XmlUtility.FromXml(XDocument.Parse("<UserProfile>\r\n  <name>Survivor</name>\r\n  <mouseLookSensitivity>0.2</mouseLookSensitivity>\r\n  <mouseLookScaleX>1</mouseLookScaleX>\r\n  <mouseLookScaleY>1</mouseLookScaleY>\r\n  <stickLookSensitivity>5</stickLookSensitivity>\r\n  <stickLookScaleX>1</stickLookScaleX>\r\n  <stickLookScaleY>1</stickLookScaleY>\r\n <inputAimAssistSensitivity>4</inputAimAssistSensitivity>\r\n <mouseTrajectoryAimAssistSensitivity>0</mouseTrajectoryAimAssistSensitivity>\r\n <gamePadTrajectoryAimAssistSensitivity>4</gamePadTrajectoryAimAssistSensitivity>\r\n</UserProfile>"));
		defaultProfile.canSave = false;
	}

	public bool HasViewedViewable(string viewableName)
	{
		return viewedViewables.Contains(viewableName);
	}

	public void MarkViewableAsViewed(string viewableName)
	{
		if (!HasViewedViewable(viewableName))
		{
			viewedViewables.Add(viewableName);
			UserProfile.onUserProfileViewedViewablesChanged?.Invoke(this);
			RequestEventualSave();
		}
	}

	public void ClearAllViewablesAsViewed()
	{
		viewedViewables.Clear();
		UserProfile.onUserProfileViewedViewablesChanged?.Invoke(this);
		RequestEventualSave();
	}
}
