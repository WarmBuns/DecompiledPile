using UnityEngine;

namespace EntityStates.VoidRaidCrab.Weapon;

public class FireMultiBeamSmall : BaseFireMultiBeam
{
	[SerializeField]
	public int numFireBeforeFinale;

	private int fireIndex;

	protected override EntityState InstantiateNextState()
	{
		if (fireIndex < numFireBeforeFinale - 1)
		{
			return new FireMultiBeamSmall
			{
				fireIndex = fireIndex + 1
			};
		}
		return new FireMultiBeamFinale();
	}
}
