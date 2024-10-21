using System.Collections.Generic;
using RoR2.UI;
using TMPro;
using UnityEngine;

public class FriendSessionBrowserController : MonoBehaviour
{
	public SessionButtonController SessionButtonPrefab;

	public Transform SessionButtonContainer;

	public RectTransform InProgressSpinner;

	public TextMeshProUGUI SearchStateText;

	public MPButton RefreshButton;

	public MPButton BackButton;

	private List<SessionButtonController> sessionButtons;
}
