using System;
using JetBrains.Annotations;
using UnityEngine;

namespace RoR2.UI.LogBook;

public class Entry
{
	public delegate EntryStatus GetStatusDelegate(in Entry entry, [NotNull] UserProfile viewerProfile);

	public delegate TooltipContent GetTooltipContentDelegate(in Entry entry, UserProfile viewerProfile, EntryStatus entryStatus);

	[NotNull]
	public delegate string GetDisplayNameDelegate(in Entry entry, [NotNull] UserProfile viewerProfile);

	public delegate bool IsWIPDelegate(in Entry entry);

	public string nameToken;

	public CategoryDef category;

	public Texture iconTexture;

	public Texture bgTexture;

	public Color color;

	public GameObject modelPrefab;

	public object extraData;

	public Action<PageBuilder> pageBuilderMethod;

	[CanBeNull]
	public ViewablesCatalog.Node viewableNode;

	[NotNull]
	public GetStatusDelegate getStatusImplementation { private get; set; } = GetStatusDefault;

	public GetTooltipContentDelegate getTooltipContentImplementation { private get; set; } = GetTooltipContentDefault;

	[NotNull]
	public GetDisplayNameDelegate getDisplayNameImplementation { private get; set; } = GetDisplayNameDefault;

	[NotNull]
	public GetDisplayNameDelegate getCategoryDisplayNameImplementation { private get; set; } = GetCategoryDisplayNameDefault;

	[NotNull]
	public IsWIPDelegate isWIPImplementation { private get; set; } = IsWIPReturnFalse;

	public bool isWip => isWIPImplementation(in this);

	public static EntryStatus GetStatusDefault(in Entry entry, [NotNull] UserProfile viewerProfile)
	{
		return EntryStatus.Unimplemented;
	}

	public EntryStatus GetStatus([NotNull] UserProfile viewerProfile)
	{
		return getStatusImplementation(in this, viewerProfile);
	}

	public static TooltipContent GetTooltipContentDefault(in Entry entry, [NotNull] UserProfile viewerProfile, EntryStatus entryStatus)
	{
		TooltipContent result = default(TooltipContent);
		result.overrideTitleText = entry.GetDisplayName(viewerProfile);
		result.overrideBodyText = entry.GetCategoryDisplayName(viewerProfile);
		return result;
	}

	public TooltipContent GetTooltipContent([NotNull] UserProfile viewerProfile, EntryStatus entryStatus)
	{
		return getTooltipContentImplementation(in this, viewerProfile, entryStatus);
	}

	[NotNull]
	public static string GetDisplayNameDefault(in Entry entry, [NotNull] UserProfile viewerProfile)
	{
		return Language.GetString(entry.nameToken ?? string.Empty);
	}

	[NotNull]
	public string GetDisplayName([NotNull] UserProfile viewerProfile)
	{
		return getDisplayNameImplementation(in this, viewerProfile);
	}

	[NotNull]
	public static string GetCategoryDisplayNameDefault(in Entry entry, [NotNull] UserProfile viewerProfile)
	{
		return Language.GetString(entry.category?.nameToken ?? string.Empty);
	}

	[NotNull]
	public string GetCategoryDisplayName([NotNull] UserProfile viewerProfile)
	{
		return getCategoryDisplayNameImplementation(in this, viewerProfile);
	}

	private static bool IsWIPReturnFalse(in Entry entry)
	{
		return false;
	}
}
