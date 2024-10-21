using RoR2.UI;
using UnityEngine;

namespace RoR2;

[RequireComponent(typeof(VoteController))]
public class CreditsController : MonoBehaviour
{
	private CreditsPanelController creditsPanelController;

	private VoteController voteController;

	private static GameObject creditsPanelPrefab => LegacyResourcesAPI.Load<GameObject>("Prefabs/UI/Credits/CreditsPanel");

	private void Awake()
	{
		voteController = GetComponent<VoteController>();
	}

	private void OnEnable()
	{
		creditsPanelController = Object.Instantiate(creditsPanelPrefab, RoR2Application.instance.mainCanvas.transform).GetComponent<CreditsPanelController>();
		creditsPanelController.voteInfoPanel.voteController = voteController;
		creditsPanelController.skipButton.onClick.AddListener(SubmitLocalVotesToEnd);
		PauseManager.IsAbleToPause = false;
	}

	private void OnDisable()
	{
		PauseManager.IsAbleToPause = true;
		if ((bool)creditsPanelController)
		{
			Object.Destroy(creditsPanelController.gameObject);
		}
	}

	private void Update()
	{
		if (!creditsPanelController)
		{
			SubmitLocalVotesToEnd();
			base.enabled = false;
		}
	}

	private void SubmitLocalVotesToEnd()
	{
		foreach (NetworkUser readOnlyLocalPlayers in NetworkUser.readOnlyLocalPlayersList)
		{
			readOnlyLocalPlayers.CallCmdSubmitVote(base.gameObject, 0);
		}
	}
}
