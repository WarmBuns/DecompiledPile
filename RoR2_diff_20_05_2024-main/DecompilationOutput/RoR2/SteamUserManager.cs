using System;
using Facepunch.Steamworks;
using UnityEngine;

namespace RoR2;

public class SteamUserManager : UserManager
{
	private string LocalUserName = string.Empty;

	public void InitializeUserManager()
	{
		LocalUserManager.onUserSignIn += OnUserSignIn;
	}

	public void OnUserSignIn(LocalUser localUser)
	{
		LocalUserName = localUser.userProfile.name;
	}

	public override string GetUserName()
	{
		LocalUser localUser = LocalUserManager.FindLocalUser(0);
		if (localUser != null)
		{
			return localUser.userProfile.name;
		}
		if (Client.Instance != null)
		{
			return Client.Instance.Username;
		}
		return base.GetUserName();
	}

	public override void GetAvatar(PlatformID id, GameObject sender, Texture2D cachedTexture, AvatarSize size, Action<Texture2D> onRecieved)
	{
		GetSteamAvatar(id, sender, cachedTexture, size, onRecieved);
	}

	public static void GetSteamAvatar(PlatformID id, GameObject sender, Texture2D cachedTexture, AvatarSize size, Action<Texture2D> onRecieved)
	{
		ulong iD = id.ID;
		Client instance = Client.Instance;
		Friends.AvatarSize size2 = size switch
		{
			AvatarSize.Small => Friends.AvatarSize.Small, 
			AvatarSize.Medium => Friends.AvatarSize.Medium, 
			_ => Friends.AvatarSize.Large, 
		};
		if (instance == null)
		{
			return;
		}
		Image cachedAvatar = instance.Friends.GetCachedAvatar(size2, iD);
		if (cachedAvatar != null)
		{
			OnSteamAvatarReceived(cachedAvatar, sender, cachedTexture, onRecieved);
			return;
		}
		Action<Image> callback = delegate(Image x)
		{
			OnSteamAvatarReceived(x, sender, cachedTexture, onRecieved);
		};
		instance.Friends.GetAvatar(size2, iD, callback);
	}

	private static void OnSteamAvatarReceived(Image image, GameObject sender, Texture2D tex, Action<Texture2D> onRecieved)
	{
		if (image == null || sender == null)
		{
			return;
		}
		int width = image.Width;
		int height = image.Height;
		tex = UserManager.BuildTexture(tex, width, height);
		byte[] data = image.Data;
		Color32[] array = new Color32[data.Length / 4];
		for (int i = 0; i < height; i++)
		{
			int num = height - 1 - i;
			for (int j = 0; j < width; j++)
			{
				int num2 = (i * width + j) * 4;
				array[num * width + j] = new Color32(data[num2], data[num2 + 1], data[num2 + 2], data[num2 + 3]);
			}
		}
		if ((bool)tex)
		{
			tex.SetPixels32(array);
			tex.Apply();
		}
		onRecieved(tex);
	}
}
