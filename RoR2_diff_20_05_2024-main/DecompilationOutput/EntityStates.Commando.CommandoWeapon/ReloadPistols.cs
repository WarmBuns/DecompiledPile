using UnityEngine;

namespace EntityStates.Commando.CommandoWeapon;

public class ReloadPistols : GenericReload
{
	private static int ReloadPistolsStateHash = Animator.StringToHash("ReloadPistols");

	private static int ReloadPistolsParamHash = Animator.StringToHash("ReloadPistols.playbackRate");

	private static int ReloadPistolsExitStateHash = Animator.StringToHash("ReloadPistolsExit");

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation("Gesture, Override", ReloadPistolsStateHash, ReloadPistolsParamHash, duration);
		PlayAnimation("Gesture, Additive", ReloadPistolsStateHash, ReloadPistolsParamHash, duration);
		FindModelChild("GunMeshL")?.gameObject.SetActive(value: false);
		FindModelChild("GunMeshR")?.gameObject.SetActive(value: false);
	}

	public override void OnExit()
	{
		FindModelChild("ReloadFXL")?.gameObject.SetActive(value: false);
		FindModelChild("ReloadFXR")?.gameObject.SetActive(value: false);
		FindModelChild("GunMeshL")?.gameObject.SetActive(value: true);
		FindModelChild("GunMeshR")?.gameObject.SetActive(value: true);
		PlayAnimation("Gesture, Override", ReloadPistolsExitStateHash);
		PlayAnimation("Gesture, Additive", ReloadPistolsExitStateHash);
		base.OnExit();
	}
}
