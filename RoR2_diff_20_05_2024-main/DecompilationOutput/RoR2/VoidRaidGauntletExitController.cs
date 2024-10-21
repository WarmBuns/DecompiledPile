using UnityEngine;

namespace RoR2;

public class VoidRaidGauntletExitController : MonoBehaviour
{
	[SerializeField]
	private MapZone exitZone;

	private void OnEnable()
	{
		if ((bool)exitZone)
		{
			exitZone.onBodyTeleport += OnBodyTeleport;
		}
	}

	private void OnDisable()
	{
		if ((bool)exitZone)
		{
			exitZone.onBodyTeleport -= OnBodyTeleport;
		}
	}

	private void OnBodyTeleport(CharacterBody body)
	{
		if (Util.HasEffectiveAuthority(body.gameObject) && (bool)body.masterObject.GetComponent<PlayerCharacterMasterController>() && (bool)VoidRaidGauntletController.instance)
		{
			VoidRaidGauntletController.instance.OnAuthorityPlayerExit();
		}
	}
}
