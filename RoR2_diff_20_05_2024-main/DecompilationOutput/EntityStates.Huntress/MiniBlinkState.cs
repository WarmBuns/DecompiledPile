using UnityEngine;

namespace EntityStates.Huntress;

public class MiniBlinkState : BlinkState
{
	protected override Vector3 GetBlinkVector()
	{
		return ((base.inputBank.moveVector == Vector3.zero) ? base.characterDirection.forward : base.inputBank.moveVector).normalized;
	}
}
