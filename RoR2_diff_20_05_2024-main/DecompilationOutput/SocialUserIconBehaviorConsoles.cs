using System.Collections;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SocialUserIconBehaviorConsoles : SocialUserIconBehavior
{
	private bool UserIconIsLoad;

	private bool loadAvatarIsDone;

	private Texture2D pointerOfTexture;

	private HGButton button;

	private bool blockToClick;

	private WaitForSeconds waiteSecond = new WaitForSeconds(0.3f);

	private Image outline;

	public override void Refresh(bool shouldForceRefresh = false)
	{
		if (!(!PlatformSystems.lobbyManager.HasMPLobbyFeature(MPLobbyFeatures.UserIcon) | (UserIconIsLoad && !shouldForceRefresh)))
		{
			PlatformSystems.userManager.GetAvatar(userID, gameObject, generatedTexture, avatarSize, HandleNewTexture);
			UserIconIsLoad = true;
			loadAvatarIsDone = false;
			if ((bool)gameObject && gameObject.activeInHierarchy)
			{
				monoBehaviour.StartCoroutine(UserIconTextureChangeCoroutine());
			}
			if (!generatedTexture && rawImageComponent != null)
			{
				rawImageComponent.texture = base.defaultTexture;
			}
		}
	}

	public override void RefreshWithUser(PlatformID newUserID)
	{
		if (PlatformSystems.lobbyManager.HasMPLobbyFeature(MPLobbyFeatures.UserIcon) && (sourceType != SourceType.Network || !newUserID.Equals(userID)))
		{
			sourceType = SourceType.Network;
			UserIconIsLoad = false;
			userID = newUserID;
			InitButtonToClickeble();
			Refresh();
		}
	}

	protected override void HandleNewTexture(Texture2D tex)
	{
		if (tex == null || rawImageComponent == null)
		{
			UserIconIsLoad = false;
			if (this != null && (bool)gameObject)
			{
				monoBehaviour.StopCoroutine(UserIconTextureChangeCoroutine());
			}
		}
		else
		{
			pointerOfTexture = tex;
			generatedTexture = pointerOfTexture;
			rawImageComponent.texture = pointerOfTexture;
			loadAvatarIsDone = true;
		}
	}

	protected override void OnAwakeBehavior()
	{
		rawImageComponent = gameObject.GetComponent<RawImage>();
		rawImageComponent.texture = base.defaultTexture;
		rawImageComponent.rectTransform.sizeDelta = new Vector2(32f, 32f);
		if (PreGameController.instance != null)
		{
			UserIconIsLoad = false;
		}
		if (!UserManager.P_UseSocialIcon.value)
		{
			gameObject.SetActive(value: false);
		}
	}

	protected override void OnEnableBehavior()
	{
		if (UserIconIsLoad)
		{
			monoBehaviour.StartCoroutine(UserIconTextureChangeCoroutine());
		}
		if (userID != default(PlatformID))
		{
			InitButtonToClickeble();
		}
	}

	private void InitButtonToClickeble()
	{
		if (!(button != null) && !(gameObject == null))
		{
			button = gameObject.AddComponent<HGButton>();
			button.onClick.RemoveAllListeners();
			button.onClick.AddListener(OnClickToSocialUserIcon);
			outline = gameObject.transform.GetComponentInChildren<Image>();
			if (outline != null)
			{
				outline.rectTransform.offsetMax = new Vector2(2f, 2f);
				outline.rectTransform.offsetMin = new Vector2(-2f, -2f);
				button.onSelect = new UnityEvent();
				button.onSelect.AddListener(Selected);
				button.onDeselect = new UnityEvent();
				button.onDeselect.AddListener(Deselected);
			}
		}
	}

	private void Selected()
	{
		outline.enabled = true;
	}

	private void Deselected()
	{
		outline.enabled = false;
	}

	private void OnClickToSocialUserIcon()
	{
		if (!blockToClick)
		{
			blockToClick = true;
		}
	}

	private void UnblockToCkick()
	{
		if (!monoBehaviour.isActiveAndEnabled)
		{
			blockToClick = false;
		}
		else
		{
			monoBehaviour.StartCoroutine(ButtonUpAcrivityCoroutine());
		}
	}

	private IEnumerator ButtonUpAcrivityCoroutine()
	{
		yield return waiteSecond;
		blockToClick = false;
	}

	private IEnumerator UserIconTextureChangeCoroutine()
	{
		while (!loadAvatarIsDone)
		{
			yield return null;
		}
		generatedTexture = pointerOfTexture;
		rawImageComponent.texture = pointerOfTexture;
	}
}
