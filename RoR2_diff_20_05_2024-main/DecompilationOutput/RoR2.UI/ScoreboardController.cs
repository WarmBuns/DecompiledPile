using System.Collections.Generic;
using System.Linq;
using RoR2.Items;
using UnityEngine;
using UnityEngine.Events;

namespace RoR2.UI;

public class ScoreboardController : MonoBehaviour
{
	public GameObject stripPrefab;

	public RectTransform container;

	private MPEventSystem eventSystem;

	[SerializeField]
	private ItemInventoryDisplay suppressedItemDisplay;

	private UIElementAllocator<ScoreboardStrip> stripAllocator;

	public static event UnityAction onScoreboardOpen;

	private void Awake()
	{
		stripAllocator = new UIElementAllocator<ScoreboardStrip>(container, stripPrefab);
		eventSystem = GetComponent<MPEventSystemLocator>().eventSystem;
	}

	private void SetStripCount(int newCount)
	{
		stripAllocator.AllocateElements(newCount);
	}

	private void Rebuild()
	{
		List<PlayerCharacterMasterController> list = PlayerCharacterMasterController.instances.Where((PlayerCharacterMasterController x) => x.gameObject.activeInHierarchy && x.master.GetBody() != null && Util.GetBestMasterName(x.master) != null).ToList();
		SetStripCount(list.Count);
		for (int i = 0; i < list.Count; i++)
		{
			stripAllocator.elements[i].SetMaster(list[i].master);
		}
	}

	private void PlayerEventToRebuild(PlayerCharacterMasterController playerCharacterMasterController)
	{
		Debug.LogError("ScoreboardController PlayerEventToRebuild call");
		Rebuild();
	}

	private void OnEnable()
	{
		if ((bool)SuppressedItemManager.suppressedInventory)
		{
			suppressedItemDisplay?.SetSubscribedInventory(SuppressedItemManager.suppressedInventory);
			SuppressedItemManager.suppressedInventory.onInventoryChanged += OnInventoryChanged;
		}
		OnInventoryChanged();
		PlayerCharacterMasterController.onPlayerAdded += PlayerEventToRebuild;
		PlayerCharacterMasterController.onPlayerRemoved += PlayerEventToRebuild;
		Rebuild();
		ScoreboardController.onScoreboardOpen?.Invoke();
	}

	private void OnDisable()
	{
		if ((bool)SuppressedItemManager.suppressedInventory)
		{
			suppressedItemDisplay?.SetSubscribedInventory(null);
			SuppressedItemManager.suppressedInventory.onInventoryChanged -= OnInventoryChanged;
		}
		PlayerCharacterMasterController.onPlayerRemoved -= PlayerEventToRebuild;
		PlayerCharacterMasterController.onPlayerAdded -= PlayerEventToRebuild;
	}

	private void OnInventoryChanged()
	{
		suppressedItemDisplay?.gameObject?.SetActive(SuppressedItemManager.HasAnyItemBeenSuppressed());
	}

	public void SelectFirstScoreboardStrip()
	{
		eventSystem.SetSelectedObject(stripAllocator.elements[0].gameObject);
	}
}
