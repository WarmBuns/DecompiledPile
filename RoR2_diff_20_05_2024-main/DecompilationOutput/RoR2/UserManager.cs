using System;
using Rewired;
using RoR2.ConVar;
using UnityEngine;

namespace RoR2;

public abstract class UserManager
{
	public enum AvatarSize
	{
		Small,
		Medium,
		Large
	}

	public enum PlatformRestriction
	{
		VoiceChat,
		TextChat,
		Multiplayer,
		Age
	}

	public static BoolConVar P_UseSocialIcon = new BoolConVar("UseSocialIconFlag", ConVarFlags.Archive, "1", "A per-platform flag that indicates whether we display user icons or not.");

	public static event Action<int, Player> OnControllerAssigned;

	public static event Action<int, Player> OnControllerUnassigned;

	public static event Action OnUserStateChanged;

	public static event Action OnDisplayNameMappingComplete;

	internal void InvokeDisplayMappingCompleteAction()
	{
		UserManager.OnDisplayNameMappingComplete?.Invoke();
	}

	public abstract void GetAvatar(PlatformID userID, GameObject requestSender, Texture2D tex, AvatarSize size, Action<Texture2D> onRecieved);

	public virtual bool GetRestricted(PlatformID userID)
	{
		return false;
	}

	protected static Texture2D BuildTexture(Texture2D generatedTexture, int width, int height)
	{
		if ((bool)generatedTexture && (generatedTexture.width != width || generatedTexture.height != height))
		{
			generatedTexture.Reinitialize(width, height);
		}
		if (generatedTexture == null)
		{
			generatedTexture = new Texture2D(width, height);
		}
		return generatedTexture;
	}

	public virtual string GetUserDisplayName(PlatformID other)
	{
		return string.Empty;
	}

	public virtual bool ShouldHideOtherDisplayNames()
	{
		return false;
	}

	public virtual bool ShouldAlwaysUseCurrentDisplayName()
	{
		return false;
	}

	public virtual int GetUserID()
	{
		return 0;
	}

	public virtual string GetUserName()
	{
		return "";
	}

	public virtual string GetUserName(PlatformID id)
	{
		return "";
	}

	protected void NotifyUserStateChanged()
	{
		UserManager.OnUserStateChanged?.Invoke();
	}

	public static void NotifyControllerAssigned(int slot, Player player)
	{
		UserManager.OnControllerAssigned?.Invoke(slot, player);
	}

	public static void NotifyControllerUnassigned(int slot, Player player)
	{
		UserManager.OnControllerUnassigned?.Invoke(slot, player);
	}

	public virtual void EnableUserControllerHandling()
	{
	}

	public virtual bool CurrentUserHasRestriction(PlatformRestriction restriction, Action callback = null)
	{
		return false;
	}

	public virtual bool CurrentUserHasAgeRestriction()
	{
		return false;
	}

	public virtual void DisplayRestrictionMessage()
	{
	}
}
