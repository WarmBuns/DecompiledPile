using RoR2;
using UnityEngine;
using UnityEngine.UI;

public class SocialUserIconBehavior
{
	protected enum SourceType
	{
		Local,
		Network
	}

	protected RawImage rawImageComponent;

	protected Texture2D generatedTexture;

	protected PlatformID userID;

	protected SourceType sourceType;

	protected GameObject gameObject;

	protected MonoBehaviour monoBehaviour;

	protected UserManager.AvatarSize avatarSize;

	protected Texture defaultTexture => LegacyResourcesAPI.Load<Texture>("Textures/UI/texDefaultSocialUserIcon");

	public void Awake(GameObject go, MonoBehaviour mb)
	{
		gameObject = go;
		monoBehaviour = mb;
		OnAwakeBehavior();
	}

	public void OnEnable(GameObject go, MonoBehaviour mb)
	{
		gameObject = go;
		monoBehaviour = mb;
		OnEnableBehavior();
	}

	public void OnDestroy()
	{
		OnDestroyBehavior();
		gameObject = null;
		monoBehaviour = null;
	}

	protected virtual void OnAwakeBehavior()
	{
		rawImageComponent = gameObject.GetComponent<RawImage>();
		rawImageComponent.texture = defaultTexture;
		if (!UserManager.P_UseSocialIcon.value)
		{
			gameObject.SetActive(value: false);
		}
	}

	protected virtual void OnEnableBehavior()
	{
	}

	protected virtual void OnDestroyBehavior()
	{
	}

	public virtual void Refresh(bool shouldForceRefresh = false)
	{
		if (PlatformSystems.lobbyManager.HasMPLobbyFeature(MPLobbyFeatures.UserIcon))
		{
			PlatformSystems.userManager.GetAvatar(userID, gameObject, generatedTexture, avatarSize, HandleNewTexture);
			if (!generatedTexture && rawImageComponent != null)
			{
				rawImageComponent.texture = defaultTexture;
			}
		}
	}

	public virtual void SetFromMaster(CharacterMaster master)
	{
		if (!PlatformSystems.lobbyManager.HasMPLobbyFeature(MPLobbyFeatures.UserIcon))
		{
			return;
		}
		if ((bool)master)
		{
			PlayerCharacterMasterController component = master.GetComponent<PlayerCharacterMasterController>();
			if ((bool)component)
			{
				NetworkUser networkUser = component.networkUser;
				RefreshWithUser(new PlatformID(networkUser.id.value));
				return;
			}
		}
		userID = default(PlatformID);
		sourceType = SourceType.Local;
		Refresh();
	}

	public virtual void RefreshWithUser(PlatformID newUserID)
	{
		if (PlatformSystems.lobbyManager.HasMPLobbyFeature(MPLobbyFeatures.UserIcon) && (sourceType != SourceType.Network || !newUserID.Equals(userID)))
		{
			sourceType = SourceType.Network;
			userID = newUserID;
			Refresh();
		}
	}

	protected virtual void HandleNewTexture(Texture2D tex)
	{
		if ((bool)tex)
		{
			generatedTexture = tex;
			if (rawImageComponent != null)
			{
				rawImageComponent.texture = tex;
			}
		}
	}
}
