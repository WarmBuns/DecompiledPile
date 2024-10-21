using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.CaptainSupplyDrop;

public class UnlockTargetState : BaseMainState
{
	public static GameObject unlockEffectPrefab;

	public static float baseDuration;

	public static string soundString;

	public PurchaseInteraction target;

	private float duration;

	public override void OnEnter()
	{
		base.OnEnter();
		if (NetworkServer.active && (bool)target && target.available)
		{
			target.Networkcost = 0;
			GameObject ownerObject = GetComponent<GenericOwnership>().ownerObject;
			if ((bool)ownerObject)
			{
				Interactor component = ownerObject.GetComponent<Interactor>();
				if ((bool)component)
				{
					component.AttemptInteraction(target.gameObject);
				}
			}
			EffectManager.SpawnEffect(unlockEffectPrefab, new EffectData
			{
				origin = target.transform.position
			}, transmit: true);
		}
		Util.PlaySound(soundString, base.gameObject);
		duration = baseDuration;
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override void OnSerialize(NetworkWriter writer)
	{
		base.OnSerialize(writer);
		writer.Write(target ? target.gameObject : null);
	}

	public override void OnDeserialize(NetworkReader reader)
	{
		base.OnDeserialize(reader);
		GameObject gameObject = reader.ReadGameObject();
		target = (gameObject ? gameObject.GetComponent<PurchaseInteraction>() : null);
	}
}
