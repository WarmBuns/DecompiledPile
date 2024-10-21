using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoR2;

public class BodySplitter
{
	public CharacterBody body;

	public float moneyMultiplier;

	private int _count = 1;

	public readonly MasterSummon masterSummon;

	private Dictionary<CharacterBody, Vector3> spawnedBodyVelocity = new Dictionary<CharacterBody, Vector3>();

	public int count
	{
		get
		{
			return _count;
		}
		set
		{
			if (value <= 0)
			{
				throw new ArgumentException("'value' cannot be non-positive.", "value");
			}
			_count = value;
		}
	}

	public Vector3 splinterInitialVelocityLocal { get; set; } = Vector3.zero;

	public float minSpawnCircleRadius { get; set; }

	public BodySplitter()
	{
		masterSummon = new MasterSummon
		{
			masterPrefab = null,
			ignoreTeamMemberLimit = false,
			useAmbientLevel = null,
			teamIndexOverride = null
		};
	}

	public void Perform()
	{
		PerformInternal(masterSummon);
	}

	private void PerformInternal(MasterSummon masterSummon)
	{
		if ((object)body == null)
		{
			throw new InvalidOperationException("'body' is null.");
		}
		if (!body)
		{
			throw new InvalidOperationException("'body' is an invalid object.");
		}
		GameObject masterPrefab = masterSummon.masterPrefab;
		CharacterMaster obj = masterPrefab.GetComponent<CharacterMaster>() ?? throw new InvalidOperationException("'splinterMasterPrefab' does not have a CharacterMaster component.");
		if (!obj.masterIndex.isValid)
		{
			throw new InvalidOperationException("'splinterMasterPrefab' is not registered with MasterCatalog.");
		}
		if ((object)MasterCatalog.GetMasterPrefab(obj.masterIndex) != masterPrefab)
		{
			throw new InvalidOperationException("'splinterMasterPrefab' is not a prefab.");
		}
		Vector3 position = body.transform.position;
		float y = Quaternion.LookRotation(body.inputBank.aimDirection).eulerAngles.y;
		GameObject bodyPrefab = obj.bodyPrefab;
		bodyPrefab.GetComponent<CharacterBody>();
		float num = CalcBodyXZRadius(bodyPrefab);
		float a = 0f;
		if (count > 1)
		{
			a = num / Mathf.Sin(MathF.PI / (float)count);
		}
		a = Mathf.Max(a, minSpawnCircleRadius);
		masterSummon.summonerBodyObject = body.gameObject;
		masterSummon.inventoryToCopy = body.inventory;
		masterSummon.loadout = (body.master ? body.master.loadout : null);
		masterSummon.inventoryItemCopyFilter = CopyItemFilter;
		foreach (float item in new DegreeSlices(count, 0.5f))
		{
			Quaternion quaternion = Quaternion.Euler(0f, y + item + 180f, 0f);
			Vector3 vector = quaternion * Vector3.forward;
			float num2 = a;
			if (Physics.Raycast(new Ray(position, vector), out var hitInfo, a + num, LayerIndex.world.intVal, QueryTriggerInteraction.Ignore))
			{
				num2 = hitInfo.distance - num;
			}
			Vector3 position2 = position + vector * num2;
			masterSummon.position = position2;
			masterSummon.rotation = quaternion;
			try
			{
				CharacterMaster characterMaster = masterSummon.Perform();
				if ((bool)characterMaster)
				{
					CharacterBody characterBody = characterMaster.GetBody();
					if ((bool)characterBody)
					{
						spawnedBodyVelocity.Add(characterBody, quaternion * splinterInitialVelocityLocal);
						characterMaster.money = (uint)Mathf.FloorToInt((float)body.master.money * moneyMultiplier);
						characterBody.characterMotor.onMotorStart += OnCharacterMotorStart;
					}
				}
			}
			catch (Exception message)
			{
				Debug.LogError(message);
			}
		}
	}

	private void OnCharacterMotorStart(CharacterBody characterBody)
	{
		if (spawnedBodyVelocity.TryGetValue(characterBody, out var value))
		{
			AddBodyVelocity(characterBody, value);
		}
		else
		{
			Debug.LogError("Split characterBody has not stored split velocity");
		}
	}

	private static float CalcBodyXZRadius(GameObject bodyPrefab)
	{
		Collider component = bodyPrefab.GetComponent<Collider>();
		if (!component)
		{
			return 0f;
		}
		Vector3 position = bodyPrefab.transform.position;
		Bounds bounds = component.bounds;
		Vector3 min = bounds.min;
		Vector3 max = bounds.max;
		return Mathf.Max(Mathf.Max(Mathf.Max(Mathf.Max(0f, position.x - min.x), position.z - min.z), max.x - position.x), max.z - position.z);
	}

	private static void AddBodyVelocity(CharacterBody body, Vector3 additionalVelocity)
	{
		IPhysMotor component = body.GetComponent<IPhysMotor>();
		if (component != null)
		{
			PhysForceInfo physForceInfo = PhysForceInfo.Create();
			physForceInfo.force = additionalVelocity;
			physForceInfo.massIsOne = true;
			physForceInfo.ignoreGroundStick = true;
			physForceInfo.disableAirControlUntilCollision = false;
			component.ApplyForceImpulse(in physForceInfo);
		}
	}

	private static bool CopyItemFilter(ItemIndex itemIndex)
	{
		ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
		if ((bool)itemDef)
		{
			return itemDef.tier == ItemTier.NoTier;
		}
		return false;
	}
}
