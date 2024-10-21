using RoR2;
using UnityEngine;

public class InviteButton : MonoBehaviour
{
	public void OnInviteButtonPressed()
	{
		PlatformSystems.lobbyManager.OpenInviteOverlay();
	}
}
