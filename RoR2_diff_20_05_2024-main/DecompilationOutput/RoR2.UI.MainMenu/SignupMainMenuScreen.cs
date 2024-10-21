using System;
using System.Collections.Generic;
using System.Net;
using Facepunch.Steamworks;
using HGsignup.Service;
using TMPro;
using UnityEngine;

namespace RoR2.UI.MainMenu;

public class SignupMainMenuScreen : BaseMainMenuScreen
{
	private const string environment = "prod";

	private const string title = "Canary";

	private static string viewableName = "Signup";

	[SerializeField]
	private TMP_InputField emailInput;

	[SerializeField]
	private HGButton submitButton;

	[SerializeField]
	private HGButton backButton;

	[SerializeField]
	private HGTextMeshProUGUI successText;

	[SerializeField]
	private HGTextMeshProUGUI failureText;

	[SerializeField]
	private HGTextMeshProUGUI waitingText;

	[SerializeField]
	private HGGamepadInputEvent backGamepadInputEvent;

	private SignupClient signupClient;

	private string email;

	private bool awaitingResponse;

	private HashSet<string> successfulEmailSubmissions;

	[SystemInitializer(new Type[] { })]
	private static void Init()
	{
		ViewablesCatalog.AddNodeToRoot(new ViewablesCatalog.Node(viewableName, isFolder: false)
		{
			shouldShowUnviewed = CheckViewable
		});
	}

	private static bool CheckViewable(UserProfile userProfile)
	{
		return !userProfile.HasViewedViewable("/" + viewableName);
	}

	private new void Awake()
	{
		signupClient = new SignupClient("prod");
		successfulEmailSubmissions = new HashSet<string>();
	}

	private new void OnEnable()
	{
		waitingText?.gameObject?.SetActive(value: false);
		successText?.gameObject?.SetActive(value: false);
		failureText?.gameObject?.SetActive(value: false);
		if ((bool)backGamepadInputEvent)
		{
			backGamepadInputEvent.enabled = true;
		}
	}

	private new void Update()
	{
		base.Update();
		if ((bool)submitButton)
		{
			if ((bool)emailInput && SignupClient.IsEmailValid(emailInput.text) && !awaitingResponse && !successfulEmailSubmissions.Contains(emailInput.text))
			{
				submitButton.interactable = true;
			}
			else
			{
				submitButton.interactable = false;
			}
		}
		if ((bool)backButton)
		{
			backButton.interactable = !awaitingResponse;
		}
		if ((bool)backGamepadInputEvent)
		{
			backGamepadInputEvent.enabled = !awaitingResponse;
		}
	}

	public void TrySubmit()
	{
		if (!awaitingResponse && (bool)emailInput && !successfulEmailSubmissions.Contains(emailInput.text) && SignupClient.IsEmailValid(emailInput.text))
		{
			email = emailInput.text;
			User user = Client.Instance.User;
			user.OnEncryptedAppTicketRequestComplete = (Action<bool, byte[]>)Delegate.Combine(user.OnEncryptedAppTicketRequestComplete, new Action<bool, byte[]>(ProcessAppTicketRefresh));
			Client.Instance.User.RequestEncryptedAppTicketAsync(new byte[0]);
			awaitingResponse = true;
			waitingText?.gameObject?.SetActive(value: true);
			successText?.gameObject?.SetActive(value: false);
			failureText?.gameObject?.SetActive(value: false);
		}
	}

	private void ProcessAppTicketRefresh(bool success, byte[] ticket)
	{
		bool flag = success;
		User user = Client.Instance.User;
		user.OnEncryptedAppTicketRequestComplete = (Action<bool, byte[]>)Delegate.Remove(user.OnEncryptedAppTicketRequestComplete, new Action<bool, byte[]>(ProcessAppTicketRefresh));
		if (success)
		{
			if (SignupClient.IsTicketValid(ticket))
			{
				if (signupClient != null)
				{
					signupClient.SignupCompleted += SignupCompleted;
					signupClient.SignupSteamAsync(email, "Canary", ticket);
				}
			}
			else
			{
				Debug.LogError("Can't signup with an invalid ticket!");
				flag = false;
			}
		}
		else
		{
			Debug.LogError("Failure refreshing Encrypted App Ticket from Steam for mailing list!");
			flag = false;
		}
		if (!flag)
		{
			awaitingResponse = false;
			waitingText?.gameObject?.SetActive(value: false);
			successText?.gameObject?.SetActive(value: false);
			failureText?.gameObject?.SetActive(value: true);
		}
	}

	private void SignupCompleted(string email, HttpStatusCode statusCode)
	{
		awaitingResponse = false;
		waitingText?.gameObject?.SetActive(value: false);
		if (statusCode == HttpStatusCode.OK)
		{
			successfulEmailSubmissions.Add(email);
			successText?.gameObject?.SetActive(value: true);
			failureText?.gameObject?.SetActive(value: false);
		}
		else
		{
			successText?.gameObject?.SetActive(value: false);
			failureText?.gameObject?.SetActive(value: true);
		}
	}
}
