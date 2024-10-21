using System;
using UnityEngine;

namespace RoR2;

public class UserManagerDummy : UserManager
{
	public override void GetAvatar(PlatformID userID, GameObject requestSender, Texture2D tex, AvatarSize size, Action<Texture2D> onRecieved)
	{
		onRecieved?.Invoke(null);
	}
}
