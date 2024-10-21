using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace RoR2;

public class AffixBeadBehavior : CharacterBody.ItemBehavior
{
	private GameObject affixBeadWard;

	public GameObject affixBeadWardReference;

	private GameObject beadHolderVFX;

	public GameObject beadHolderVFXReference;

	private void Update()
	{
		if (!NetworkServer.active)
		{
			return;
		}
		bool flag = stack > 0;
		if ((bool)affixBeadWard != flag)
		{
			if (flag)
			{
				affixBeadWard = Object.Instantiate(affixBeadWardReference);
				affixBeadWard.GetComponent<TeamFilter>().teamIndex = body.teamComponent.teamIndex;
				affixBeadWard.GetComponent<BuffWard>().Networkradius = 30f + body.radius;
				affixBeadWard.GetComponent<NetworkedBodyAttachment>().AttachToGameObjectAndSpawn(body.gameObject);
				beadHolderVFX = Object.Instantiate(beadHolderVFXReference);
				beadHolderVFX.GetComponent<NetworkedBodyAttachment>().AttachToGameObjectAndSpawn(body.gameObject, "Head");
				beadHolderVFX.transform.localScale *= body.modelLocator.modelScaleCompensation;
			}
			else
			{
				Object.Destroy(affixBeadWard);
				affixBeadWard = null;
				Object.Destroy(beadHolderVFX);
				beadHolderVFX = null;
			}
		}
	}

	private void OnEnable()
	{
		beadHolderVFXReference = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC2/Elites/EliteBead/EliteBeadFire.prefab").WaitForCompletion();
		affixBeadWardReference = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC2/Elites/EliteBead/AffixBeadWard.prefab").WaitForCompletion();
	}

	private void OnDisable()
	{
		if ((bool)affixBeadWard)
		{
			Object.Destroy(affixBeadWard);
		}
		if ((bool)beadHolderVFX)
		{
			Object.Destroy(beadHolderVFX);
		}
	}
}
