using System;
using System.Collections.Generic;
using System.Linq;
using Epic.OnlineServices;
using Epic.OnlineServices.Connect;
using Epic.OnlineServices.UserInfo;
using UnityEngine;

namespace RoR2;

public class UserManagerEOS : UserManager
{
	private string LocalUserName = string.Empty;

	private static Dictionary<ProductUserId, ExternalAccountInfo> UserIDsToAccountInfo = new Dictionary<ProductUserId, ExternalAccountInfo>();

	private static List<ProductUserId> IDQueryList = new List<ProductUserId>();

	public void InitializeUserManager()
	{
		EOSLoginManager.OnAuthLoggedIn += TryGetLogin;
		LocalUserManager.onUserSignIn += OnUserSignIn;
	}

	public void OnUserSignIn(LocalUser localUser)
	{
		LocalUserName = localUser.userProfile.name;
	}

	public override void GetAvatar(PlatformID userID, GameObject requestSender, Texture2D tex, AvatarSize size, Action<Texture2D> onRecieved)
	{
		ulong result = 0uL;
		ProductUserId productUserID = userID.productUserID;
		if (UserIDsToAccountInfo.ContainsKey(productUserID) && ulong.TryParse(UserIDsToAccountInfo[productUserID].AccountId, out result))
		{
			SteamUserManager.GetSteamAvatar(new PlatformID(result), requestSender, tex, size, onRecieved);
		}
	}

	public override string GetUserName()
	{
		LocalUser localUser = LocalUserManager.FindLocalUser(0);
		if (localUser != null)
		{
			return localUser.userProfile.name;
		}
		return LocalUserName;
	}

	public override int GetUserID()
	{
		return -1;
	}

	public void QueryForDisplayNames(PlatformID[] ids, Action callback = null)
	{
		if (ids != null)
		{
			QueryForDisplayNames(ids.Select((PlatformID x) => x.productUserID).ToArray(), callback);
		}
	}

	public void QueryForDisplayNames(ProductUserId productId, Action callback = null)
	{
		QueryForDisplayNames(new ProductUserId[1] { productId }, callback);
	}

	public void QueryForDisplayNames(ProductUserId[] productIds, Action callback = null)
	{
		QueryProductUserIdMappingsOptions queryProductUserIdMappingsOptions = default(QueryProductUserIdMappingsOptions);
		queryProductUserIdMappingsOptions.LocalUserId = EOSLoginManager.loggedInProductId;
		queryProductUserIdMappingsOptions.ProductUserIds = productIds;
		QueryProductUserIdMappingsOptions options = queryProductUserIdMappingsOptions;
		IDQueryList.AddRange(productIds);
		IDQueryList = IDQueryList.Distinct().ToList();
		EOSPlatformManager.GetPlatformInterface().GetConnectInterface().QueryProductUserIdMappings(ref options, productIds, delegate(ref QueryProductUserIdMappingsCallbackInfo data)
		{
			OnQueryProductUserIdMappingsComplete(data);
			callback?.Invoke();
		});
	}

	private bool GetUserNameFromExternalAccountType(ProductUserId id, uint index, out ExternalAccountInfo accountInfo)
	{
		accountInfo = default(ExternalAccountInfo);
		CopyProductUserExternalAccountByIndexOptions copyProductUserExternalAccountByIndexOptions = default(CopyProductUserExternalAccountByIndexOptions);
		copyProductUserExternalAccountByIndexOptions.TargetUserId = id;
		copyProductUserExternalAccountByIndexOptions.ExternalAccountInfoIndex = index;
		CopyProductUserExternalAccountByIndexOptions options = copyProductUserExternalAccountByIndexOptions;
		if (EOSPlatformManager.GetPlatformInterface().GetConnectInterface().CopyProductUserExternalAccountByIndex(ref options, out var outExternalAccountInfo) == Result.Success)
		{
			accountInfo = outExternalAccountInfo.Value;
			return true;
		}
		return false;
	}

	private void OnQueryProductUserIdMappingsComplete(QueryProductUserIdMappingsCallbackInfo data)
	{
		if (data.GetResultCode() != Result.Success)
		{
			return;
		}
		List<ProductUserId> list = new List<ProductUserId>(IDQueryList);
		foreach (ProductUserId iDQuery in IDQueryList)
		{
			if (GetUserNameFromExternalAccountType(iDQuery, 1u, out var accountInfo))
			{
				UserIDsToAccountInfo[iDQuery] = accountInfo;
				list.Remove(iDQuery);
			}
			else if (GetUserNameFromExternalAccountType(iDQuery, 0u, out accountInfo))
			{
				UserIDsToAccountInfo[iDQuery] = accountInfo;
				list.Remove(iDQuery);
			}
		}
		IDQueryList = list;
		if (IDQueryList.Count == 0)
		{
			InvokeDisplayMappingCompleteAction();
		}
	}

	public override string GetUserDisplayName(PlatformID other)
	{
		return GetUserDisplayName(other, null);
	}

	public string GetUserDisplayName(PlatformID other, Action callback)
	{
		if (UserIDsToAccountInfo.ContainsKey(other.productUserID))
		{
			return UserIDsToAccountInfo[other.productUserID].DisplayName;
		}
		if (!string.IsNullOrEmpty(other.productUserID.ToString()))
		{
			QueryForDisplayNames(new PlatformID[1] { other }, callback);
		}
		return string.Empty;
	}

	private void TryGetLogin(EpicAccountId obj)
	{
		UserInfoInterface userInfoInterface = EOSPlatformManager.GetPlatformInterface().GetUserInfoInterface();
		if (userInfoInterface != null && EOSLoginManager.loggedInAuthId != null)
		{
			QueryUserInfoOptions queryUserInfoOptions = default(QueryUserInfoOptions);
			queryUserInfoOptions.LocalUserId = obj;
			queryUserInfoOptions.TargetUserId = obj;
			QueryUserInfoOptions options = queryUserInfoOptions;
			userInfoInterface.QueryUserInfo(ref options, null, OnQueryUserInfo);
		}
	}

	private void OnQueryUserInfo(ref QueryUserInfoCallbackInfo data)
	{
		if (data.ResultCode == Result.Success)
		{
			UserInfoInterface userInfoInterface = EOSPlatformManager.GetPlatformInterface().GetUserInfoInterface();
			if (userInfoInterface != null)
			{
				CopyUserInfoOptions copyUserInfoOptions = default(CopyUserInfoOptions);
				copyUserInfoOptions.LocalUserId = EOSLoginManager.loggedInAuthId;
				copyUserInfoOptions.TargetUserId = EOSLoginManager.loggedInAuthId;
				CopyUserInfoOptions options = copyUserInfoOptions;
				userInfoInterface.CopyUserInfo(ref options, out var outUserInfo);
				LocalUserName = outUserInfo.Value.DisplayName;
			}
		}
	}
}
