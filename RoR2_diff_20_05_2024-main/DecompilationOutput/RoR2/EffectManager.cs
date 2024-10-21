using System;
using System.Collections.Generic;
using System.Diagnostics;
using Grumpy;
using RoR2.Audio;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace RoR2;

public static class EffectManager
{
	private class EffectMessage : MessageBase
	{
		public EffectIndex effectIndex;

		public readonly EffectData effectData = new EffectData();

		public override void Serialize(NetworkWriter writer)
		{
			writer.WriteEffectIndex(effectIndex);
			writer.Write(effectData);
		}

		public override void Deserialize(NetworkReader reader)
		{
			effectIndex = reader.ReadEffectIndex();
			reader.ReadEffectData(effectData);
		}
	}

	public static bool DisableAllEffectSpawning = false;

	private static bool _UsePools = true;

	private static Dictionary<GameObject, EffectPool> _EffectPrefabMap = new Dictionary<GameObject, EffectPool>();

	private static Dictionary<GameObject, bool> _ShouldUsePooledEffectMap = new Dictionary<GameObject, bool>();

	private static EffectManagerHelper _LastSpawnedEffect = null;

	private static EffectData _simpleEffectData = null;

	private const float POOL_EXPIRATION_TIME = 45f;

	private const int POOLS_UNLOADING_TIMESTEP = 5;

	private const int MINIMAL_POOLS_AMOUNT_BEFORE_UNLOADING = 5;

	private const int MAXIMUM_POOLS_AMOUNT_BEFORE_UNLOADING_IN_USE_POOLS = 30;

	private static readonly QosChannelIndex qosChannel = QosChannelIndex.effects;

	private static int frameCount = 0;

	private static readonly EffectMessage outgoingEffectMessage = new EffectMessage();

	private static readonly EffectMessage incomingEffectMessage = new EffectMessage();

	public static EffectManagerHelper LastSpawnedEffect
	{
		get
		{
			EffectManagerHelper lastSpawnedEffect = _LastSpawnedEffect;
			_LastSpawnedEffect = null;
			return lastSpawnedEffect;
		}
	}

	public static bool UsePools => _UsePools;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void Init()
	{
		RoR2Application.onUpdate += Update;
		SceneManager.sceneUnloaded += OnSceneUnloaded;
	}

	private static void OnSceneUnloaded(Scene scene)
	{
		KillAllPools();
	}

	public static void TogglePooling()
	{
		_UsePools = !_UsePools;
		UnityEngine.Debug.LogFormat("EffectManager pooling is {0}", _UsePools ? "ENABLED" : "DISABLED");
	}

	public static bool ShouldUsePooledEffect(GameObject inPrefab)
	{
		if (inPrefab != null && UsePools)
		{
			if (!_ShouldUsePooledEffectMap.TryGetValue(inPrefab, out var value))
			{
				VFXAttributes component = inPrefab.GetComponent<VFXAttributes>();
				if (component != null)
				{
					value = !component.DoNotPool;
					_ShouldUsePooledEffectMap.Add(inPrefab, value);
				}
				else
				{
					value = true;
					_ShouldUsePooledEffectMap.Add(inPrefab, value);
				}
			}
			return value;
		}
		return false;
	}

	private static void CreatePoolIfNotPresent(GameObject inPrefab)
	{
		if (!_EffectPrefabMap.ContainsKey(inPrefab))
		{
			EffectPool effectPool = new EffectPool();
			effectPool.PrefabObject = inPrefab;
			effectPool.AlwaysGrowable = true;
			effectPool.AddComponentIfMissing = true;
			effectPool.Initialize(1, 1);
			_EffectPrefabMap.Add(inPrefab, effectPool);
		}
	}

	public static EffectManagerHelper GetPooledEffect(GameObject inPrefab, Vector3 inPosition, Quaternion inRotation)
	{
		CreatePoolIfNotPresent(inPrefab);
		EffectPool effectPool = _EffectPrefabMap[inPrefab];
		EffectManagerHelper @object = effectPool.GetObject();
		if (@object != null)
		{
			@object.SetOwningPool(effectPool);
			@object.Initialize();
			@object.gameObject.transform.position = inPosition;
			@object.gameObject.transform.rotation = inRotation;
		}
		return @object;
	}

	public static EffectManagerHelper GetAndActivatePooledEffect(GameObject inPrefab, Vector3 inPosition, Quaternion inRotation)
	{
		EffectManagerHelper pooledEffect = GetPooledEffect(inPrefab, inPosition, inRotation);
		pooledEffect.Reset(inActivate: true);
		pooledEffect.gameObject.SetActive(value: true);
		pooledEffect.ResetPostActivate();
		return pooledEffect;
	}

	public static EffectManagerHelper GetPooledEffect(GameObject inPrefab, Transform inTransform)
	{
		CreatePoolIfNotPresent(inPrefab);
		EffectPool effectPool = _EffectPrefabMap[inPrefab];
		EffectManagerHelper @object = effectPool.GetObject();
		if (@object != null)
		{
			@object.SetOwningPool(effectPool);
			@object.Initialize();
			@object.gameObject.transform.SetParent(inTransform);
		}
		return @object;
	}

	public static EffectManagerHelper GetAndActivatePooledEffect(GameObject inPrefab, Transform inTransform, bool inResetLocal = false)
	{
		EffectManagerHelper pooledEffect = GetPooledEffect(inPrefab, inTransform);
		if (inResetLocal)
		{
			pooledEffect.gameObject.transform.localPosition = Vector3.zero;
			pooledEffect.gameObject.transform.localRotation = Quaternion.identity;
			pooledEffect.gameObject.transform.localScale = Vector3.one;
		}
		pooledEffect.Reset(inActivate: true);
		pooledEffect.gameObject.SetActive(value: true);
		pooledEffect.ResetPostActivate();
		return pooledEffect;
	}

	public static EffectManagerHelper GetPooledEffect(GameObject inPrefab, Vector3 inPosition, Quaternion inRotation, Transform inTransform)
	{
		CreatePoolIfNotPresent(inPrefab);
		EffectPool effectPool = _EffectPrefabMap[inPrefab];
		EffectManagerHelper @object = effectPool.GetObject();
		if (@object != null)
		{
			@object.SetOwningPool(effectPool);
			@object.Initialize();
			@object.gameObject.transform.localPosition = inPosition;
			@object.gameObject.transform.localRotation = inRotation;
		}
		return @object;
	}

	public static EffectManagerHelper GetAndActivatePooledEffect(GameObject inPrefab, Vector3 inPosition, Quaternion inRotation, Transform inTransform)
	{
		EffectManagerHelper pooledEffect = GetPooledEffect(inPrefab, inPosition, inRotation, inTransform);
		pooledEffect.Reset(inActivate: true);
		pooledEffect.gameObject.SetActive(value: true);
		pooledEffect.ResetPostActivate();
		return pooledEffect;
	}

	public static void ClearPool(GameObject inPrefab)
	{
		if (_EffectPrefabMap.ContainsKey(inPrefab))
		{
			_EffectPrefabMap[inPrefab].Reset();
			_EffectPrefabMap.Remove(inPrefab);
		}
	}

	public static void KillAllPools()
	{
		UnityEngine.Debug.LogWarning("EffectManager: Killing all pools");
		foreach (EffectPool value in _EffectPrefabMap.Values)
		{
			value.Kill();
		}
		_EffectPrefabMap.Clear();
		_ShouldUsePooledEffectMap.Clear();
		GC.Collect();
	}

	public static void ClearAllPools(bool inForceAllClear = false)
	{
		UnityEngine.Debug.LogWarning("EffectManager: Clearing all pools");
		foreach (EffectPool value in _EffectPrefabMap.Values)
		{
			value.Reset(inForceAllClear);
		}
		_EffectPrefabMap.Clear();
		_ShouldUsePooledEffectMap.Clear();
	}

	public static void DumpPools()
	{
	}

	[NetworkMessageHandler(msgType = 52, server = true)]
	private static void HandleEffectServer(NetworkMessage netMsg)
	{
		HandleEffectServerInternal(netMsg);
	}

	[NetworkMessageHandler(msgType = 52, client = true)]
	private static void HandleEffectClient(NetworkMessage netMsg)
	{
		HandleEffectClientInternal(netMsg);
	}

	public static void SpawnEffect(GameObject effectPrefab, EffectData effectData, bool transmit)
	{
		if (DisableAllEffectSpawning)
		{
			return;
		}
		EffectIndex effectIndex = EffectCatalog.FindEffectIndexFromPrefab(effectPrefab);
		if (effectIndex == EffectIndex.Invalid)
		{
			if ((bool)effectPrefab && !string.IsNullOrEmpty(effectPrefab.name))
			{
				UnityEngine.Debug.LogError("Unable to SpawnEffect from prefab named '" + effectPrefab?.name + "'");
			}
			else
			{
				UnityEngine.Debug.LogError($"Unable to SpawnEffect.  Is null? {effectPrefab == null}.  Name = '{effectPrefab?.name}'.\n{new StackTrace()}");
			}
		}
		else
		{
			SpawnEffect(effectIndex, effectData, transmit);
		}
	}

	public static void SpawnEffect(EffectIndex effectIndex, EffectData effectData, bool transmit)
	{
		if (DisableAllEffectSpawning)
		{
			return;
		}
		if (transmit)
		{
			TransmitEffect(effectIndex, effectData);
			if (NetworkServer.active)
			{
				return;
			}
		}
		if (!NetworkClient.active)
		{
			return;
		}
		if (effectData.networkSoundEventIndex != NetworkSoundEventIndex.Invalid)
		{
			PointSoundManager.EmitSoundLocal(NetworkSoundEventCatalog.GetAkIdFromNetworkSoundEventIndex(effectData.networkSoundEventIndex), effectData.origin);
		}
		EffectDef effectDef = EffectCatalog.GetEffectDef(effectIndex);
		if (effectDef == null)
		{
			return;
		}
		string spawnSoundEventName = effectDef.spawnSoundEventName;
		if (!string.IsNullOrEmpty(spawnSoundEventName))
		{
			PointSoundManager.EmitSoundLocal((AkEventIdArg)spawnSoundEventName, effectData.origin);
		}
		SurfaceDef surfaceDef = SurfaceDefCatalog.GetSurfaceDef(effectData.surfaceDefIndex);
		if ((object)surfaceDef != null)
		{
			string impactSoundString = surfaceDef.impactSoundString;
			if (!string.IsNullOrEmpty(impactSoundString))
			{
				PointSoundManager.EmitSoundLocal((AkEventIdArg)impactSoundString, effectData.origin);
			}
		}
		if (!VFXBudget.CanAffordSpawn(effectDef.prefabVfxAttributes) || (effectDef.cullMethod != null && !effectDef.cullMethod(effectData)))
		{
			return;
		}
		EffectData effectData2 = effectData.Clone();
		EffectManagerHelper effectManagerHelper = null;
		if (!ShouldUsePooledEffect(effectDef.prefab))
		{
			EffectComponent component = UnityEngine.Object.Instantiate(effectDef.prefab, effectData2.origin, effectData2.rotation).GetComponent<EffectComponent>();
			if ((bool)component)
			{
				component.effectData = effectData2.Clone();
			}
			return;
		}
		effectManagerHelper = GetPooledEffect(effectDef.prefab, effectData2.origin, effectData2.rotation);
		EffectComponent component2 = effectManagerHelper.gameObject.GetComponent<EffectComponent>();
		if ((bool)component2)
		{
			component2.effectData = effectData2.Clone();
			effectManagerHelper.Reset(inActivate: true);
			effectManagerHelper.gameObject.SetActive(value: true);
			effectManagerHelper.ResetPostActivate();
		}
	}

	private static void Update()
	{
		frameCount++;
		if (frameCount >= 5)
		{
			frameCount = 0;
			_ = _EffectPrefabMap.Count;
			_ = 5;
			UnloadOutdatedPools();
		}
	}

	private static void UnloadOutdatedPools()
	{
		float num = float.PositiveInfinity;
		PrefabComponentPool<EffectManagerHelper> prefabComponentPool = null;
		bool flag = _EffectPrefabMap.Count > 30;
		foreach (EffectPool value in _EffectPrefabMap.Values)
		{
			if (!value.DoNotCull && value.timestamp < num && (value.InUseCount() < 1 || flag))
			{
				num = value.timestamp;
				prefabComponentPool = value;
			}
		}
		if (Time.realtimeSinceStartup - num > 45f)
		{
			prefabComponentPool.Kill();
			_EffectPrefabMap.Remove(prefabComponentPool.PrefabObject);
		}
	}

	private static void TransmitEffect(EffectIndex effectIndex, EffectData effectData, NetworkConnection netOrigin = null)
	{
		outgoingEffectMessage.effectIndex = effectIndex;
		EffectData.Copy(effectData, outgoingEffectMessage.effectData);
		if (NetworkServer.active)
		{
			foreach (NetworkConnection connection in NetworkServer.connections)
			{
				if (connection != null && connection != netOrigin)
				{
					connection.SendByChannel(52, outgoingEffectMessage, qosChannel.intVal);
				}
			}
			return;
		}
		if (ClientScene.readyConnection != null)
		{
			ClientScene.readyConnection.SendByChannel(52, outgoingEffectMessage, qosChannel.intVal);
		}
	}

	private static void HandleEffectClientInternal(NetworkMessage netMsg)
	{
		netMsg.ReadMessage(incomingEffectMessage);
		SpawnEffect(incomingEffectMessage.effectIndex, incomingEffectMessage.effectData, transmit: false);
	}

	private static void HandleEffectServerInternal(NetworkMessage netMsg)
	{
		netMsg.ReadMessage(incomingEffectMessage);
		TransmitEffect(incomingEffectMessage.effectIndex, incomingEffectMessage.effectData, netMsg.conn);
	}

	public static void SimpleMuzzleFlash(GameObject effectPrefab, GameObject obj, string muzzleName, bool transmit)
	{
		if (!obj)
		{
			return;
		}
		ModelLocator component = obj.GetComponent<ModelLocator>();
		if (!component || !component.modelTransform)
		{
			return;
		}
		ChildLocator component2 = component.modelTransform.GetComponent<ChildLocator>();
		if ((bool)component2)
		{
			int childIndex = component2.FindChildIndex(muzzleName);
			Transform transform = component2.FindChild(childIndex);
			if ((bool)transform)
			{
				EffectData effectData = new EffectData
				{
					origin = transform.position
				};
				effectData.SetChildLocatorTransformReference(obj, childIndex);
				SpawnEffect(effectPrefab, effectData, transmit);
			}
		}
	}

	public static void SimpleImpactEffect(GameObject effectPrefab, Vector3 hitPos, Vector3 normal, bool transmit)
	{
		SpawnEffect(effectPrefab, new EffectData
		{
			origin = hitPos,
			rotation = ((normal == Vector3.zero) ? Quaternion.identity : Util.QuaternionSafeLookRotation(normal))
		}, transmit);
	}

	public static void SimpleImpactEffect(GameObject effectPrefab, Vector3 hitPos, Vector3 normal, Color color, bool transmit)
	{
		SpawnEffect(effectPrefab, new EffectData
		{
			origin = hitPos,
			rotation = Util.QuaternionSafeLookRotation(normal),
			color = color
		}, transmit);
	}

	public static void SimpleEffect(GameObject effectPrefab, Vector3 position, Quaternion rotation, bool transmit)
	{
		SpawnEffect(effectPrefab, new EffectData
		{
			origin = position,
			rotation = rotation
		}, transmit);
	}

	public static void SimpleSoundEffect(NetworkSoundEventIndex soundEventIndex, Vector3 position, bool transmit)
	{
		SpawnEffect(EffectIndex.Invalid, new EffectData
		{
			origin = position,
			networkSoundEventIndex = soundEventIndex
		}, transmit);
	}

	public static void ReturnToPoolOrDestroyInstance(this EffectManagerHelper effectManagerHelper, ref GameObject instance)
	{
		if (UsePools)
		{
			if (effectManagerHelper != null && effectManagerHelper.OwningPool != null)
			{
				effectManagerHelper.OwningPool.ReturnObject(effectManagerHelper);
			}
			else
			{
				UnityEngine.Object.Destroy(instance);
			}
			effectManagerHelper = null;
		}
		else
		{
			UnityEngine.Object.Destroy(instance);
		}
	}
}
