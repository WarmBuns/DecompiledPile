using UnityEngine;
using UnityEngine.EventSystems;

namespace RoR2;

public class VoiceIcon : UIBehaviour
{
	public GameObject MutedIcon;

	public GameObject SilentIcon;

	public GameObject SpeakingIcon;

	public GameObject UnavailableIcon;

	private PlatformID playerId;

	private bool isLocalMuted;

	private bool waitForInitialization;

	private float retryCount;

	private bool doUserRetry;

	private int togglePressedCount;

	protected override void OnDestroy()
	{
		base.OnDestroy();
	}

	protected override void Awake()
	{
		base.Awake();
		togglePressedCount = 0;
		if (!PlatformSystems.lobbyManager.HasMPLobbyFeature(MPLobbyFeatures.Voice))
		{
			base.gameObject.SetActive(value: false);
			return;
		}
		MutedIcon.SetActive(value: false);
		SilentIcon.SetActive(value: false);
		SpeakingIcon.SetActive(value: false);
		UnavailableIcon.SetActive(value: false);
	}

	public virtual void Refresh()
	{
	}

	public void Update()
	{
	}

	public void SetFromPlayerId(PlatformID playerUserID)
	{
	}

	public void ToggleMute()
	{
		UnavailableIcon.SetActive(value: false);
		MutedIcon.SetActive(value: false);
		SilentIcon.SetActive(value: true);
		SpeakingIcon.SetActive(value: false);
	}
}
