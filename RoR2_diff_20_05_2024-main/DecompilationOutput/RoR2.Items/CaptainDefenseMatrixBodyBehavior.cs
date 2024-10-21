using UnityEngine;

namespace RoR2.Items;

public class CaptainDefenseMatrixBodyBehavior : BaseItemBodyBehavior
{
	private GameObject attachmentGameObject;

	private NetworkedBodyAttachment attachment;

	private bool attachmentActive
	{
		get
		{
			return (object)attachment != null;
		}
		set
		{
			if (value != attachmentActive)
			{
				if (value)
				{
					attachmentGameObject = Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/BodyAttachments/CaptainDefenseMatrixItemBodyAttachment"));
					attachment = attachmentGameObject.GetComponent<NetworkedBodyAttachment>();
					attachment.AttachToGameObjectAndSpawn(base.body.gameObject);
				}
				else
				{
					Object.Destroy(attachmentGameObject);
					attachmentGameObject = null;
					attachment = null;
				}
			}
		}
	}

	[ItemDefAssociation(useOnServer = true, useOnClient = false)]
	private static ItemDef GetItemDef()
	{
		return RoR2Content.Items.CaptainDefenseMatrix;
	}

	private void OnDisable()
	{
		attachmentActive = false;
	}

	private void FixedUpdate()
	{
		attachmentActive = base.body.healthComponent.alive;
	}
}
