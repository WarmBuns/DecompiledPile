using UnityEngine;

namespace RoR2.UI;

public class ArtifactOfRebirthTooltipController : MonoBehaviour
{
	public InspectPanelController inspectPanelController;

	private InspectInfo inspectInfo;

	private void Awake()
	{
		Initialize();
	}

	public void Start()
	{
		ShowInspectInfoIfAvailable();
	}

	public void OnEnable()
	{
		ShowInspectInfoIfAvailable();
	}

	public void Initialize()
	{
		foreach (NetworkUser readOnlyLocalPlayers in NetworkUser.readOnlyLocalPlayersList)
		{
			if (readOnlyLocalPlayers.isLocalPlayer)
			{
				inspectPanelController.gameObject.SetActive(value: true);
				ItemIndex rebirthItem = readOnlyLocalPlayers.rebirthItem;
				if (rebirthItem != ItemIndex.None)
				{
					PickupIndex pickupIndex = PickupCatalog.FindPickupIndex(rebirthItem);
					inspectInfo = PickupCatalog.GetPickupDef(pickupIndex);
					inspectInfo.MarkForceShowInfo();
				}
				break;
			}
		}
	}

	private void ShowInspectInfoIfAvailable()
	{
		if (inspectInfo != null)
		{
			inspectPanelController.Show(inspectInfo);
		}
		else
		{
			inspectPanelController.ForceEnableInspectPanel();
		}
	}
}
