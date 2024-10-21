using System;
using System.Collections.Generic;
using System.Text;
using Grumpy;
using UnityEngine;

namespace RoR2.Orbs;

public class OrbPool : GenericPool<Orb>
{
	protected Type _OrbType;

	protected static bool _VerboseLogging = false;

	protected static Dictionary<Type, OrbPool> _OrbPoolMap = new Dictionary<Type, OrbPool>();

	protected static bool _UsePools = true;

	public static bool VerboseLogging => _VerboseLogging;

	public static bool UsePools => _UsePools;

	public static void ToggleVerboseLogging()
	{
		_VerboseLogging = !_VerboseLogging;
		Debug.LogFormat("Orb pooling {0}", _VerboseLogging ? "ENABLED" : "DISABLED");
	}

	public void Initialize(Type inOrbType, int inMaxSize, int inStartSize)
	{
		_OrbType = inOrbType;
		base.PoolName = _OrbType.ToString();
		base.Initialize(inMaxSize, inStartSize);
	}

	protected override Orb CreateNewObject(bool inPoolEntry = true)
	{
		if (_OrbType != null)
		{
			Orb orb = Activator.CreateInstance(_OrbType) as Orb;
			orb.instanceID = _Pool.Count + _InUse.Count;
			if (VerboseLogging)
			{
				Debug.LogFormat("OrbPool: {0} created instance {1} ({2})...", base.PoolName, _OrbType.ToString(), orb.instanceID);
			}
			return orb;
		}
		Debug.LogError("OrbPool: No type set!");
		return null;
	}

	protected override void DestroyObject(Orb inObject)
	{
	}

	public void DumpPool(StringBuilder inDumpSB = null)
	{
		StringBuilder obj = inDumpSB ?? new StringBuilder();
		obj.AppendFormat("OrbPool {0}\n", base.PoolName);
		obj.AppendFormat("\t Pool Count: {0}\n", _Pool.Count);
		obj.AppendFormat("\tInUse Count: {0}\n", _InUse.Count);
	}

	public static void TogglePooling()
	{
		_UsePools = !_UsePools;
		Debug.LogFormat("Orb pooling is {0}", _UsePools ? "ENABLED" : "DISABLED");
	}

	protected static void CreatePoolIfNotPresent(Type inOrbType)
	{
		if (!_OrbPoolMap.ContainsKey(inOrbType))
		{
			OrbPool orbPool = new OrbPool();
			orbPool.AlwaysGrowable = true;
			orbPool.Initialize(inOrbType, 1, 1);
			_OrbPoolMap.Add(inOrbType, orbPool);
		}
	}

	public static Orb GetPooledOrb(Type inOrbType)
	{
		CreatePoolIfNotPresent(inOrbType);
		OrbPool orbPool = _OrbPoolMap[inOrbType];
		Orb @object = orbPool.GetObject();
		if (@object != null)
		{
			@object.SetOwningPool(orbPool);
			@object.Reset();
		}
		return @object;
	}

	public static void ClearAllPools()
	{
		Debug.LogWarning("OrbPool: Clearing all pools");
		foreach (OrbPool value in _OrbPoolMap.Values)
		{
			value.Reset();
		}
		_OrbPoolMap.Clear();
	}

	public static void DumpPools()
	{
	}
}
