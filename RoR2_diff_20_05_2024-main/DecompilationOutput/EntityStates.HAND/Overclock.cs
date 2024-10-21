using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.HAND;

public class Overclock : BaseState
{
	public static float baseDuration = 0.25f;

	public static GameObject healEffectPrefab;

	public static float healPercentage = 0.15f;

	public override void OnEnter()
	{
		base.OnEnter();
		if (NetworkServer.active)
		{
			_ = (bool)base.characterBody;
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge > baseDuration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}
}
