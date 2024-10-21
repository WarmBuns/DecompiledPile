using Rewired;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI;

public class LocalUserSignInCardController : MonoBehaviour
{
	public TextMeshProUGUI nameLabel;

	public TextMeshProUGUI promptLabel;

	public Image cardImage;

	public Sprite playerCardNone;

	public Sprite playerCardKBM;

	public Sprite playerCardController;

	public Color unselectedColor;

	public Color selectedColor;

	private UserProfileListController userProfileSelectionList;

	public GameObject userProfileSelectionListPrefab;

	private Player _rewiredPlayer;

	private UserProfile _requestedUserProfile;

	public Player rewiredPlayer
	{
		get
		{
			return _rewiredPlayer;
		}
		set
		{
			if (_rewiredPlayer != value)
			{
				_rewiredPlayer = value;
				if (_rewiredPlayer == null)
				{
					requestedUserProfile = null;
				}
			}
		}
	}

	public UserProfile requestedUserProfile
	{
		get
		{
			return _requestedUserProfile;
		}
		private set
		{
			if (_requestedUserProfile != value)
			{
				if (_requestedUserProfile != null)
				{
					_requestedUserProfile.isClaimed = false;
				}
				_requestedUserProfile = value;
				if (_requestedUserProfile != null)
				{
					_requestedUserProfile.isClaimed = true;
				}
			}
		}
	}

	private void Update()
	{
		if (requestedUserProfile != null != (bool)userProfileSelectionList)
		{
			if (!userProfileSelectionList)
			{
				GameObject gameObject = Object.Instantiate(userProfileSelectionListPrefab, base.transform);
				userProfileSelectionList = gameObject.GetComponent<UserProfileListController>();
				userProfileSelectionList.GetComponent<MPEventSystemProvider>().eventSystem = MPEventSystemManager.FindEventSystem(rewiredPlayer);
				userProfileSelectionList.onProfileSelected += OnUserSelectedUserProfile;
			}
			else
			{
				Object.Destroy(userProfileSelectionList.gameObject);
				userProfileSelectionList = null;
			}
		}
		if (rewiredPlayer != null)
		{
			cardImage.color = selectedColor;
			nameLabel.gameObject.SetActive(value: true);
			if (requestedUserProfile == null)
			{
				cardImage.sprite = playerCardNone;
				nameLabel.text = "";
				promptLabel.text = "...";
			}
			else
			{
				cardImage.sprite = playerCardKBM;
				nameLabel.text = requestedUserProfile.name;
				promptLabel.text = "";
			}
		}
		else
		{
			nameLabel.gameObject.SetActive(value: false);
			promptLabel.text = "Press 'Start'";
			cardImage.color = unselectedColor;
			cardImage.sprite = playerCardNone;
		}
	}

	private void OnUserSelectedUserProfile(UserProfile userProfile)
	{
		requestedUserProfile = userProfile;
	}
}
