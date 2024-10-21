using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RoR2.UI;

public class AchievementNotificationPanel : MonoBehaviour
{
	private static readonly List<AchievementNotificationPanel> instancesList = new List<AchievementNotificationPanel>();

	public Image achievementIconImage;

	public TextMeshProUGUI achievementName;

	public TextMeshProUGUI achievementDescription;

	public GameObject rewardArea;

	public TextMeshProUGUI rewardAmount;

	public UnityEvent onStart;

	private void Awake()
	{
		instancesList.Add(this);
		onStart.Invoke();
	}

	private void OnDestroy()
	{
		instancesList.Remove(this);
	}

	public void SetAchievementDef(AchievementDef achievementDef)
	{
		achievementIconImage.sprite = achievementDef.GetAchievedIcon();
		achievementName.text = Language.GetString(achievementDef.nameToken);
		achievementDescription.text = Language.GetString(achievementDef.descriptionToken);
		if (achievementDef.lunarCoinReward != 0)
		{
			rewardArea.SetActive(value: true);
			rewardAmount.text = achievementDef.GetReward();
		}
	}

	[CanBeNull]
	private static Canvas GetUserCanvas([NotNull] LocalUser localUser)
	{
		if ((bool)Run.instance)
		{
			CameraRigController cameraRigController = localUser.cameraRigController;
			if (!cameraRigController)
			{
				return null;
			}
			HUD hud = cameraRigController.hud;
			if (!hud)
			{
				return null;
			}
			return hud.mainContainerCanvas;
		}
		return RoR2Application.instance.mainCanvas;
	}

	private static bool IsAppropriateTimeToDisplayUserAchievementNotification(LocalUser localUser)
	{
		return !GameOverController.instance;
	}

	private static void DispatchAchievementNotification(Canvas canvas, AchievementDef achievementDef)
	{
		Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/UI/AchievementNotificationPanel"), canvas.transform).GetComponent<AchievementNotificationPanel>().SetAchievementDef(achievementDef);
		Util.PlaySound(achievementDef.GetAchievementSoundString(), RoR2Application.instance.gameObject);
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		RoR2Application.onFixedUpdate += StaticFixedUpdate;
	}

	private static void StaticFixedUpdate()
	{
		foreach (LocalUser readOnlyLocalUsers in LocalUserManager.readOnlyLocalUsersList)
		{
			if (!readOnlyLocalUsers.userProfile.hasUnviewedAchievement)
			{
				continue;
			}
			Canvas canvas = GetUserCanvas(readOnlyLocalUsers);
			if (canvas == null || instancesList.Any((AchievementNotificationPanel instance) => instance.transform.parent == canvas.transform) || !IsAppropriateTimeToDisplayUserAchievementNotification(readOnlyLocalUsers))
			{
				continue;
			}
			string text = readOnlyLocalUsers.userProfile.PopNextUnviewedAchievementName();
			if (text != null)
			{
				AchievementDef achievementDef = AchievementManager.GetAchievementDef(text);
				if (achievementDef != null)
				{
					DispatchAchievementNotification(canvas, achievementDef);
				}
			}
		}
	}
}
