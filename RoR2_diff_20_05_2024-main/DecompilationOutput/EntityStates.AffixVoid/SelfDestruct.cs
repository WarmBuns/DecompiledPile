using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.AffixVoid;

public class SelfDestruct : BaseState
{
	[SerializeField]
	public float duration;

	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	[SerializeField]
	public string enterSoundString;

	public override void OnEnter()
	{
		base.OnEnter();
		PlayCrossfade(animationLayerName, animationStateName, duration);
		if ((bool)base.characterBody)
		{
			base.characterBody.isSprinting = false;
		}
		if ((bool)base.characterDirection)
		{
			base.characterDirection.moveVector = base.characterDirection.forward;
		}
		if ((bool)base.rigidbodyMotor)
		{
			base.rigidbodyMotor.moveVector = Vector3.zero;
		}
		Util.PlaySound(enterSoundString, base.gameObject);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (NetworkServer.active && base.fixedAge >= duration)
		{
			EntityState.Destroy(base.gameObject);
		}
	}
}
