using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RoR2;

[RequireComponent(typeof(RawImage))]
public class SocialUserIcon : UIBehaviour
{
	private SocialUserIconBehavior behavior = SocialUserIconBehaviorFactory.Get();

	[SerializeField]
	private UserManager.AvatarSize avatarSize;

	protected override void OnEnable()
	{
		behavior.OnEnable(base.gameObject, this);
	}

	protected override void Awake()
	{
		behavior.Awake(base.gameObject, this);
	}

	protected override void OnDestroy()
	{
		behavior.OnDestroy();
	}

	public virtual void Refresh(bool shouldForceRefresh = false)
	{
		behavior.Refresh(shouldForceRefresh);
	}

	public virtual void SetFromMaster(CharacterMaster master)
	{
		behavior.SetFromMaster(master);
	}

	public void RefreshWithUser(PlatformID newUserID)
	{
		behavior.RefreshWithUser(newUserID);
	}
}
