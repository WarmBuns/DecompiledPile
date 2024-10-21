using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using HG;
using UnityEngine;

namespace RoR2;

public static class ConCommandArgExtensions
{
	public abstract class BaseCharacterBodyInstanceSearchHandler
	{
		public abstract bool ShouldHandle(ConCommandArgs args, string argString);

		public abstract void GetResults(ConCommandArgs args, string argString, List<CharacterBody> dest);
	}

	public class NearestCharacterBodyInstanceSearchHandler : BaseCharacterBodyInstanceSearchHandler
	{
		public override bool ShouldHandle(ConCommandArgs args, string argString)
		{
			return argString.Equals("nearest", StringComparison.OrdinalIgnoreCase);
		}

		public override void GetResults(ConCommandArgs args, string argString, List<CharacterBody> dest)
		{
			CharacterBody senderBody = args.TryGetSenderBody();
			if (!senderBody)
			{
				throw new ConCommandException($"Sender must have a valid body to use \"{argString}\".");
			}
			Vector3 myPosition = senderBody.corePosition;
			ReadOnlyCollection<CharacterBody> readOnlyInstancesList = CharacterBody.readOnlyInstancesList;
			dest.AddRange(from candidateBody in readOnlyInstancesList
				where (object)senderBody != candidateBody
				orderby (candidateBody.corePosition - myPosition).sqrMagnitude
				select candidateBody);
		}
	}

	public class SenderCharacterBodyInstanceSearchHandler : BaseCharacterBodyInstanceSearchHandler
	{
		public override bool ShouldHandle(ConCommandArgs args, string argString)
		{
			return argString.Equals("me", StringComparison.OrdinalIgnoreCase);
		}

		public override void GetResults(ConCommandArgs args, string argString, List<CharacterBody> dest)
		{
			CharacterBody characterBody = args.TryGetSenderBody();
			if (!characterBody)
			{
				throw new ConCommandException($"Sender must have a valid body to use \"{argString}\".");
			}
			dest.Add(characterBody);
		}
	}

	private static readonly BaseCharacterBodyInstanceSearchHandler[] finders = new BaseCharacterBodyInstanceSearchHandler[2]
	{
		new SenderCharacterBodyInstanceSearchHandler(),
		new NearestCharacterBodyInstanceSearchHandler()
	};

	public static BodyIndex? TryGetArgBodyIndex(this ConCommandArgs args, int index)
	{
		if (index < args.userArgs.Count)
		{
			BodyIndex bodyIndex = BodyCatalog.FindBodyIndexCaseInsensitive(args[index]);
			if (bodyIndex != BodyIndex.None)
			{
				return bodyIndex;
			}
		}
		return null;
	}

	public static BodyIndex GetArgBodyIndex(this ConCommandArgs args, int index)
	{
		return args.TryGetArgBodyIndex(index) ?? throw new ConCommandException($"Argument {index} is not a valid body name.");
	}

	public static EquipmentIndex? TryGetArgEquipmentIndex(this ConCommandArgs args, int index)
	{
		string text = args.TryGetArgString(index);
		if (text != null)
		{
			EquipmentIndex equipmentIndex = EquipmentCatalog.FindEquipmentIndex(text);
			if (equipmentIndex != EquipmentIndex.None || text.Equals("None", StringComparison.Ordinal))
			{
				return equipmentIndex;
			}
		}
		return null;
	}

	public static EquipmentIndex GetArgEquipmentIndex(this ConCommandArgs args, int index)
	{
		return args.TryGetArgEquipmentIndex(index) ?? throw new ConCommandException("No EquipmentIndex is defined for an equipment named '" + args.TryGetArgString(index) + "'. Use the \"equipment_list\" command to get a list of all valid equipment.");
	}

	public static void GetArgCharacterBodyInstances(this ConCommandArgs args, int argIndex, List<CharacterBody> dest)
	{
		if (argIndex >= args.userArgs.Count)
		{
			return;
		}
		string argString = args[argIndex];
		args.TryGetSenderBody();
		List<CharacterBody> list = CollectionPool<CharacterBody, List<CharacterBody>>.RentCollection();
		try
		{
			for (int i = 0; i < finders.Length; i++)
			{
				BaseCharacterBodyInstanceSearchHandler baseCharacterBodyInstanceSearchHandler = finders[i];
				if (baseCharacterBodyInstanceSearchHandler.ShouldHandle(args, argString))
				{
					baseCharacterBodyInstanceSearchHandler.GetResults(args, argString, list);
					dest.AddRange(list);
					break;
				}
			}
		}
		catch (ConCommandException ex)
		{
			throw new ConCommandException($"Argument {argIndex}: {ex.Message}");
		}
		finally
		{
			list = CollectionPool<CharacterBody, List<CharacterBody>>.ReturnCollection(list);
		}
	}

	public static void TryGetArgCharacterBodyInstances(this ConCommandArgs args, int argIndex, List<CharacterBody> dest)
	{
		try
		{
			args.GetArgCharacterBodyInstances(argIndex, dest);
		}
		catch (ConCommandException)
		{
		}
	}

	public static CharacterBody GetArgCharacterBodyInstance(this ConCommandArgs args, int argIndex)
	{
		List<CharacterBody> list = CollectionPool<CharacterBody, List<CharacterBody>>.RentCollection();
		try
		{
			args.GetArgCharacterBodyInstances(argIndex, list);
			return (list.Count > 0) ? list[0] : null;
		}
		finally
		{
			list = CollectionPool<CharacterBody, List<CharacterBody>>.ReturnCollection(list);
		}
	}

	public static CharacterBody TryGetArgCharacterBodyInstance(this ConCommandArgs args, int argIndex)
	{
		try
		{
			return args.GetArgCharacterBodyInstance(argIndex);
		}
		catch (ConCommandException)
		{
			return null;
		}
	}

	public static CharacterMaster GetArgCharacterMasterInstance(this ConCommandArgs args, int argIndex)
	{
		CharacterBody argCharacterBodyInstance = args.GetArgCharacterBodyInstance(argIndex);
		if ((bool)argCharacterBodyInstance)
		{
			return argCharacterBodyInstance.master;
		}
		return null;
	}

	public static CharacterMaster TryGetArgCharacterMasterInstance(this ConCommandArgs args, int argIndex)
	{
		try
		{
			return args.GetArgCharacterMasterInstance(argIndex);
		}
		catch (ConCommandException)
		{
			return null;
		}
	}

	public static ItemIndex? TryGetArgItemIndex(this ConCommandArgs args, int index)
	{
		string text = args.TryGetArgString(index);
		if (text != null)
		{
			ItemIndex itemIndex = ItemCatalog.FindItemIndex(text);
			if (itemIndex != ItemIndex.None || text.Equals("None", StringComparison.Ordinal))
			{
				return itemIndex;
			}
		}
		return null;
	}

	public static ItemIndex GetArgItemIndex(this ConCommandArgs args, int index)
	{
		return args.TryGetArgItemIndex(index) ?? throw new ConCommandException("No ItemIndex is defined for an item named '" + args.TryGetArgString(index) + "'. Use the \"item_list\" command to get a list of all valid items.");
	}

	public static MasterCatalog.MasterIndex? TryGetArgMasterIndex(this ConCommandArgs args, int argIndex)
	{
		if (argIndex < args.userArgs.Count)
		{
			string text = args[argIndex];
			MasterCatalog.MasterIndex masterIndex = MasterCatalog.FindMasterIndex(text);
			if (masterIndex != MasterCatalog.MasterIndex.none || text.Equals("None", StringComparison.OrdinalIgnoreCase))
			{
				return masterIndex;
			}
		}
		return null;
	}

	public static MasterCatalog.MasterIndex GetArgMasterIndex(this ConCommandArgs args, int index)
	{
		return args.TryGetArgMasterIndex(index) ?? throw new ConCommandException($"Argument {index} is not a valid character master prefab name.");
	}
}
