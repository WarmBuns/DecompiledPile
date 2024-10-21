using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace RoR2.UI;

[RequireComponent(typeof(HudElement))]
public class EnemyInfoPanel : MonoBehaviour
{
	public GameObject monsterBodiesContainer;

	public RectTransform monsterBodyIconContainer;

	public GameObject inventoryContainer;

	public ItemInventoryDisplay inventoryDisplay;

	public GameObject iconPrefab;

	private UIElementAllocator<RawImage> monsterBodyIconAllocator;

	private BodyIndex[] currentMonsterBodyIndices = Array.Empty<BodyIndex>();

	private HUD hud;

	private ItemIndex[] itemAcquisitionOrder;

	private int itemAcquisitionOrderLength;

	private int[] itemStacks;

	private static GameObject panelPrefab = null;

	private static readonly List<BodyIndex> bodyIndexList = new List<BodyIndex>();

	private static bool isDirty = false;

	private static readonly List<int> cachedArenaMonsterBodiesList = new List<int>();

	private void Awake()
	{
		monsterBodyIconAllocator = new UIElementAllocator<RawImage>(monsterBodyIconContainer, iconPrefab);
		itemStacks = ItemCatalog.RequestItemStackArray();
		itemAcquisitionOrder = ItemCatalog.RequestItemOrderBuffer();
		monsterBodiesContainer.SetActive(value: false);
		inventoryContainer.SetActive(value: false);
	}

	private void OnDestroy()
	{
		ItemCatalog.ReturnItemOrderBuffer(itemAcquisitionOrder);
		itemAcquisitionOrder = null;
		ItemCatalog.ReturnItemStackArray(itemStacks);
		itemStacks = null;
	}

	private void OnEnable()
	{
		InstanceTracker.Add(this);
		MarkDirty();
	}

	private void OnDisable()
	{
		MarkDirty();
		InstanceTracker.Remove(this);
	}

	private void TrySetMonsterBodies<T>([NotNull] T newBodyIndices) where T : IList<BodyIndex>
	{
		bool flag = false;
		if (currentMonsterBodyIndices.Length != newBodyIndices.Count)
		{
			Array.Resize(ref currentMonsterBodyIndices, newBodyIndices.Count);
			flag = true;
		}
		for (int i = 0; i < newBodyIndices.Count; i++)
		{
			if (currentMonsterBodyIndices[i] != newBodyIndices[i])
			{
				currentMonsterBodyIndices[i] = newBodyIndices[i];
				flag = true;
			}
		}
		if (flag)
		{
			SetMonsterBodies(currentMonsterBodyIndices);
		}
	}

	private void SetMonsterBodies([NotNull] BodyIndex[] bodyIndices)
	{
		monsterBodyIconAllocator.AllocateElements(bodyIndices.Length);
		for (int i = 0; i < bodyIndices.Length; i++)
		{
			CharacterBody bodyPrefabBodyComponent = BodyCatalog.GetBodyPrefabBodyComponent(bodyIndices[i]);
			monsterBodyIconAllocator.elements[i].texture = bodyPrefabBodyComponent?.portraitIcon;
		}
		monsterBodiesContainer.SetActive(bodyIndices.Length != 0);
	}

	private void TrySetItems<T1, T2>([NotNull] T1 newItemAcquisitionOrder, int newItemAcquisitionOrderLength, [NotNull] T2 newItemStacks) where T1 : IList<ItemIndex> where T2 : IList<int>
	{
		bool flag = false;
		bool flag2 = false;
		if (itemAcquisitionOrderLength != newItemAcquisitionOrderLength)
		{
			flag = true;
		}
		else
		{
			for (int i = 0; i < itemAcquisitionOrderLength; i++)
			{
				if (itemAcquisitionOrder[i] != newItemAcquisitionOrder[i])
				{
					flag = true;
					break;
				}
			}
		}
		for (int j = 0; j < itemStacks.Length; j++)
		{
			if (itemStacks[j] != newItemStacks[j])
			{
				flag2 = true;
				break;
			}
		}
		if (flag)
		{
			itemAcquisitionOrderLength = newItemAcquisitionOrderLength;
			for (int k = 0; k < itemAcquisitionOrderLength; k++)
			{
				itemAcquisitionOrder[k] = newItemAcquisitionOrder[k];
			}
		}
		if (flag2)
		{
			for (int l = 0; l < itemStacks.Length; l++)
			{
				itemStacks[l] = newItemStacks[l];
			}
		}
		if (flag || flag2)
		{
			SetItems(itemAcquisitionOrder, itemAcquisitionOrderLength, itemStacks);
		}
	}

	private void SetItems([NotNull] ItemIndex[] newItemAcquisitionOrder, int newItemAcquisitionOrderLength, [NotNull] int[] newItemStacks)
	{
		inventoryContainer.SetActive(newItemAcquisitionOrderLength > 0);
		inventoryDisplay.SetItems(newItemAcquisitionOrder, newItemAcquisitionOrderLength, newItemStacks);
	}

	private static EnemyInfoPanel SetDisplayingOnHud([NotNull] HUD hud, bool shouldDisplay)
	{
		List<EnemyInfoPanel> instancesList = InstanceTracker.GetInstancesList<EnemyInfoPanel>();
		EnemyInfoPanel enemyInfoPanel = null;
		for (int i = 0; i < instancesList.Count; i++)
		{
			EnemyInfoPanel enemyInfoPanel2 = instancesList[i];
			if ((object)enemyInfoPanel2.hud == hud)
			{
				enemyInfoPanel = enemyInfoPanel2;
				break;
			}
		}
		if ((bool)enemyInfoPanel != shouldDisplay)
		{
			if (!enemyInfoPanel)
			{
				Transform transform = null;
				if ((bool)hud.gameModeUiInstance)
				{
					ChildLocator component = hud.gameModeUiInstance.GetComponent<ChildLocator>();
					if ((bool)component)
					{
						Transform transform2 = component.FindChild("RightInfoBar");
						if ((bool)transform2)
						{
							transform = transform2.GetComponent<RectTransform>();
						}
					}
				}
				if ((bool)transform)
				{
					EnemyInfoPanel component2 = UnityEngine.Object.Instantiate(panelPrefab, transform).GetComponent<EnemyInfoPanel>();
					component2.hud = hud;
					enemyInfoPanel = component2;
				}
			}
			else
			{
				UnityEngine.Object.Destroy(enemyInfoPanel.gameObject);
				enemyInfoPanel = null;
			}
		}
		return enemyInfoPanel;
	}

	[SystemInitializer(new Type[] { })]
	private static void Init()
	{
		ArenaMissionController.onInstanceChangedGlobal += ArenaMissionControllerOnOnInstanceChangedGlobal;
		LegacyResourcesAPI.LoadAsyncCallback("Prefabs/UI/EnemyInfoPanel", delegate(GameObject operationResult)
		{
			panelPrefab = operationResult;
		});
		HUD.onHudTargetChangedGlobal += OnHudTargetChangedGlobal;
		EnemyInfoPanelInventoryProvider.onInventoriesChanged += RefreshAll;
		RoR2Application.onFixedUpdate += StaticFixedUpdate;
	}

	private static void ArenaMissionControllerOnOnInstanceChangedGlobal()
	{
		MarkDirty();
	}

	private static void OnHudTargetChangedGlobal([NotNull] HUD hud)
	{
		MarkDirty();
	}

	private static void MarkDirty()
	{
		if (!isDirty)
		{
			RoR2Application.onNextUpdate += RefreshAll;
		}
	}

	private static void RefreshAll()
	{
		for (int i = 0; i < HUD.readOnlyInstanceList.Count; i++)
		{
			RefreshHUD(HUD.readOnlyInstanceList[i]);
		}
		isDirty = false;
	}

	private static void RefreshHUD(HUD hud)
	{
		if (!hud.targetMaster)
		{
			return;
		}
		TeamIndex teamIndex = hud.targetMaster.teamIndex;
		ItemIndex[] array = ItemCatalog.RequestItemOrderBuffer();
		int itemAcquisitonOrderLength = 0;
		int[] array2 = ItemCatalog.RequestItemStackArray();
		int[] array3 = ItemCatalog.RequestItemStackArray();
		List<EnemyInfoPanelInventoryProvider> instancesList = InstanceTracker.GetInstancesList<EnemyInfoPanelInventoryProvider>();
		int i = 0;
		for (int count = instancesList.Count; i < count; i++)
		{
			EnemyInfoPanelInventoryProvider enemyInfoPanelInventoryProvider = instancesList[i];
			if (enemyInfoPanelInventoryProvider.teamFilter.teamIndex == teamIndex)
			{
				continue;
			}
			List<ItemIndex> list = enemyInfoPanelInventoryProvider.inventory.itemAcquisitionOrder;
			int j = 0;
			for (int count2 = list.Count; j < count2; j++)
			{
				ItemIndex itemIndex = list[j];
				ref int reference = ref array3[(int)itemIndex];
				if (reference == 0)
				{
					reference = 1;
					array[itemAcquisitonOrderLength++] = itemIndex;
				}
			}
			for (int k = 0; k < array2.Length; k++)
			{
				array2[k] += enemyInfoPanelInventoryProvider.inventory.GetItemCount((ItemIndex)k);
			}
		}
		bodyIndexList.Clear();
		if ((bool)ArenaMissionController.instance)
		{
			SyncListInt syncActiveMonsterBodies = ArenaMissionController.instance.syncActiveMonsterBodies;
			for (int l = 0; l < syncActiveMonsterBodies.Count; l++)
			{
				bodyIndexList.Add((BodyIndex)syncActiveMonsterBodies[l]);
			}
		}
		else
		{
			BodyIndex bodyIndex = (Stage.instance ? Stage.instance.singleMonsterTypeBodyIndex : BodyIndex.None);
			if (bodyIndex != BodyIndex.None)
			{
				bodyIndexList.Add(bodyIndex);
			}
		}
		SetDisplayDataForViewer(hud, bodyIndexList, array, itemAcquisitonOrderLength, array2);
		ItemCatalog.ReturnItemStackArray(array3);
		ItemCatalog.ReturnItemStackArray(array2);
		ItemCatalog.ReturnItemOrderBuffer(array);
	}

	private static void SetDisplayDataForViewer([NotNull] HUD hud, [NotNull] List<BodyIndex> bodyIndices, [NotNull] ItemIndex[] itemAcquisitionOrderBuffer, int itemAcquisitonOrderLength, int[] itemStacks)
	{
		bool shouldDisplay = bodyIndices.Count > 0 || itemAcquisitonOrderLength > 0;
		EnemyInfoPanel enemyInfoPanel = SetDisplayingOnHud(hud, shouldDisplay);
		if ((bool)enemyInfoPanel && enemyInfoPanel.isActiveAndEnabled)
		{
			enemyInfoPanel.TrySetMonsterBodies(bodyIndices);
			enemyInfoPanel.TrySetItems(itemAcquisitionOrderBuffer, itemAcquisitonOrderLength, itemStacks);
		}
	}

	private static void StaticFixedUpdate()
	{
		if (!ArenaMissionController.instance)
		{
			return;
		}
		bool flag = false;
		SyncListInt syncActiveMonsterBodies = ArenaMissionController.instance.syncActiveMonsterBodies;
		if (cachedArenaMonsterBodiesList.Count == syncActiveMonsterBodies.Count)
		{
			int i = 0;
			for (int count = syncActiveMonsterBodies.Count; i < count; i++)
			{
				if (cachedArenaMonsterBodiesList[i] != syncActiveMonsterBodies[i])
				{
					flag = true;
					break;
				}
			}
		}
		else
		{
			flag = true;
		}
		if (flag)
		{
			cachedArenaMonsterBodiesList.Clear();
			int j = 0;
			for (int count2 = syncActiveMonsterBodies.Count; j < count2; j++)
			{
				cachedArenaMonsterBodiesList.Add(syncActiveMonsterBodies[j]);
			}
			MarkDirty();
		}
	}
}
