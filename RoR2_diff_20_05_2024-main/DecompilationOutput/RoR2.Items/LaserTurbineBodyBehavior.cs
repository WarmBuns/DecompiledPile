using UnityEngine;

namespace RoR2.Items;

public class LaserTurbineBodyBehavior : BaseItemBodyBehavior
{
	private GameObject laserTurbineControllerInstance;

	[ItemDefAssociation(useOnServer = true, useOnClient = false)]
	private static ItemDef GetItemDef()
	{
		return RoR2Content.Items.LaserTurbine;
	}

	private void OnEnable()
	{
		laserTurbineControllerInstance = Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/LaserTurbineController"), base.body.corePosition, Quaternion.identity);
		laserTurbineControllerInstance.GetComponent<GenericOwnership>().ownerObject = base.gameObject;
		laserTurbineControllerInstance.GetComponent<NetworkedBodyAttachment>().AttachToGameObjectAndSpawn(base.gameObject);
	}

	private void OnDestroy()
	{
		Object.Destroy(laserTurbineControllerInstance);
		laserTurbineControllerInstance = null;
	}
}
