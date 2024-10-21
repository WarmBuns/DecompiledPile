using System.Collections.Generic;
using Epic.OnlineServices;
using Epic.OnlineServices.Connect;
using Epic.OnlineServices.Friends;

namespace RoR2;

public class EOSFriendsManager
{
	public static FriendsInterface Interface;

	private static ConnectInterface _connectInterface;

	public static List<ProductUserId> FriendsProductUserIds { get; } = new List<ProductUserId>();

	private static EpicAccountId LocalUserEpicAccountId => EOSLoginManager.loggedInAuthId;

	private static ProductUserId LocalUserProductUserId => EOSLoginManager.loggedInProductId;

	public EOSFriendsManager()
	{
		Interface = EOSPlatformManager.GetPlatformInterface().GetFriendsInterface();
		_connectInterface = EOSPlatformManager.GetPlatformInterface().GetConnectInterface();
		EOSLoginManager.OnAuthLoggedIn += Initialize;
	}

	private static void Initialize(EpicAccountId obj)
	{
		QueryFriendsRefresh(null, delegate
		{
			PopulateFriendsEpicAccountIds();
		});
		AddNotifyFriendsUpdateOptions options = default(AddNotifyFriendsUpdateOptions);
		Interface.AddNotifyFriendsUpdate(ref options, null, FriendsUpdateHandler);
	}

	private static void FriendsUpdateHandler(ref OnFriendsUpdateInfo data)
	{
		switch (data.CurrentStatus)
		{
		case FriendsStatus.NotFriends:
			FriendsProductUserIds.Remove(data.TargetUserId.ToProductUserId());
			break;
		case FriendsStatus.Friends:
			QueryFriendsRefresh(null, TryAddFriend);
			break;
		}
	}

	private static void TryAddFriend(ref QueryFriendsCallbackInfo data)
	{
		ProductUserId productUserId = data.LocalUserId.ToProductUserId();
		if (productUserId != null && !FriendsProductUserIds.Contains(productUserId))
		{
			FriendsProductUserIds.Add(productUserId);
		}
	}

	private static void QueryFriendsRefresh(object clientData = null, OnQueryFriendsCallback callback = null)
	{
		QueryFriendsOptions queryFriendsOptions = default(QueryFriendsOptions);
		queryFriendsOptions.LocalUserId = LocalUserEpicAccountId;
		QueryFriendsOptions options = queryFriendsOptions;
		Interface.QueryFriends(ref options, clientData, callback);
	}

	private static void PopulateFriendsEpicAccountIds()
	{
		GetFriendsCountOptions getFriendsCountOptions = default(GetFriendsCountOptions);
		getFriendsCountOptions.LocalUserId = LocalUserEpicAccountId;
		GetFriendsCountOptions options = getFriendsCountOptions;
		int friendsCount = Interface.GetFriendsCount(ref options);
		Utf8String[] array = new Utf8String[friendsCount];
		EpicAccountId[] array2 = new EpicAccountId[friendsCount];
		for (int i = 0; i < friendsCount; i++)
		{
			GetFriendAtIndexOptions getFriendAtIndexOptions = default(GetFriendAtIndexOptions);
			getFriendAtIndexOptions.Index = i;
			getFriendAtIndexOptions.LocalUserId = LocalUserEpicAccountId;
			GetFriendAtIndexOptions options2 = getFriendAtIndexOptions;
			EpicAccountId friendAtIndex = Interface.GetFriendAtIndex(ref options2);
			array[i] = friendAtIndex.ToString();
			array2[i] = friendAtIndex;
		}
		QueryExternalAccountMappingsOptions queryExternalAccountMappingsOptions = default(QueryExternalAccountMappingsOptions);
		queryExternalAccountMappingsOptions.AccountIdType = ExternalAccountType.Epic;
		queryExternalAccountMappingsOptions.ExternalAccountIds = array;
		queryExternalAccountMappingsOptions.LocalUserId = LocalUserProductUserId;
		QueryExternalAccountMappingsOptions options3 = queryExternalAccountMappingsOptions;
		_connectInterface.QueryExternalAccountMappings(ref options3, array2, PopulateFriendsProductUserId);
	}

	private static void PopulateFriendsProductUserId(ref QueryExternalAccountMappingsCallbackInfo data)
	{
		if (data.ResultCode == Result.Success && data.ClientData is EpicAccountId[] array)
		{
			EpicAccountId[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				ProductUserId productUserId = array2[i].ToProductUserId();
				FriendsProductUserIds.Add(productUserId);
				(PlatformSystems.userManager as UserManagerEOS)?.QueryForDisplayNames(productUserId);
			}
			FriendsProductUserIds.Add(EOSLoginManager.loggedInProductId);
			(PlatformSystems.userManager as UserManagerEOS)?.QueryForDisplayNames(EOSLoginManager.loggedInProductId);
		}
	}
}
