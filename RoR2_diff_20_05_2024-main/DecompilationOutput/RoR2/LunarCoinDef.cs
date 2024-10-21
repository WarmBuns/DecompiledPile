using UnityEngine;

namespace RoR2;

[CreateAssetMenu(menuName = "RoR2/MiscPickupDefs/LunarCoinDef")]
public class LunarCoinDef : MiscPickupDef
{
	[SerializeField]
	public string internalNameOverride;

	public override string GetInternalName()
	{
		if (!string.IsNullOrEmpty(internalNameOverride))
		{
			return internalNameOverride;
		}
		return "MiscPickupIndex." + base.name;
	}

	public override void GrantPickup(ref PickupDef.GrantContext context)
	{
		NetworkUser networkUser = Util.LookUpBodyNetworkUser(context.body);
		if ((bool)networkUser)
		{
			networkUser.AwardLunarCoins(coinValue);
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
