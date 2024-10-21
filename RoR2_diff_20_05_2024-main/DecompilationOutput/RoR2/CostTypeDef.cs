using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using UnityEngine;

namespace RoR2;

public class CostTypeDef
{
	public delegate void BuildCostStringDelegate(CostTypeDef costTypeDef, BuildCostStringContext context);

	public struct BuildCostStringContext
	{
		public StringBuilder stringBuilder;

		public int cost;
	}

	public delegate Color32 GetCostColorDelegate(CostTypeDef costTypeDef, GetCostColorContext context);

	public struct GetCostColorContext
	{
		public bool forWorldDisplay;
	}

	public delegate void BuildCostStringStyledDelegate(CostTypeDef costTypeDef, BuildCostStringStyledContext context);

	public struct BuildCostStringStyledContext
	{
		public StringBuilder stringBuilder;

		public int cost;

		public bool forWorldDisplay;

		public bool includeColor;
	}

	public delegate bool IsAffordableDelegate(CostTypeDef costTypeDef, IsAffordableContext context);

	public struct IsAffordableContext
	{
		public int cost;

		public Interactor activator;
	}

	public delegate void PayCostDelegate(CostTypeDef costTypeDef, PayCostContext context);

	public struct PayCostContext
	{
		public int cost;

		public Interactor activator;

		public CharacterBody activatorBody;

		public CharacterMaster activatorMaster;

		public GameObject purchasedObject;

		public PayCostResults results;

		public Xoroshiro128Plus rng;

		public ItemIndex avoidedItemIndex;
	}

	public class PayCostResults
	{
		public List<ItemIndex> itemsTaken = new List<ItemIndex>();

		public List<EquipmentIndex> equipmentTaken = new List<EquipmentIndex>();
	}

	public string name;

	public ItemTier itemTier = ItemTier.NoTier;

	public ColorCatalog.ColorIndex colorIndex = ColorCatalog.ColorIndex.Error;

	public string costStringFormatToken;

	public string costStringStyle;

	public bool saturateWorldStyledCostString = true;

	public bool darkenWorldStyledCostString = true;

	public BuildCostStringDelegate buildCostString { private get; set; } = BuildCostStringDefault;

	public GetCostColorDelegate getCostColor { private get; set; } = GetCostColorDefault;

	public BuildCostStringStyledDelegate buildCostStringStyled { private get; set; } = BuildCostStringStyledDefault;

	public IsAffordableDelegate isAffordable { private get; set; }

	public PayCostDelegate payCost { private get; set; }

	public void BuildCostString(int cost, [NotNull] StringBuilder stringBuilder)
	{
		buildCostString(this, new BuildCostStringContext
		{
			cost = cost,
			stringBuilder = stringBuilder
		});
	}

	public static void BuildCostStringDefault(CostTypeDef costTypeDef, BuildCostStringContext context)
	{
		context.stringBuilder.Append(Language.GetStringFormatted(costTypeDef.costStringFormatToken, context.cost));
	}

	public Color32 GetCostColor(bool forWorldDisplay)
	{
		return getCostColor(this, new GetCostColorContext
		{
			forWorldDisplay = forWorldDisplay
		});
	}

	public static Color32 GetCostColorDefault(CostTypeDef costTypeDef, GetCostColorContext context)
	{
		Color32 color = ColorCatalog.GetColor(costTypeDef.colorIndex);
		if (context.forWorldDisplay)
		{
			Color.RGBToHSV(color, out var H, out var S, out var V);
			if (costTypeDef.saturateWorldStyledCostString && S > 0f)
			{
				S = 1f;
			}
			if (costTypeDef.darkenWorldStyledCostString)
			{
				V *= 0.5f;
			}
			color = Color.HSVToRGB(H, S, V);
		}
		return color;
	}

	public void BuildCostStringStyled(int cost, [NotNull] StringBuilder stringBuilder, bool forWorldDisplay, bool includeColor = true)
	{
		buildCostStringStyled(this, new BuildCostStringStyledContext
		{
			cost = cost,
			forWorldDisplay = forWorldDisplay,
			stringBuilder = stringBuilder,
			includeColor = includeColor
		});
	}

	public static void BuildCostStringStyledDefault(CostTypeDef costTypeDef, BuildCostStringStyledContext context)
	{
		StringBuilder stringBuilder = context.stringBuilder;
		stringBuilder.Append("<nobr>");
		if (costTypeDef.costStringStyle != null)
		{
			stringBuilder.Append("<style=");
			stringBuilder.Append(costTypeDef.costStringStyle);
			stringBuilder.Append(">");
		}
		if (context.includeColor)
		{
			Color32 costColor = costTypeDef.GetCostColor(context.forWorldDisplay);
			stringBuilder.Append("<color=#");
			stringBuilder.AppendColor32RGBHexValues(costColor);
			stringBuilder.Append(">");
		}
		costTypeDef.BuildCostString(context.cost, context.stringBuilder);
		if (context.includeColor)
		{
			stringBuilder.Append("</color>");
		}
		if (costTypeDef.costStringStyle != null)
		{
			stringBuilder.Append("</style>");
		}
		stringBuilder.Append("</nobr>");
	}

	public bool IsAffordable(int cost, Interactor activator)
	{
		return isAffordable(this, new IsAffordableContext
		{
			cost = cost,
			activator = activator
		});
	}

	public PayCostResults PayCost(int cost, Interactor activator, GameObject purchasedObject, Xoroshiro128Plus rng, ItemIndex avoidedItemIndex)
	{
		PayCostResults payCostResults = new PayCostResults();
		CharacterBody component = activator.GetComponent<CharacterBody>();
		payCost(this, new PayCostContext
		{
			cost = cost,
			activator = activator,
			activatorBody = component,
			activatorMaster = (component ? component.master : null),
			purchasedObject = purchasedObject,
			results = payCostResults,
			rng = rng,
			avoidedItemIndex = avoidedItemIndex
		});
		return payCostResults;
	}
}
