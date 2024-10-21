using System;
using Epic.OnlineServices;
using Epic.OnlineServices.Auth;
using Epic.OnlineServices.Connect;
using Facepunch.Steamworks;
using RoR2.ConVar;

namespace RoR2;

public class EOSLoginManager
{
	public enum EOSLoginState
	{
		None,
		AttemptingLogin,
		AttemptingLink,
		FailedLogin,
		FailedLink,
		Success
	}

	public static EOSLoginManager instance = null;

	public static readonly BoolConVar cvLinkEOSAccount = new BoolConVar("eos_link_account", ConVarFlags.None, "0", "trigger the account linking process");

	public LoginCredentialType _loginCredentialType = LoginCredentialType.ExchangeCode;

	public string _loginCredentialId;

	public string _loginCredentialToken;

	private bool showCredentialPopup;

	public static EpicAccountId loggedInAuthId = null;

	public static ProductUserId loggedInProductId = null;

	public static PlatformID loggedInUserID;

	private ConnectInterface connectInterface;

	private static string ticket;

	public static EOSLoginState loginState { get; private set; } = EOSLoginState.None;

	public static event Action<EpicAccountId> OnAuthLoggedIn;

	public static event Action<ProductUserId> OnConnectLoggedIn;

	public static bool IsWaitingOnLogin()
	{
		return loginState != EOSLoginState.Success;
	}

	public void TryLogin()
	{
		instance = this;
		RoR2Application.onUpdate += OnUpdate;
		ExternalAuthLogin_Steam();
	}

	private void OnUpdate()
	{
		if (cvLinkEOSAccount.value && !IsWaitingOnLogin())
		{
			StartSteamLoginWithDefaultOptions(attemptAccountLink: true);
		}
	}

	private void OnSteamworksInitialized()
	{
		UpdateSessionAuthTicket();
		SteamworksClientManager.onLoaded -= OnSteamworksInitialized;
	}

	private void UpdateSessionAuthTicket()
	{
		ticket = Client.Instance.Auth.GetAuthSessionTicket().Data.ToHexString();
		StartSteamLoginWithDefaultOptions();
	}

	public void ExternalAuthLogin_Steam()
	{
		if (Client.Instance != null)
		{
			OnSteamworksInitialized();
		}
		else
		{
			SteamworksClientManager.onLoaded += OnSteamworksInitialized;
		}
	}

	public void StartSteamLoginWithDefaultOptions(bool attemptAccountLink = false)
	{
		Epic.OnlineServices.Auth.LoginOptions loginOptions = default(Epic.OnlineServices.Auth.LoginOptions);
		loginOptions.Credentials = new Epic.OnlineServices.Auth.Credentials
		{
			Type = LoginCredentialType.ExternalAuth,
			Token = ticket,
			ExternalType = ExternalCredentialType.SteamSessionTicket
		};
		loginOptions.ScopeFlags = AuthScopeFlags.BasicProfile | AuthScopeFlags.FriendsList | AuthScopeFlags.Presence;
		Epic.OnlineServices.Auth.LoginOptions loginOptions2 = loginOptions;
		StartEGSLogin(loginOptions2, attemptAccountLink);
	}

	private void StartEGSLoginWithDefaultOptions()
	{
		Epic.OnlineServices.Auth.LoginOptions loginOptions = default(Epic.OnlineServices.Auth.LoginOptions);
		loginOptions.Credentials = new Epic.OnlineServices.Auth.Credentials
		{
			Type = _loginCredentialType,
			Id = _loginCredentialId,
			Token = _loginCredentialToken
		};
		loginOptions.ScopeFlags = AuthScopeFlags.BasicProfile | AuthScopeFlags.FriendsList | AuthScopeFlags.Presence;
		Epic.OnlineServices.Auth.LoginOptions loginOptions2 = loginOptions;
		StartEGSLogin(loginOptions2);
	}

	private void StartEGSLogin(Epic.OnlineServices.Auth.LoginOptions loginOptions, bool attemptLink = false)
	{
		loginState = EOSLoginState.AttemptingLogin;
		AuthInterface authInterface = EOSPlatformManager.GetPlatformInterface().GetAuthInterface();
		if (authInterface != null)
		{
			authInterface.Login(ref loginOptions, null, delegate(ref Epic.OnlineServices.Auth.LoginCallbackInfo loginCallbackInfo)
			{
				if (loginCallbackInfo.ResultCode == Result.Success)
				{
					loginState = EOSLoginState.Success;
					cvLinkEOSAccount.SetBool(newValue: false);
					ProcessSuccessfulAuthLogin(loginCallbackInfo.LocalUserId);
				}
				else if (Common.IsOperationComplete(loginCallbackInfo.ResultCode))
				{
					if (loginCallbackInfo.ContinuanceToken != null)
					{
						if (attemptLink)
						{
							loginState = EOSLoginState.AttemptingLink;
							Epic.OnlineServices.Auth.LinkAccountOptions linkAccountOptions = default(Epic.OnlineServices.Auth.LinkAccountOptions);
							linkAccountOptions.ContinuanceToken = loginCallbackInfo.ContinuanceToken;
							linkAccountOptions.LinkAccountFlags = LinkAccountFlags.NoFlags;
							Epic.OnlineServices.Auth.LinkAccountOptions options = linkAccountOptions;
							authInterface.LinkAccount(ref options, null, delegate(ref Epic.OnlineServices.Auth.LinkAccountCallbackInfo linkAccountCallbackInfo)
							{
								if (linkAccountCallbackInfo.ResultCode == Result.Success)
								{
									loginState = EOSLoginState.Success;
									ProcessSuccessfulAuthLogin(linkAccountCallbackInfo.LocalUserId);
								}
								else
								{
									cvLinkEOSAccount.SetBool(newValue: false);
									loginState = EOSLoginState.FailedLink;
								}
							});
						}
						else
						{
							ProcessSteamAppTicketConnectLogin(loginOptions.Credentials.Value.Token);
						}
					}
					else
					{
						loginState = EOSLoginState.FailedLogin;
					}
				}
				else
				{
					loginState = EOSLoginState.FailedLogin;
				}
			});
			return;
		}
		loginState = EOSLoginState.FailedLogin;
		throw new Exception("Failed to get auth interface");
	}

	private void ProcessSuccessfulAuthLogin(EpicAccountId loggedInId)
	{
		loggedInAuthId = loggedInId;
		EOSLoginManager.OnAuthLoggedIn?.Invoke(loggedInAuthId);
		CopyUserAuthTokenOptions options = default(CopyUserAuthTokenOptions);
		if (EOSPlatformManager.GetPlatformInterface().GetAuthInterface().CopyUserAuthToken(ref options, loggedInAuthId, out var outUserAuthToken) == Result.Success)
		{
			Epic.OnlineServices.Connect.LoginOptions loginOptions = default(Epic.OnlineServices.Connect.LoginOptions);
			loginOptions.Credentials = new Epic.OnlineServices.Connect.Credentials
			{
				Token = outUserAuthToken.Value.AccessToken,
				Type = ExternalCredentialType.Epic
			};
			loginOptions.UserLoginInfo = null;
			Epic.OnlineServices.Connect.LoginOptions options2 = loginOptions;
			AttemptConnectLogin(options2);
		}
	}

	private void ProcessSteamAppTicketConnectLogin(string appTicket)
	{
		Epic.OnlineServices.Connect.LoginOptions loginOptions = default(Epic.OnlineServices.Connect.LoginOptions);
		loginOptions.Credentials = new Epic.OnlineServices.Connect.Credentials
		{
			Token = appTicket,
			Type = ExternalCredentialType.SteamSessionTicket
		};
		loginOptions.UserLoginInfo = null;
		Epic.OnlineServices.Connect.LoginOptions options = loginOptions;
		AttemptConnectLogin(options);
	}

	private bool IsConnectInterfaceValid()
	{
		if (connectInterface == null)
		{
			connectInterface = EOSPlatformManager.GetPlatformInterface().GetConnectInterface();
		}
		return connectInterface != null;
	}

	private void AttemptConnectLogin(Epic.OnlineServices.Connect.LoginOptions options)
	{
		if (IsConnectInterfaceValid())
		{
			connectInterface.Login(ref options, null, delegate(ref Epic.OnlineServices.Connect.LoginCallbackInfo loginCallbackInfo)
			{
				if (loginCallbackInfo.ResultCode == Result.Success)
				{
					CompleteConnectLogin(loginCallbackInfo.LocalUserId);
				}
				else if (loginCallbackInfo.ResultCode == Result.InvalidUser)
				{
					CreateUserOptions createUserOptions = default(CreateUserOptions);
					createUserOptions.ContinuanceToken = loginCallbackInfo.ContinuanceToken;
					CreateUserOptions options2 = createUserOptions;
					connectInterface.CreateUser(ref options2, null, delegate(ref CreateUserCallbackInfo createUserCallbackInfo)
					{
						if (createUserCallbackInfo.ResultCode == Result.Success)
						{
							CompleteConnectLogin(createUserCallbackInfo.LocalUserId);
						}
						else
						{
							loginState = EOSLoginState.FailedLogin;
						}
					});
				}
				else if (Common.IsOperationComplete(loginCallbackInfo.ResultCode))
				{
					loginState = EOSLoginState.FailedLogin;
				}
			});
			return;
		}
		throw new Exception("Failed to get connect interface");
	}

	private void CompleteConnectLogin(ProductUserId localUserId)
	{
		loggedInProductId = localUserId;
		loggedInUserID = new PlatformID(loggedInProductId);
		EOSLoginManager.OnConnectLoggedIn?.Invoke(loggedInProductId);
		AddNotifyAuthExpirationOptions options = default(AddNotifyAuthExpirationOptions);
		connectInterface.AddNotifyAuthExpiration(ref options, null, OnAuthExpirationCallback);
		loginState = EOSLoginState.Success;
	}

	private void OnAuthExpirationCallback(ref AuthExpirationCallbackInfo data)
	{
		if (loggedInAuthId != null)
		{
			ProcessSuccessfulAuthLogin(loggedInAuthId);
		}
		else
		{
			UpdateSessionAuthTicket();
		}
	}
}
