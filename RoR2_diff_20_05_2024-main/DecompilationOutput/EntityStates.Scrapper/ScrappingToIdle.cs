using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Scrapper;

public class ScrappingToIdle : ScrapperBaseState
{
	public static string enterSoundString;

	public static string exitSoundString;

	public static float duration;

	public static float dropUpVelocityStrength;

	public static float dropForwardVelocityStrength;

	public static GameObject muzzleflashEffectPrefab;

	public static string muzzleString;

	private bool foundValidScrap;

	private static int ScrappingToIdleStateHash = Animator.StringToHash("ScrappingToIdle");

	private static int ScrappingParamHash = Animator.StringToHash("Scrapping.playbackRate");

	protected override bool enableInteraction => false;

	public override void OnEnter()
	{
		base.OnEnter();
		Util.PlaySound(enterSoundString, base.gameObject);
		PlayAnimation("Base", ScrappingToIdleStateHash, ScrappingParamHash, duration);
		if ((bool)muzzleflashEffectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(muzzleflashEffectPrefab, base.gameObject, muzzleString, transmit: false);
		}
		if (NetworkServer.active)
		{
			foundValidScrap = false;
			PickupIndex none = PickupIndex.none;
			none = PickupCatalog.FindScrapIndexForItemTier(ItemCatalog.GetItemDef(scrapperController.lastScrappedItemIndex).tier);
			if (none != PickupIndex.none)
			{
				foundValidScrap = true;
				Transform transform = FindModelChild(muzzleString);
				PickupDropletController.CreatePickupDroplet(none, transform.position, Vector3.up * dropUpVelocityStrength + transform.forward * dropForwardVelocityStrength);
				scrapperController.itemsEaten--;
			}
		}
	}

	public override void OnExit()
	{
		Util.PlaySound(exitSoundString, base.gameObject);
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (foundValidScrap && scrapperController.itemsEaten > 0 && base.fixedAge > duration / 2f)
		{
			outer.SetNextState(new ScrappingToIdle());
		}
		else if (base.fixedAge > duration)
		{
			outer.SetNextState(new Idle());
		}
	}
}
