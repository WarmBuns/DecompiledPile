using UnityEngine;

namespace RoR2;

[CreateAssetMenu(menuName = "RoR2/MiscPickupDefs/VoidCoinDef")]
public class VoidCoinDef : MiscPickupDef
{
	public override void GrantPickup(ref PickupDef.GrantContext context)
	{
		if ((bool)context.body.master)
		{
			context.body.master.GiveVoidCoins(coinValue);
			context.shouldDestroy = true;
			context.shouldNotify = true;
		}
		else
		{
			context.shouldNotify = false;
			context.shouldDestroy = false;
		}
	}
}
