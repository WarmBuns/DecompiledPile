using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using RoR2.Achievements;
using UnityEngine;

namespace RoR2;

public static class AchievementManager
{
	private struct AchievementSortPair
	{
		public int score;

		public AchievementDef achievementDef;
	}

	public struct Enumerator : IEnumerator<AchievementDef>, IEnumerator, IDisposable
	{
		private int position;

		public AchievementDef Current => achievementDefs[position];

		object IEnumerator.Current => Current;

		public bool MoveNext()
		{
			position++;
			return position < achievementDefs.Length;
		}

		public void Reset()
		{
			position = -1;
		}

		void IDisposable.Dispose()
		{
		}
	}

	private static readonly Dictionary<LocalUser, UserAchievementManager> userToManagerMap = new Dictionary<LocalUser, UserAchievementManager>();

	public static ResourceAvailability availability;

	private static readonly Queue<Action> taskQueue = new Queue<Action>();

	private static readonly Dictionary<string, AchievementDef> achievementNamesToDefs = new Dictionary<string, AchievementDef>();

	private static readonly List<string> achievementIdentifiers = new List<string>();

	public static readonly ReadOnlyCollection<string> readOnlyAchievementIdentifiers = achievementIdentifiers.AsReadOnly();

	private static AchievementDef[] achievementDefs;

	private static AchievementDef[] serverAchievementDefs;

	public static readonly GenericStaticEnumerable<AchievementDef, Enumerator> allAchievementDefs;

	public static int achievementCount => achievementDefs.Length;

	public static int serverAchievementCount => serverAchievementDefs.Length;

	public static event Action onAchievementsRegistered;

	public static UserAchievementManager GetUserAchievementManager([NotNull] LocalUser user)
	{
		userToManagerMap.TryGetValue(user, out var value);
		return value;
	}

	public static IEnumerator DoInit()
	{
		yield return CollectAchievementDefs(achievementNamesToDefs);
		LocalUserManager.onUserSignIn += delegate(LocalUser localUser)
		{
			if (localUser.userProfile.canSave)
			{
				UserAchievementManager userAchievementManager = new UserAchievementManager();
				userAchievementManager.OnInstall(localUser);
				userToManagerMap[localUser] = userAchievementManager;
			}
		};
		LocalUserManager.onUserSignOut += delegate(LocalUser localUser)
		{
			if (userToManagerMap.TryGetValue(localUser, out var value))
			{
				value.OnUninstall();
				userToManagerMap.Remove(localUser);
			}
		};
		RoR2Application.onUpdate += delegate
		{
			foreach (KeyValuePair<LocalUser, UserAchievementManager> item in userToManagerMap)
			{
				item.Value.Update();
			}
		};
		availability.MakeAvailable();
	}

	public static void AddTask(Action action)
	{
		taskQueue.Enqueue(action);
	}

	public static void ProcessTasks()
	{
		while (taskQueue.Count > 0)
		{
			taskQueue.Dequeue()();
		}
	}

	public static AchievementDef GetAchievementDef(string achievementIdentifier)
	{
		if (achievementNamesToDefs.TryGetValue(achievementIdentifier, out var value))
		{
			return value;
		}
		return null;
	}

	public static AchievementDef GetAchievementDef(AchievementIndex index)
	{
		if (index.intValue >= 0 && index.intValue < achievementDefs.Length)
		{
			return achievementDefs[index.intValue];
		}
		return null;
	}

	public static AchievementDef GetAchievementDef(ServerAchievementIndex index)
	{
		if (index.intValue >= 0 && index.intValue < serverAchievementDefs.Length)
		{
			return serverAchievementDefs[index.intValue];
		}
		return null;
	}

	public static AchievementDef GetAchievementDefFromUnlockable(string unlockableRewardIdentifier)
	{
		for (int i = 0; i < achievementDefs.Length; i++)
		{
			if (achievementDefs[i].unlockableRewardIdentifier == unlockableRewardIdentifier)
			{
				return achievementDefs[i];
			}
		}
		return null;
	}

	public static IEnumerator CollectAchievementDefs(Dictionary<string, AchievementDef> map)
	{
		List<AchievementDef> achievementDefsList = new List<AchievementDef>();
		map.Clear();
		List<Assembly> list = new List<Assembly>();
		if (RoR2Application.isModded)
		{
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (Assembly item in assemblies)
			{
				list.Add(item);
			}
		}
		else
		{
			list.Add(typeof(BaseAchievement).Assembly);
		}
		foreach (Assembly item2 in list)
		{
			Type[] types;
			try
			{
				types = item2.GetTypes();
			}
			catch (ReflectionTypeLoadException ex)
			{
				Debug.LogError($"CollectAchievementDefs:  {ex}");
				types = ex.Types;
				if (types == null)
				{
					continue;
				}
			}
			catch (Exception arg)
			{
				Debug.LogError($"CollectAchievementDefs:  {arg}");
				continue;
			}
			foreach (Type item3 in from type in types
				where type != null && type.IsSubclassOf(typeof(BaseAchievement))
				orderby type.Name
				select type)
			{
				RegisterAchievementAttribute registerAchievementAttribute = (RegisterAchievementAttribute)item3.GetCustomAttributes(inherit: false).FirstOrDefault((object v) => v is RegisterAchievementAttribute);
				if (registerAchievementAttribute != null)
				{
					if (map.ContainsKey(registerAchievementAttribute.identifier))
					{
						Debug.LogErrorFormat("Class {0} attempted to register as achievement {1}, but class {2} has already registered as that achievement.", item3.FullName, registerAchievementAttribute.identifier, achievementNamesToDefs[registerAchievementAttribute.identifier].type.FullName);
						continue;
					}
					UnlockableDef unlockableDef = UnlockableCatalog.GetUnlockableDef(registerAchievementAttribute.unlockableRewardIdentifier);
					AchievementDef achievementDef2 = new AchievementDef
					{
						identifier = registerAchievementAttribute.identifier,
						unlockableRewardIdentifier = registerAchievementAttribute.unlockableRewardIdentifier,
						prerequisiteAchievementIdentifier = registerAchievementAttribute.prerequisiteAchievementIdentifier,
						nameToken = "ACHIEVEMENT_" + registerAchievementAttribute.identifier.ToUpper(CultureInfo.InvariantCulture) + "_NAME",
						descriptionToken = "ACHIEVEMENT_" + registerAchievementAttribute.identifier.ToUpper(CultureInfo.InvariantCulture) + "_DESCRIPTION",
						type = item3,
						serverTrackerType = registerAchievementAttribute.serverTrackerType,
						lunarCoinReward = registerAchievementAttribute.lunarCoinReward
					};
					if ((bool)unlockableDef && (bool)unlockableDef.achievementIcon)
					{
						achievementDef2.SetAchievedIcon(unlockableDef.achievementIcon);
					}
					else
					{
						achievementDef2.iconPath = "Textures/AchievementIcons/tex" + registerAchievementAttribute.identifier + "Icon";
						achievementDef2.PreloadIcon();
					}
					achievementIdentifiers.Add(registerAchievementAttribute.identifier);
					map.Add(registerAchievementAttribute.identifier, achievementDef2);
					achievementDefsList.Add(achievementDef2);
					if (unlockableDef != null)
					{
						unlockableDef.getHowToUnlockString = () => Language.GetStringFormatted("UNLOCK_VIA_ACHIEVEMENT_FORMAT", Language.GetString(achievementDef2.nameToken), Language.GetString(achievementDef2.descriptionToken));
						unlockableDef.getUnlockedString = () => Language.GetStringFormatted("UNLOCKED_FORMAT", Language.GetString(achievementDef2.nameToken), Language.GetString(achievementDef2.descriptionToken));
					}
				}
				yield return null;
			}
		}
		achievementDefs = achievementDefsList.ToArray();
		SortAchievements(achievementDefs);
		serverAchievementDefs = achievementDefs.Where((AchievementDef achievementDef) => achievementDef.serverTrackerType != null).ToArray();
		for (int j = 0; j < achievementDefs.Length; j++)
		{
			achievementDefs[j].index = new AchievementIndex
			{
				intValue = j
			};
		}
		for (int k = 0; k < serverAchievementDefs.Length; k++)
		{
			serverAchievementDefs[k].serverIndex = new ServerAchievementIndex
			{
				intValue = k
			};
		}
		for (int l = 0; l < achievementIdentifiers.Count; l++)
		{
			string currentAchievementIdentifier = achievementIdentifiers[l];
			map[currentAchievementIdentifier].childAchievementIdentifiers = achievementIdentifiers.Where((string v) => map[v].prerequisiteAchievementIdentifier == currentAchievementIdentifier).ToArray();
		}
		AchievementManager.onAchievementsRegistered?.Invoke();
		yield return null;
	}

	private static void SortAchievements(AchievementDef[] achievementDefsArray)
	{
		AchievementSortPair[] array = new AchievementSortPair[achievementDefsArray.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new AchievementSortPair
			{
				score = UnlockableCatalog.GetUnlockableSortScore(achievementDefsArray[i].unlockableRewardIdentifier),
				achievementDef = achievementDefsArray[i]
			};
		}
		Array.Sort(array, (AchievementSortPair a, AchievementSortPair b) => a.score - b.score);
		for (int j = 0; j < array.Length; j++)
		{
			achievementDefsArray[j] = array[j].achievementDef;
		}
	}

	public static Enumerator GetEnumerator()
	{
		return default(Enumerator);
	}
}
