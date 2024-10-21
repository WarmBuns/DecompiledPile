using System.Collections.ObjectModel;
using RoR2;

namespace EntityStates.Interactables.MSObelisk;

public class ReadyToEndGame : BaseState
{
	public static string chargeupChildString;

	public static string chargeupSoundString;

	public static float chargeupDuration;

	private ChildLocator childLocator;

	private PurchaseInteraction purchaseInteraction;

	private bool ready;

	public override void OnEnter()
	{
		base.OnEnter();
		childLocator = GetComponent<ChildLocator>();
		purchaseInteraction = GetComponent<PurchaseInteraction>();
		purchaseInteraction.NetworkcontextToken = "MSOBELISK_CONTEXT_CONFIRMATION";
		purchaseInteraction.Networkavailable = false;
		childLocator.FindChild(chargeupChildString).gameObject.SetActive(value: true);
		Util.PlaySound(chargeupSoundString, base.gameObject);
		ReadOnlyCollection<PlayerCharacterMasterController> instances = PlayerCharacterMasterController.instances;
		for (int i = 0; i < instances.Count; i++)
		{
			instances[i].master.preventGameOver = true;
		}
		for (int j = 0; j < CameraRigController.readOnlyInstancesList.Count; j++)
		{
			CameraRigController.readOnlyInstancesList[j].disableSpectating = true;
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= chargeupDuration && !ready)
		{
			ready = true;
			purchaseInteraction.Networkavailable = true;
			base.gameObject.GetComponent<EntityStateMachine>().mainStateType = new SerializableEntityStateType(typeof(EndingGame));
		}
	}
}
