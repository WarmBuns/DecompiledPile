using System;
using System.Collections.Generic;
using System.Linq;
using HG;
using RoR2.Audio;
using RoR2.Items;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RoR2;

public static class CostTypeCatalog
{
	private static class LunarItemOrEquipmentCostTypeHelper
	{
		private static ItemIndex[] lunarItemIndices = Array.Empty<ItemIndex>();

		private static EquipmentIndex[] lunarEquipmentIndices = Array.Empty<EquipmentIndex>();

		public static bool IsAffordable(CostTypeDef costTypeDef, CostTypeDef.IsAffordableContext context)
		{
			CharacterBody component = context.activator.GetComponent<CharacterBody>();
			if (!component)
			{
				return false;
			}
			Inventory inventory = component.inventory;
			if (!inventory)
			{
				return false;
			}
			int cost = context.cost;
			int num = 0;
			for (int i = 0; i < lunarItemIndices.Length; i++)
			{
				int itemCount = inventory.GetItemCount(lunarItemIndices[i]);
				num += itemCount;
				if (num >= cost)
				{
					return true;
				}
			}
			int j = 0;
			for (int equipmentSlotCount = inventory.GetEquipmentSlotCount(); j < equipmentSlotCount; j++)
			{
				EquipmentState equipment = inventory.GetEquipment((uint)j);
				for (int k = 0; k < lunarEquipmentIndices.Length; k++)
				{
					if (equipment.equipmentIndex == lunarEquipmentIndices[k])
					{
						num++;
						if (num >= cost)
						{
							return true;
						}
					}
				}
			}
			return false;
		}

		public static void PayCost(CostTypeDef costTypeDef, CostTypeDef.PayCostContext context)
		{
			Inventory inventory = context.activator.GetComponent<CharacterBody>().inventory;
			int cost = context.cost;
			int itemWeight = 0;
			int equipmentWeight = 0;
			for (int i = 0; i < lunarItemIndices.Length; i++)
			{
				ItemIndex itemIndex = lunarItemIndices[i];
				int itemCount = inventory.GetItemCount(itemIndex);
				itemWeight += itemCount;
			}
			int j = 0;
			for (int equipmentSlotCount = inventory.GetEquipmentSlotCount(); j < equipmentSlotCount; j++)
			{
				EquipmentState equipment = inventory.GetEquipment((uint)j);
				if (Array.IndexOf(lunarEquipmentIndices, equipment.equipmentIndex) != -1)
				{
					int num = equipmentWeight + 1;
					equipmentWeight = num;
				}
			}
			int totalWeight = itemWeight + equipmentWeight;
			for (int k = 0; k < cost; k++)
			{
				TakeOne();
			}
			MultiShopCardUtils.OnNonMoneyPurchase(context);
			void TakeOne()
			{
				float nextNormalizedFloat = context.rng.nextNormalizedFloat;
				float num2 = (float)itemWeight / (float)totalWeight;
				if (nextNormalizedFloat < num2)
				{
					int num3 = Mathf.FloorToInt(Util.Remap(Util.Remap(nextNormalizedFloat, 0f, num2, 0f, 1f), 0f, 1f, 0f, itemWeight));
					int num4 = 0;
					for (int l = 0; l < lunarItemIndices.Length; l++)
					{
						ItemIndex itemIndex2 = lunarItemIndices[l];
						int itemCount2 = inventory.GetItemCount(itemIndex2);
						num4 += itemCount2;
						if (num3 < num4)
						{
							inventory.RemoveItem(itemIndex2);
							context.results.itemsTaken.Add(itemIndex2);
							break;
						}
					}
				}
				else
				{
					int num5 = Mathf.FloorToInt(Util.Remap(Util.Remap(nextNormalizedFloat, num2, 1f, 0f, 1f), 0f, 1f, 0f, equipmentWeight));
					int num6 = 0;
					for (int m = 0; m < inventory.GetEquipmentSlotCount(); m++)
					{
						EquipmentIndex equipmentIndex = inventory.GetEquipment((uint)m).equipmentIndex;
						if (Array.IndexOf(lunarEquipmentIndices, equipmentIndex) != -1)
						{
							num6++;
							if (num5 < num6)
							{
								inventory.SetEquipment(EquipmentState.empty, (uint)m);
								context.results.equipmentTaken.Add(equipmentIndex);
							}
						}
					}
				}
			}
		}

		private static void PayOne(Inventory inventory)
		{
			new WeightedSelection<ItemIndex>(lunarItemIndices.Length);
			new WeightedSelection<uint>(inventory.GetEquipmentSlotCount());
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < lunarItemIndices.Length; i++)
			{
				ItemIndex itemIndex = lunarItemIndices[i];
				int itemCount = inventory.GetItemCount(itemIndex);
				num += itemCount;
			}
			int j = 0;
			for (int equipmentSlotCount = inventory.GetEquipmentSlotCount(); j < equipmentSlotCount; j++)
			{
				EquipmentState equipment = inventory.GetEquipment((uint)j);
				if (Array.IndexOf(lunarEquipmentIndices, equipment.equipmentIndex) != -1)
				{
					num2++;
				}
			}
		}

		[SystemInitializer(new Type[]
		{
			typeof(ItemCatalog),
			typeof(EquipmentCatalog)
		})]
		private static void Init()
		{
			lunarItemIndices = ItemCatalog.lunarItemList.ToArray();
			lunarEquipmentIndices = EquipmentCatalog.equipmentList.Where((EquipmentIndex v) => EquipmentCatalog.GetEquipmentDef(v).isLunar).ToArray();
		}
	}

	private static CostTypeDef[] costTypeDefs;

	public static readonly CatalogModHelper<CostTypeDef> modHelper = new CatalogModHelper<CostTypeDef>(delegate(int i, CostTypeDef def)
	{
		Register((CostTypeIndex)i, def);
	}, (CostTypeDef v) => v.name);

	public static int costTypeCount => costTypeDefs.Length;

	private static void Register(CostTypeIndex costType, CostTypeDef costTypeDef)
	{
		if (costType < CostTypeIndex.Count)
		{
			costTypeDef.name = costType.ToString();
		}
		costTypeDefs[(int)costType] = costTypeDef;
	}

	[SystemInitializer(new Type[] { })]
	private static void Init()
	{
		costTypeDefs = new CostTypeDef[16];
		Register(CostTypeIndex.None, new CostTypeDef
		{
			buildCostString = delegate(CostTypeDef costTypeDef, CostTypeDef.BuildCostStringContext context)
			{
				context.stringBuilder.Append("");
			},
			isAffordable = (CostTypeDef costTypeDef, CostTypeDef.IsAffordableContext context) => true,
			payCost = delegate(CostTypeDef costTypeDef, CostTypeDef.PayCostContext context)
			{
				MultiShopCardUtils.OnNonMoneyPurchase(context);
			}
		});
		Register(CostTypeIndex.Money, new CostTypeDef
		{
			costStringFormatToken = "COST_MONEY_FORMAT",
			isAffordable = delegate(CostTypeDef costTypeDef, CostTypeDef.IsAffordableContext context)
			{
				CharacterBody component = context.activator.GetComponent<CharacterBody>();
				if ((bool)component)
				{
					CharacterMaster master = component.master;
					if ((bool)master)
					{
						return master.money >= context.cost;
					}
				}
				return false;
			},
			payCost = delegate(CostTypeDef costTypeDef, CostTypeDef.PayCostContext context)
			{
				if ((bool)context.activatorMaster)
				{
					context.activatorMaster.money -= (uint)context.cost;
					MultiShopCardUtils.OnMoneyPurchase(context);
				}
			},
			colorIndex = ColorCatalog.ColorIndex.Money
		});
		Register(CostTypeIndex.PercentHealth, new CostTypeDef
		{
			costStringFormatToken = "COST_PERCENTHEALTH_FORMAT",
			saturateWorldStyledCostString = false,
			darkenWorldStyledCostString = true,
			isAffordable = delegate(CostTypeDef costTypeDef, CostTypeDef.IsAffordableContext context)
			{
				HealthComponent component2 = context.activator.GetComponent<HealthComponent>();
				return (bool)component2 && component2.combinedHealth / component2.fullCombinedHealth * 100f >= (float)context.cost;
			},
			payCost = delegate(CostTypeDef costTypeDef, CostTypeDef.PayCostContext context)
			{
				HealthComponent component3 = context.activator.GetComponent<HealthComponent>();
				if ((bool)component3)
				{
					float combinedHealth = component3.combinedHealth;
					float num = component3.fullCombinedHealth * (float)context.cost / 100f;
					if (combinedHealth > num)
					{
						component3.TakeDamage(new DamageInfo
						{
							damage = num,
							attacker = context.purchasedObject,
							position = context.purchasedObject.transform.position,
							damageType = (DamageType.NonLethal | DamageType.BypassArmor)
						});
						MultiShopCardUtils.OnNonMoneyPurchase(context);
					}
				}
			},
			colorIndex = ColorCatalog.ColorIndex.Blood
		});
		Register(CostTypeIndex.LunarCoin, new CostTypeDef
		{
			costStringFormatToken = "COST_LUNARCOIN_FORMAT",
			saturateWorldStyledCostString = false,
			darkenWorldStyledCostString = true,
			isAffordable = delegate(CostTypeDef costTypeDef, CostTypeDef.IsAffordableContext context)
			{
				NetworkUser networkUser = Util.LookUpBodyNetworkUser(context.activator.gameObject);
				return (bool)networkUser && networkUser.lunarCoins >= context.cost;
			},
			payCost = delegate(CostTypeDef costTypeDef, CostTypeDef.PayCostContext context)
			{
				NetworkUser networkUser2 = Util.LookUpBodyNetworkUser(context.activator.gameObject);
				if ((bool)networkUser2)
				{
					networkUser2.DeductLunarCoins((uint)context.cost);
					MultiShopCardUtils.OnNonMoneyPurchase(context);
				}
			},
			colorIndex = ColorCatalog.ColorIndex.LunarCoin
		});
		Register(CostTypeIndex.VoidCoin, new CostTypeDef
		{
			costStringFormatToken = "COST_VOIDCOIN_FORMAT",
			isAffordable = delegate(CostTypeDef costTypeDef, CostTypeDef.IsAffordableContext context)
			{
				CharacterBody component4 = context.activator.GetComponent<CharacterBody>();
				if ((bool)component4)
				{
					CharacterMaster master2 = component4.master;
					if ((bool)master2)
					{
						return master2.voidCoins >= context.cost;
					}
				}
				return false;
			},
			payCost = delegate(CostTypeDef costTypeDef, CostTypeDef.PayCostContext context)
			{
				if ((bool)context.activatorMaster)
				{
					context.activatorMaster.voidCoins -= (uint)context.cost;
					MultiShopCardUtils.OnNonMoneyPurchase(context);
				}
			},
			saturateWorldStyledCostString = false,
			darkenWorldStyledCostString = true,
			colorIndex = ColorCatalog.ColorIndex.VoidCoin
		});
		Register(CostTypeIndex.SoulCost, new CostTypeDef
		{
			costStringFormatToken = "COST_SOULCOST_FORMAT",
			saturateWorldStyledCostString = false,
			darkenWorldStyledCostString = true,
			isAffordable = delegate(CostTypeDef costTypeDef, CostTypeDef.IsAffordableContext context)
			{
				HealthComponent component5 = context.activator.GetComponent<HealthComponent>();
				return (bool)component5 && component5.combinedHealth / component5.fullCombinedHealth * 100f >= (float)context.cost;
			},
			payCost = delegate(CostTypeDef costTypeDef, CostTypeDef.PayCostContext context)
			{
				HealthComponent component6 = context.activator.GetComponent<HealthComponent>();
				if ((bool)component6)
				{
					float combinedHealth2 = component6.combinedHealth;
					float num2 = component6.fullCombinedHealth * (float)context.cost / 100f;
					int newCount = context.cost / 10;
					if (combinedHealth2 > num2)
					{
						component6.body.SetBuffCount(DLC2Content.Buffs.SoulCost.buffIndex, newCount);
						MultiShopCardUtils.OnNonMoneyPurchase(context);
					}
				}
			},
			colorIndex = ColorCatalog.ColorIndex.BossItem
		});
		Register(CostTypeIndex.WhiteItem, new CostTypeDef
		{
			costStringFormatToken = "COST_ITEM_FORMAT",
			isAffordable = IsAffordableItem,
			payCost = PayCostItems,
			colorIndex = ColorCatalog.ColorIndex.Tier1Item,
			itemTier = ItemTier.Tier1
		});
		Register(CostTypeIndex.GreenItem, new CostTypeDef
		{
			costStringFormatToken = "COST_ITEM_FORMAT",
			saturateWorldStyledCostString = true,
			isAffordable = IsAffordableItem,
			payCost = PayCostItems,
			colorIndex = ColorCatalog.ColorIndex.Tier2Item,
			itemTier = ItemTier.Tier2
		});
		Register(CostTypeIndex.RedItem, new CostTypeDef
		{
			costStringFormatToken = "COST_ITEM_FORMAT",
			saturateWorldStyledCostString = false,
			darkenWorldStyledCostString = true,
			isAffordable = IsAffordableItem,
			payCost = PayCostItems,
			colorIndex = ColorCatalog.ColorIndex.Tier3Item,
			itemTier = ItemTier.Tier3
		});
		Register(CostTypeIndex.BossItem, new CostTypeDef
		{
			costStringFormatToken = "COST_ITEM_FORMAT",
			darkenWorldStyledCostString = true,
			isAffordable = IsAffordableItem,
			payCost = PayCostItems,
			colorIndex = ColorCatalog.ColorIndex.BossItem,
			itemTier = ItemTier.Boss
		});
		Register(CostTypeIndex.Equipment, new CostTypeDef
		{
			costStringFormatToken = "COST_EQUIPMENT_FORMAT",
			isAffordable = delegate(CostTypeDef costTypeDef, CostTypeDef.IsAffordableContext context)
			{
				CharacterBody component7 = context.activator.gameObject.GetComponent<CharacterBody>();
				if ((bool)component7)
				{
					Inventory inventory = component7.inventory;
					if ((bool)inventory)
					{
						return inventory.currentEquipmentIndex != EquipmentIndex.None;
					}
				}
				return false;
			},
			payCost = PayEquipment,
			colorIndex = ColorCatalog.ColorIndex.Equipment
		});
		Register(CostTypeIndex.VolatileBattery, new CostTypeDef
		{
			costStringFormatToken = "COST_VOLATILEBATTERY_FORMAT",
			isAffordable = delegate(CostTypeDef costTypeDef, CostTypeDef.IsAffordableContext context)
			{
				CharacterBody component8 = context.activator.gameObject.GetComponent<CharacterBody>();
				if ((bool)component8)
				{
					Inventory inventory2 = component8.inventory;
					if ((bool)inventory2)
					{
						return inventory2.currentEquipmentIndex == RoR2Content.Equipment.QuestVolatileBattery.equipmentIndex;
					}
				}
				return false;
			},
			payCost = PayEquipment,
			colorIndex = ColorCatalog.ColorIndex.Equipment
		});
		Register(CostTypeIndex.LunarItemOrEquipment, new CostTypeDef
		{
			costStringFormatToken = "COST_LUNAR_FORMAT",
			isAffordable = LunarItemOrEquipmentCostTypeHelper.IsAffordable,
			payCost = LunarItemOrEquipmentCostTypeHelper.PayCost,
			colorIndex = ColorCatalog.ColorIndex.LunarItem
		});
		Register(CostTypeIndex.ArtifactShellKillerItem, new CostTypeDef
		{
			costStringFormatToken = "COST_ARTIFACTSHELLKILLERITEM_FORMAT",
			isAffordable = delegate(CostTypeDef costTypeDef, CostTypeDef.IsAffordableContext context)
			{
				CharacterBody component9 = context.activator.gameObject.GetComponent<CharacterBody>();
				if ((bool)component9)
				{
					Inventory inventory3 = component9.inventory;
					if ((bool)inventory3)
					{
						return inventory3.GetItemCount(RoR2Content.Items.ArtifactKey) >= context.cost;
					}
				}
				return false;
			},
			payCost = delegate(CostTypeDef costTypeDef, CostTypeDef.PayCostContext context)
			{
				context.activatorBody.inventory.RemoveItem(RoR2Content.Items.ArtifactKey, context.cost);
				MultiShopCardUtils.OnNonMoneyPurchase(context);
			},
			colorIndex = ColorCatalog.ColorIndex.Artifact
		});
		Register(CostTypeIndex.TreasureCacheItem, new CostTypeDef
		{
			costStringFormatToken = "COST_TREASURECACHEITEM_FORMAT",
			isAffordable = delegate(CostTypeDef costTypeDef, CostTypeDef.IsAffordableContext context)
			{
				CharacterBody component10 = context.activator.gameObject.GetComponent<CharacterBody>();
				if ((bool)component10)
				{
					Inventory inventory4 = component10.inventory;
					if ((bool)inventory4)
					{
						return inventory4.GetItemCount(RoR2Content.Items.TreasureCache) >= context.cost;
					}
				}
				return false;
			},
			payCost = delegate(CostTypeDef costTypeDef, CostTypeDef.PayCostContext context)
			{
				context.activatorBody.inventory.RemoveItem(RoR2Content.Items.TreasureCache, context.cost);
				MultiShopCardUtils.OnNonMoneyPurchase(context);
			},
			colorIndex = ColorCatalog.ColorIndex.Tier1Item
		});
		Register(CostTypeIndex.TreasureCacheVoidItem, new CostTypeDef
		{
			costStringFormatToken = "COST_TREASURECACHEVOIDITEM_FORMAT",
			isAffordable = delegate(CostTypeDef costTypeDef, CostTypeDef.IsAffordableContext context)
			{
				CharacterBody component11 = context.activator.gameObject.GetComponent<CharacterBody>();
				if ((bool)component11)
				{
					Inventory inventory5 = component11.inventory;
					if ((bool)inventory5)
					{
						return inventory5.GetItemCount(DLC1Content.Items.TreasureCacheVoid) >= context.cost;
					}
				}
				return false;
			},
			payCost = delegate(CostTypeDef costTypeDef, CostTypeDef.PayCostContext context)
			{
				context.activatorBody.inventory.RemoveItem(DLC1Content.Items.TreasureCacheVoid, context.cost);
				MultiShopCardUtils.OnNonMoneyPurchase(context);
			},
			colorIndex = ColorCatalog.ColorIndex.VoidItem
		});
		modHelper.CollectAndRegisterAdditionalEntries(ref costTypeDefs);
		static bool IsAffordableItem(CostTypeDef costTypeDef, CostTypeDef.IsAffordableContext context)
		{
			CharacterBody component12 = context.activator.GetComponent<CharacterBody>();
			if ((bool)component12)
			{
				Inventory inventory6 = component12.inventory;
				if ((bool)inventory6)
				{
					return inventory6.HasAtLeastXTotalItemsOfTier(costTypeDef.itemTier, context.cost);
				}
			}
			return false;
		}
		static void PayCostItems(CostTypeDef costTypeDef, CostTypeDef.PayCostContext context)
		{
			List<ItemIndex> itemsToTake;
			if ((bool)context.activatorBody)
			{
				Inventory inventory7 = context.activatorBody.inventory;
				if ((bool)inventory7)
				{
					itemsToTake = CollectionPool<ItemIndex, List<ItemIndex>>.RentCollection();
					WeightedSelection<ItemIndex> weightedSelection2 = new WeightedSelection<ItemIndex>();
					WeightedSelection<ItemIndex> weightedSelection3 = new WeightedSelection<ItemIndex>();
					WeightedSelection<ItemIndex> weightedSelection4 = new WeightedSelection<ItemIndex>();
					foreach (ItemIndex allItem in ItemCatalog.allItems)
					{
						if (allItem != context.avoidedItemIndex)
						{
							int itemCount = inventory7.GetItemCount(allItem);
							if (itemCount > 0)
							{
								ItemDef itemDef = ItemCatalog.GetItemDef(allItem);
								if (itemDef.tier == costTypeDef.itemTier)
								{
									(itemDef.ContainsTag(ItemTag.PriorityScrap) ? weightedSelection4 : (itemDef.ContainsTag(ItemTag.Scrap) ? weightedSelection3 : weightedSelection2)).AddChoice(allItem, itemCount);
								}
							}
						}
					}
					TakeItemsFromWeightedSelection(weightedSelection4);
					TakeItemsFromWeightedSelection(weightedSelection3);
					TakeItemsFromWeightedSelection(weightedSelection2);
					for (int i = itemsToTake.Count; i < context.cost; i++)
					{
						itemsToTake.Add(context.avoidedItemIndex);
					}
					bool flag = false;
					for (int j = 0; j < itemsToTake.Count; j++)
					{
						ItemIndex itemIndex = itemsToTake[j];
						context.results.itemsTaken.Add(itemIndex);
						if (itemIndex == DLC1Content.Items.RegeneratingScrap.itemIndex)
						{
							flag = true;
							inventory7.GiveItem(DLC1Content.Items.RegeneratingScrapConsumed);
							EntitySoundManager.EmitSoundServer(NetworkSoundEventCatalog.FindNetworkSoundEventIndex("Play_item_proc_regenScrap_consume"), context.activatorBody.gameObject);
							ModelLocator component13 = context.activatorBody.GetComponent<ModelLocator>();
							if ((bool)component13)
							{
								Transform modelTransform = component13.modelTransform;
								if ((bool)modelTransform)
								{
									CharacterModel component14 = modelTransform.GetComponent<CharacterModel>();
									if ((bool)component14)
									{
										List<GameObject> itemDisplayObjects = component14.GetItemDisplayObjects(DLC1Content.Items.RegeneratingScrap.itemIndex);
										if (itemDisplayObjects.Count > 0)
										{
											GameObject gameObject = itemDisplayObjects[0];
											GameObject effectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/RegeneratingScrap/RegeneratingScrapExplosionDisplay.prefab").WaitForCompletion();
											EffectData effectData = new EffectData
											{
												origin = gameObject.transform.position,
												rotation = gameObject.transform.rotation
											};
											EffectManager.SpawnEffect(effectPrefab, effectData, transmit: true);
										}
									}
								}
							}
							EffectManager.SimpleMuzzleFlash(Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/RegeneratingScrap/RegeneratingScrapExplosionInPrinter.prefab").WaitForCompletion(), context.purchasedObject, "DropPivot", transmit: true);
						}
						inventory7.RemoveItem(itemIndex);
					}
					if (flag)
					{
						CharacterMasterNotificationQueue.SendTransformNotification(context.activatorBody.master, DLC1Content.Items.RegeneratingScrap.itemIndex, DLC1Content.Items.RegeneratingScrapConsumed.itemIndex, CharacterMasterNotificationQueue.TransformationType.Default);
					}
					CollectionPool<ItemIndex, List<ItemIndex>>.ReturnCollection(itemsToTake);
				}
			}
			MultiShopCardUtils.OnNonMoneyPurchase(context);
			void TakeItemsFromWeightedSelection(WeightedSelection<ItemIndex> weightedSelection)
			{
				while (weightedSelection.Count > 0 && itemsToTake.Count < context.cost)
				{
					int choiceIndex2 = weightedSelection.EvaluateToChoiceIndex(context.rng.nextNormalizedFloat);
					TakeItemFromWeightedSelection(weightedSelection, choiceIndex2);
				}
			}
		}
		static void PayEquipment(CostTypeDef costTypeDef, CostTypeDef.PayCostContext context)
		{
			Inventory inventory8 = context.activatorBody.inventory;
			EquipmentIndex equipmentIndex = inventory8.GetEquipmentIndex();
			if ((bool)inventory8)
			{
				inventory8.SetEquipmentIndex(EquipmentIndex.None);
			}
			context.results.equipmentTaken.Add(equipmentIndex);
			MultiShopCardUtils.OnNonMoneyPurchase(context);
		}
		void TakeItemFromWeightedSelection(WeightedSelection<ItemIndex> weightedSelection, int choiceIndex)
		{
			WeightedSelection<ItemIndex>.ChoiceInfo choice = weightedSelection.GetChoice(choiceIndex);
			ItemIndex value = choice.value;
			int num3 = (int)choice.weight;
			num3--;
			if (num3 <= 0)
			{
				weightedSelection.RemoveChoice(choiceIndex);
			}
			else
			{
				weightedSelection.ModifyChoiceWeight(choiceIndex, num3);
			}
			P_2.itemsToTake.Add(value);
		}
	}

	public static CostTypeDef GetCostTypeDef(CostTypeIndex costTypeIndex)
	{
		return ArrayUtils.GetSafe(costTypeDefs, (int)costTypeIndex);
	}
}
