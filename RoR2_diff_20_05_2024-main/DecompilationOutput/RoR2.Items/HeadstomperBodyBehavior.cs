using UnityEngine;

namespace RoR2.Items;

public class HeadstomperBodyBehavior : BaseItemBodyBehavior
{
	private GameObject headstompersControllerObject;

	[ItemDefAssociation(useOnServer = true, useOnClient = false)]
	private static ItemDef GetItemDef()
	{
		return RoR2Content.Items.FallBoots;
	}

	private void OnEnable()
	{
		headstompersControllerObject = Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/HeadstompersController"));
		headstompersControllerObject.GetComponent<NetworkedBodyAttachment>().AttachToGameObjectAndSpawn(base.body.gameObject);
	}

	private void OnDisable()
	{
		if ((bool)headstompersControllerObject)
		{
			Object.Destroy(headstompersControllerObject);
			headstompersControllerObject = null;
		}
	}
}
