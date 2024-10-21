using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class SummonMasterBehavior : NetworkBehaviour
{
	[Tooltip("The master to spawn")]
	public GameObject masterPrefab;

	public bool callOnEquipmentSpentOnPurchase;

	public bool destroyAfterSummoning = true;

	public override int GetNetworkChannel()
	{
		return QosChannelIndex.defaultReliable.intVal;
	}

	[Server]
	public void OpenSummon(Interactor activator)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.SummonMasterBehavior::OpenSummon(RoR2.Interactor)' called on client");
		}
		else
		{
			OpenSummonReturnMaster(activator);
		}
	}

	[Server]
	public CharacterMaster OpenSummonReturnMaster(Interactor activator)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'RoR2.CharacterMaster RoR2.SummonMasterBehavior::OpenSummonReturnMaster(RoR2.Interactor)' called on client");
			return null;
		}
		float num = 0f;
		CharacterMaster characterMaster = new MasterSummon
		{
			masterPrefab = masterPrefab,
			position = base.transform.position + Vector3.up * num,
			rotation = base.transform.rotation,
			summonerBodyObject = activator?.gameObject,
			ignoreTeamMemberLimit = true,
			useAmbientLevel = true
		}.Perform();
		if ((bool)characterMaster)
		{
			GameObject bodyObject = characterMaster.GetBodyObject();
			if ((bool)bodyObject)
			{
				ModelLocator component = bodyObject.GetComponent<ModelLocator>();
				if ((bool)component && (bool)component.modelTransform)
				{
					TemporaryOverlayInstance temporaryOverlayInstance = TemporaryOverlayManager.AddOverlay(component.modelTransform.gameObject);
					temporaryOverlayInstance.duration = 0.5f;
					temporaryOverlayInstance.animateShaderAlpha = true;
					temporaryOverlayInstance.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
					temporaryOverlayInstance.destroyComponentOnEnd = true;
					temporaryOverlayInstance.originalMaterial = LegacyResourcesAPI.Load<Material>("Materials/matSummonDrone");
					temporaryOverlayInstance.AddToCharacterModel(component.modelTransform.GetComponent<CharacterModel>());
				}
			}
		}
		if (destroyAfterSummoning)
		{
			Object.Destroy(base.gameObject);
		}
		return characterMaster;
	}

	public void OnEnable()
	{
		if (callOnEquipmentSpentOnPurchase)
		{
			PurchaseInteraction.onEquipmentSpentOnPurchase += OnEquipmentSpentOnPurchase;
		}
	}

	public void OnDisable()
	{
		if (callOnEquipmentSpentOnPurchase)
		{
			PurchaseInteraction.onEquipmentSpentOnPurchase -= OnEquipmentSpentOnPurchase;
		}
	}

	[Server]
	private void OnEquipmentSpentOnPurchase(PurchaseInteraction purchaseInteraction, Interactor interactor, EquipmentIndex equipmentIndex)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.SummonMasterBehavior::OnEquipmentSpentOnPurchase(RoR2.PurchaseInteraction,RoR2.Interactor,RoR2.EquipmentIndex)' called on client");
		}
		else if (purchaseInteraction == GetComponent<PurchaseInteraction>())
		{
			CharacterMaster characterMaster = OpenSummonReturnMaster(interactor);
			if ((bool)characterMaster)
			{
				characterMaster.inventory.SetEquipmentIndex(equipmentIndex);
			}
		}
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		bool result = default(bool);
		return result;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
	}

	public override void PreStartClient()
	{
	}
}
