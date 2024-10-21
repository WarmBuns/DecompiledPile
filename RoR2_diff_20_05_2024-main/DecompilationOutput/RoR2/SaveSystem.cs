using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Facepunch.Steamworks;
using JetBrains.Annotations;
using RoR2.Stats;
using UnityEngine;
using Zio;
using Zio.FileSystems;

namespace RoR2;

public abstract class SaveSystem : SaveSystemBase
{
	public bool isXmlReady;

	public bool isInitialLoadFinished;

	public MemoryStream configData;

	public UserProfile userProfile;

	protected readonly Dictionary<FileReference, DateTime> latestWrittenRequestTimesByFile = new Dictionary<FileReference, DateTime>();

	public readonly Dictionary<string, UserProfile> loadedUserProfiles = new Dictionary<string, UserProfile>(StringComparer.OrdinalIgnoreCase);

	public readonly List<LoadUserProfileOperationResult> badFileResults = new List<LoadUserProfileOperationResult>();

	public readonly List<UserProfile> loggedInProfiles = new List<UserProfile>();

	private static float secondAccumulator;

	private const string userProfilesFolder = "/UserProfiles";

	protected readonly Queue<FileOutput> pendingOutputQueue = new Queue<FileOutput>();

	private readonly List<Task> activeTasks = new List<Task>();

	public bool doesInitialDataExist
	{
		get
		{
			if (configData != null)
			{
				return userProfile != null;
			}
			return false;
		}
	}

	public static event Action onAvailableUserProfilesChanged;

	public static void SkipBOM(Stream stream)
	{
		long position = stream.Position;
		if (stream.Length - position < 3)
		{
			return;
		}
		int num = stream.ReadByte();
		int num2 = stream.ReadByte();
		if (num != 255 || num2 != 254)
		{
			int num3 = stream.ReadByte();
			if (num != 239 || num2 != 187 || num3 != 191)
			{
				stream.Position = position;
			}
		}
	}

	public virtual void InitializeSaveSystem()
	{
	}

	public override void LoadInitialData()
	{
	}

	protected bool CanWrite(FileOutput fileOutput)
	{
		if (fileOutput.contents.Length == 0)
		{
			Debug.LogErrorFormat("Cannot write UserProfile \"{0}\" with zero-length contents. This would erase the file.");
			return false;
		}
		if (latestWrittenRequestTimesByFile.TryGetValue(fileOutput.fileReference, out var value))
		{
			return value < fileOutput.requestTime;
		}
		return true;
	}

	protected UserProfile AttemptToRecoverUserData(string profileXML)
	{
		string value = "<a";
		string value2 = "<c";
		int num = profileXML.IndexOf(value);
		int num2 = profileXML.IndexOf(value2);
		string[] array = null;
		string text = null;
		if (num != -1 && num + 1 < profileXML.Length)
		{
			int num3 = profileXML.IndexOf("<", num + 1);
			int num4 = profileXML.IndexOf(">", num);
			if (num4 != -1)
			{
				num3 = ((num3 != -1) ? (num3 - 1) : (profileXML.Length - 1));
				string text2 = profileXML.Substring(num4 + 1, num3 - num4);
				array = text2.Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				Debug.LogError("Recovered achievements: " + text2);
			}
		}
		if (num2 != -1 && num2 + 1 < profileXML.Length)
		{
			int num5 = profileXML.IndexOf("<", num2 + 1);
			int num6 = profileXML.IndexOf(">", num2);
			if (num6 != -1)
			{
				num5 = ((num5 != -1) ? (num5 - 1) : (profileXML.Length - 1));
				text = profileXML.Substring(num6 + 1, num5 - num6);
				Debug.LogError("Recovered coins: " + text);
			}
		}
		UserProfile userProfile = CreateGuestProfile();
		if (array != null && array.Length != 0)
		{
			string[] array2 = array;
			foreach (string achievementName in array2)
			{
				userProfile.AddAchievement(achievementName, isExternal: true);
			}
		}
		else
		{
			Debug.LogError("XML didn't contain achievements!");
		}
		uint result = 0u;
		if (text != null && uint.TryParse(text, out result))
		{
			userProfile.coins = result;
		}
		else
		{
			Debug.LogError("XML didn't contain coins!");
		}
		return userProfile;
	}

	protected void AddActiveTask(Task task)
	{
		lock (activeTasks)
		{
			activeTasks.Add(task);
		}
	}

	protected void RemoveActiveTask(Task task)
	{
		lock (activeTasks)
		{
			activeTasks.Remove(task);
		}
	}

	public void StaticUpdate()
	{
		secondAccumulator += Time.unscaledDeltaTime;
		if (secondAccumulator > 1f)
		{
			secondAccumulator -= 1f;
			foreach (UserProfile loggedInProfile in loggedInProfiles)
			{
				loggedInProfile.totalLoginSeconds++;
			}
		}
		foreach (UserProfile loggedInProfile2 in loggedInProfiles)
		{
			if (loggedInProfile2.saveRequestPending && Save(loggedInProfile2, blocking: false))
			{
				loggedInProfile2.saveRequestPending = false;
			}
		}
		ProcessFileOutputQueue();
	}

	public abstract void SaveHistory(byte[] data, string fileName);

	public abstract Dictionary<string, byte[]> LoadHistory();

	public void RequestSave(UserProfile profile, bool immediate = false)
	{
		if (profile.canSave)
		{
			if (immediate)
			{
				Save(profile, blocking: true);
			}
			else
			{
				profile.saveRequestPending = true;
			}
		}
	}

	public bool Save(UserProfile data, bool blocking)
	{
		try
		{
			StartSave(data, blocking);
			return true;
		}
		catch
		{
			return false;
		}
	}

	public virtual void SaveData(byte[] inputData, string fileName)
	{
		Debug.LogError("Child class method not implemented");
	}

	public void AddLoadedUserProfile(string name, UserProfile profile)
	{
		if (profile != null && !loadedUserProfiles.ContainsKey(name))
		{
			loadedUserProfiles.Add(name, profile);
		}
	}

	public void RemoveLoadedUserProfile(ulong saveId)
	{
		foreach (UserProfile value in loadedUserProfiles.Values)
		{
			if (value.saveID == saveId)
			{
				loadedUserProfiles.Remove(value.name);
				break;
			}
		}
	}

	public List<string> GetAvailableProfileNames()
	{
		List<string> list = new List<string>();
		foreach (KeyValuePair<string, UserProfile> loadedUserProfile in loadedUserProfiles)
		{
			if (!loadedUserProfile.Value.isClaimed)
			{
				list.Add(loadedUserProfile.Key);
			}
		}
		list.Sort();
		return list;
	}

	public UserProfile GetProfile(string profileName)
	{
		profileName = profileName.ToLower(CultureInfo.InvariantCulture);
		if (loadedUserProfiles.TryGetValue(profileName, out var value))
		{
			return value;
		}
		return null;
	}

	public virtual UserProfile CreateProfile(IFileSystem fileSystem, string name, ulong platformUserID = 0uL)
	{
		UserProfile newProfile = UserProfile.FromXml(UserProfile.ToXml(UserProfile.defaultProfile));
		PlatformInitProfile(ref newProfile, fileSystem, name);
		newProfile.saveID = platformUserID;
		newProfile.isInUse = true;
		AddLoadedUserProfile(newProfile.name, newProfile);
		Save(newProfile, blocking: true);
		SaveSystem.onAvailableUserProfilesChanged?.Invoke();
		return newProfile;
	}

	protected virtual void PlatformInitProfile(ref UserProfile newProfile, IFileSystem fileSystem, string name)
	{
		newProfile.fileName = Guid.NewGuid().ToString();
		newProfile.fileSystem = fileSystem;
		newProfile.filePath = "/UserProfiles/" + newProfile.fileName + ".xml";
		newProfile.name = name;
		newProfile.canSave = true;
	}

	public UserProfile CreateGuestProfile()
	{
		UserProfile userProfile = new UserProfile();
		Copy(UserProfile.defaultProfile, userProfile);
		userProfile.name = "Guest";
		return userProfile;
	}

	public override void LoadUserProfiles()
	{
		badFileResults.Clear();
		loadedUserProfiles.Clear();
		UserProfile.LoadDefaultProfile();
		FileSystem cloudStorage = RoR2Application.cloudStorage;
		if (cloudStorage != null)
		{
			if (!cloudStorage.DirectoryExists("/UserProfiles"))
			{
				cloudStorage.CreateDirectory("/UserProfiles");
			}
			foreach (UPath item2 in cloudStorage.EnumeratePaths("/UserProfiles"))
			{
				if (cloudStorage.FileExists(item2) && string.CompareOrdinal(item2.GetExtensionWithDot(), ".xml") == 0)
				{
					LoadUserProfileOperationResult item = LoadUserProfileFromDisk(cloudStorage, item2);
					UserProfile userProfile = item.userProfile;
					if (userProfile != null)
					{
						loadedUserProfiles[userProfile.fileName] = userProfile;
					}
					if (item.exception != null)
					{
						badFileResults.Add(item);
					}
				}
			}
			OutputBadFileResults();
			SaveSystem.onAvailableUserProfilesChanged?.Invoke();
		}
		else
		{
			Debug.LogError("cloud storage is null");
		}
	}

	private void OutputBadFileResults()
	{
		if (badFileResults.Count == 0)
		{
			return;
		}
		try
		{
			using Stream stream = RoR2Application.fileSystem.CreateFile(new UPath("/bad_profiles.log"));
			using TextWriter textWriter = new StreamWriter(stream);
			foreach (LoadUserProfileOperationResult badFileResult in badFileResults)
			{
				textWriter.WriteLine("Failed to load file \"{0}\" ({1}B)", badFileResult.fileName, badFileResult.fileLength);
				textWriter.WriteLine("Exception: {0}", badFileResult.exception);
				textWriter.Write("Base64 Contents: ");
				textWriter.WriteLine(badFileResult.failureContents ?? string.Empty);
				textWriter.WriteLine(string.Empty);
			}
		}
		catch (Exception ex)
		{
			Debug.LogFormat("Could not write bad UserProfile load log! Reason: {0}", ex.Message);
		}
	}

	public virtual void Shutdown()
	{
		foreach (UserProfile loggedInProfile in loggedInProfiles)
		{
			RequestSave(loggedInProfile, immediate: true);
		}
	}

	public virtual void SaveLoggedInUserProfiles()
	{
		foreach (UserProfile loggedInProfile in loggedInProfiles)
		{
			RequestSave(loggedInProfile);
		}
	}

	public static void Copy(UserProfile src, UserProfile dest)
	{
		dest.fileSystem = src.fileSystem;
		dest.filePath = src.filePath;
		StatSheet.Copy(src.statSheet, dest.statSheet);
		src.loadout.Copy(dest.loadout);
		dest.tutorialSprint = src.tutorialSprint;
		dest.tutorialDifficulty = src.tutorialDifficulty;
		dest.tutorialEquipment = src.tutorialEquipment;
		SaveFieldAttribute[] saveFields = UserProfile.saveFields;
		for (int i = 0; i < saveFields.Length; i++)
		{
			saveFields[i].copier(src, dest);
		}
		dest.isClaimed = false;
		dest.canSave = false;
		dest.fileName = src.fileName;
		dest.onPickupDiscovered = null;
		dest.onStatsReceived = null;
		dest.loggedIn = false;
	}

	protected void EnqueueFileOutput(FileOutput fileOutput)
	{
		lock (pendingOutputQueue)
		{
			pendingOutputQueue.Enqueue(fileOutput);
		}
	}

	private static bool ProfileNameIsReserved([NotNull] string profileName)
	{
		return string.Equals("default", profileName, StringComparison.OrdinalIgnoreCase);
	}

	[ConCommand(commandName = "user_profile_save", flags = ConVarFlags.None, helpText = "Saves the named profile to disk, if it exists.")]
	private static void CCUserProfileSave(ConCommandArgs args)
	{
		args.CheckArgumentCount(1);
		string text = args[0];
		if (ProfileNameIsReserved(text))
		{
			Debug.LogFormat("Cannot save profile \"{0}\", it is a reserved profile.", text);
			return;
		}
		UserProfile profile = PlatformSystems.saveSystem.GetProfile(text);
		if (profile == null)
		{
			Debug.LogFormat("Could not find profile \"{0}\" to save.", text);
		}
		else
		{
			profile.RequestEventualSave();
		}
	}

	[ConCommand(commandName = "user_profile_copy", flags = ConVarFlags.None, helpText = "Copies the profile named by the first argument to a new profile named by the second argument. This does not save the profile.")]
	private static void CCUserProfileCopy(ConCommandArgs args)
	{
		args.CheckArgumentCount(2);
		string text = args[0].ToLower(CultureInfo.InvariantCulture);
		string text2 = args[1].ToLower(CultureInfo.InvariantCulture);
		UserProfile profile = PlatformSystems.saveSystem.GetProfile(text);
		if (profile == null)
		{
			Debug.LogFormat("Profile {0} does not exist, so it cannot be copied.", text);
			return;
		}
		if (PlatformSystems.saveSystem.GetProfile(text2) != null)
		{
			Debug.LogFormat("Profile {0} already exists, and cannot be copied to.", text2);
			return;
		}
		UserProfile userProfile = new UserProfile();
		Copy(profile, userProfile);
		userProfile.fileSystem = profile.fileSystem ?? RoR2Application.cloudStorage;
		userProfile.filePath = "/UserProfiles/" + text2 + ".xml";
		userProfile.fileName = text2;
		userProfile.canSave = true;
		PlatformSystems.saveSystem.loadedUserProfiles[text2] = userProfile;
		SaveSystem.onAvailableUserProfilesChanged?.Invoke();
	}

	[ConCommand(commandName = "user_profile_delete", flags = ConVarFlags.None, helpText = "Unloads the named user profile and deletes it from the disk if it exists.")]
	private static void CCUserProfileDelete(ConCommandArgs args)
	{
		args.CheckArgumentCount(1);
		string text = args[0];
		if (ProfileNameIsReserved(text))
		{
			Debug.LogFormat("Cannot delete profile \"{0}\", it is a reserved profile.", text);
		}
		else
		{
			DeleteUserProfile(text);
		}
	}

	private static void DeleteUserProfile(string fileName)
	{
		fileName = fileName.ToLower(CultureInfo.InvariantCulture);
		UserProfile profile = PlatformSystems.saveSystem.GetProfile(fileName);
		if (PlatformSystems.saveSystem.loadedUserProfiles.ContainsKey(fileName))
		{
			PlatformSystems.saveSystem.loadedUserProfiles.Remove(fileName);
		}
		if (profile != null && profile.fileSystem != null)
		{
			profile.fileSystem.DeleteFile(profile.filePath);
		}
		SaveSystem.onAvailableUserProfilesChanged?.Invoke();
	}

	[ConCommand(commandName = "create_corrupted_profiles", flags = ConVarFlags.None, helpText = "Creates corrupted user profiles.")]
	private static void CCCreateCorruptedProfiles(ConCommandArgs args)
	{
		FileSystem fileSystem = RoR2Application.cloudStorage;
		WriteFile("empty", "");
		WriteFile("truncated", "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<UserProfile>\r\n");
		WriteFile("multiroot", "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<UserProfile>\r\n</UserProfile>\r\n<UserProfile>\r\n</UserProfile>");
		WriteFile("outoforder", "<?xml version=\"1.0\" encodi=\"utf-8\"ng?>\r\n<Userrofile>\r\n<UserProfile>\r\n</UserProfileProfile>\r\n</UserP>");
		void WriteFile(string fileName, string contents)
		{
			using Stream stream = fileSystem.OpenFile("/UserProfiles/" + fileName + ".xml", FileMode.Create, FileAccess.Write);
			using (TextWriter textWriter = new StreamWriter(stream))
			{
				textWriter.Write(contents.ToCharArray());
				textWriter.Flush();
			}
			stream.Flush();
		}
	}

	[ConCommand(commandName = "userprofile_test_buffer_overflow", flags = ConVarFlags.None, helpText = "")]
	private static void CCUserProfileTestBufferOverflow(ConCommandArgs args)
	{
		args.CheckArgumentCount(1);
		int num = 128;
		_ = RoR2Application.cloudStorage;
		RemoteFile remoteFile = Client.Instance.RemoteStorage.OpenFile(args[0]);
		_ = remoteFile.SizeInBytes;
		FieldInfo field = remoteFile.GetType().GetField("_sizeInBytes", BindingFlags.Instance | BindingFlags.NonPublic);
		int num2 = (int)field.GetValue(remoteFile);
		field.SetValue(remoteFile, num2 + num);
		byte[] array = remoteFile.ReadAllBytes();
		byte[] array2 = new byte[num];
		for (int i = 0; i < num; i++)
		{
			array2[i] = array[num2 + i];
		}
		GUIUtility.systemCopyBuffer = Encoding.UTF8.GetString(array2);
		field.SetValue(remoteFile, num2);
	}
}
