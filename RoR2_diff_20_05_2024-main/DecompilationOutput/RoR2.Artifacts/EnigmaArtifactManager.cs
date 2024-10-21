using System;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace RoR2.Artifacts;

public static class EnigmaArtifactManager
{
	private static readonly Xoroshiro128Plus serverInitialEquipmentRng = new Xoroshiro128Plus(0uL);

	private static readonly Xoroshiro128Plus serverActivationEquipmentRng = new Xoroshiro128Plus(0uL);

	private static List<EquipmentIndex> validEquipment = new List<EquipmentIndex>();

	private static BodyIndex toolbotBodyIndex = BodyIndex.None;

	private static ArtifactDef myArtifact => RoR2Content.Artifacts.enigmaArtifactDef;

	[SystemInitializer(new Type[]
	{
		typeof(ArtifactCatalog),
		typeof(BodyCatalog)
	})]
	private static void Init()
	{
		RunArtifactManager.onArtifactEnabledGlobal += OnArtifactEnabled;
		RunArtifactManager.onArtifactDisabledGlobal += OnArtifactDisabled;
		Run.onRunStartGlobal += OnRunStartGlobal;
		toolbotBodyIndex = BodyCatalog.FindBodyIndex("ToolbotBody");
	}

	private static void OnRunStartGlobal(Run run)
	{
		if (!NetworkServer.active)
		{
			return;
		}
		serverInitialEquipmentRng.ResetSeed(run.seed);
		serverActivationEquipmentRng.ResetSeed(serverInitialEquipmentRng.nextUlong);
		validEquipment.Clear();
		foreach (EquipmentIndex enigmaEquipment in EquipmentCatalog.enigmaEquipmentList)
		{
			EquipmentDef equipmentDef = EquipmentCatalog.GetEquipmentDef(enigmaEquipment);
			if ((bool)equipmentDef && (!equipmentDef.requiredExpansion || run.IsExpansionEnabled(equipmentDef.requiredExpansion)))
			{
				validEquipment.Add(enigmaEquipment);
			}
		}
	}

	private static void OnArtifactEnabled(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
	{
		if (!(artifactDef != myArtifact) && NetworkServer.active)
		{
			CharacterBody.onBodyStartGlobal += OnBodyStartGlobalServer;
			EquipmentSlot.onServerEquipmentActivated += OnServerEquipmentActivated;
		}
	}

	private static void OnArtifactDisabled(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
	{
		if (!(artifactDef != myArtifact))
		{
			EquipmentSlot.onServerEquipmentActivated -= OnServerEquipmentActivated;
			CharacterBody.onBodyStartGlobal -= OnBodyStartGlobalServer;
		}
	}

	private static void OnBodyStartGlobalServer(CharacterBody characterBody)
	{
		if (characterBody.isPlayerControlled)
		{
			OnPlayerCharacterBodyStartServer(characterBody);
		}
	}

	private static void OnPlayerCharacterBodyStartServer(CharacterBody characterBody)
	{
		Inventory inventory = characterBody.inventory;
		int val = ((characterBody.bodyIndex != toolbotBodyIndex) ? 1 : 2);
		int num = Math.Max(inventory.GetEquipmentSlotCount(), val);
		for (int i = 0; i < num; i++)
		{
			if (inventory.GetEquipment((uint)i).equipmentIndex == EquipmentIndex.None)
			{
				EquipmentIndex randomEquipment = GetRandomEquipment(serverInitialEquipmentRng, (int)(i + characterBody.bodyIndex));
				EquipmentState equipmentState = new EquipmentState(randomEquipment, Run.FixedTimeStamp.negativeInfinity, 1);
				inventory.SetEquipment(equipmentState, (uint)i);
			}
		}
	}

	private static void OnServerEquipmentActivated(EquipmentSlot equipmentSlot, EquipmentIndex equipmentIndex)
	{
		EquipmentIndex randomEquipment = GetRandomEquipment(serverActivationEquipmentRng, (int)equipmentIndex);
		equipmentSlot.characterBody.inventory.SetEquipmentIndex(randomEquipment);
	}

	private static EquipmentIndex GetRandomEquipment(Xoroshiro128Plus rng, int offset)
	{
		int count = validEquipment.Count;
		int num = rng.RangeInt(0, count);
		num += offset;
		num %= count;
		return validEquipment[num];
	}
}
