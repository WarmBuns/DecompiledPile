using System;
using System.Collections.Generic;
using RoR2.ConVar;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

namespace RoR2.Audio;

public static class PointSoundManager
{
	private class NetworkSoundEventMessage : MessageBase
	{
		public NetworkSoundEventIndex soundEventIndex;

		public Vector3 position;

		public override void Serialize(NetworkWriter writer)
		{
			base.Serialize(writer);
			writer.WriteNetworkSoundEventIndex(soundEventIndex);
			writer.Write(position);
		}

		public override void Deserialize(NetworkReader reader)
		{
			base.Deserialize(reader);
			soundEventIndex = reader.ReadNetworkSoundEventIndex();
			position = reader.ReadVector3();
		}
	}

	private struct TimeoutInfo
	{
		public readonly AkGameObj emitter;

		public readonly float startTime;

		public TimeoutInfo(AkGameObj emitter, float startTime)
		{
			this.emitter = emitter;
			this.startTime = startTime;
		}
	}

	private static readonly Stack<AkGameObj> emitterPool = new Stack<AkGameObj>();

	private static GameObject emitterObjectPrefab;

	private const uint EMPTY_AK_EVENT_SOUND = 2166136261u;

	private static readonly NetworkSoundEventMessage sharedMessage = new NetworkSoundEventMessage();

	private static readonly QosChannelIndex channel = QosChannelIndex.effects;

	private const short messageType = 72;

	private static readonly FloatConVar cvPointSoundManagerTimeout = new FloatConVar("pointsoundmanager_timeout", ConVarFlags.None, "4.5", "Timeout value in seconds to use for sound emitters dispatched by PointSoundManager. -1 for end-of-playback callbacks instead, which we suspect may have thread-safety issues.");

	private static readonly Queue<TimeoutInfo> timeouts = new Queue<TimeoutInfo>();

	[InitDuringStartup]
	private static void InitSoundEmitter()
	{
		RoR2Application.onLoad = (Action)Delegate.Combine(RoR2Application.onLoad, (Action)delegate
		{
			LoadEmitterPrefab();
		});
	}

	private static void LoadEmitterPrefab()
	{
		AsyncOperationHandle<GameObject> asyncOperationHandle = Addressables.LoadAssetAsync<GameObject>("29b2b6ef68391f74e91693f36016b301");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> obj)
		{
			SetEmitterPrefab(obj.Result);
		};
	}

	private static void SetEmitterPrefab(GameObject emitterPrefab)
	{
		emitterObjectPrefab = emitterPrefab;
	}

	private static AkGameObj RequestEmitter()
	{
		if (emitterPool.Count > 0)
		{
			return emitterPool.Pop();
		}
		if ((bool)emitterObjectPrefab)
		{
			return UnityEngine.Object.Instantiate(emitterObjectPrefab).GetComponent<AkGameObj>();
		}
		LoadEmitterPrefab();
		GameObject gameObject = new GameObject("SoundEmitter");
		gameObject.AddComponent<Rigidbody>().isKinematic = true;
		return gameObject.AddComponent<AkGameObj>();
	}

	private static void FreeEmitter(AkGameObj emitter)
	{
		if ((bool)emitter)
		{
			emitterPool.Push(emitter);
		}
	}

	public static uint EmitSoundLocal(AkEventIdArg akEventId, Vector3 position)
	{
		if (WwiseIntegrationManager.noAudio || akEventId.id == 0 || akEventId.id == 2166136261u)
		{
			return 0u;
		}
		AkGameObj akGameObj = RequestEmitter();
		akGameObj.transform.position = position;
		akGameObj.SetPosition();
		uint result;
		if (cvPointSoundManagerTimeout.value < 0f)
		{
			result = AkSoundEngine.PostEvent(akEventId, akGameObj.gameObject, 1u, Callback, akGameObj);
		}
		else
		{
			result = AkSoundEngine.PostEvent(akEventId, akGameObj.gameObject);
			timeouts.Enqueue(new TimeoutInfo(akGameObj, Time.unscaledTime));
		}
		return result;
	}

	private static void Callback(object cookie, AkCallbackType in_type, AkCallbackInfo in_info)
	{
		if (in_type == AkCallbackType.AK_EndOfEvent)
		{
			FreeEmitter((AkGameObj)cookie);
		}
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		SceneManager.sceneUnloaded += OnSceneUnloaded;
		RoR2Application.onUpdate += StaticUpdate;
	}

	private static void StaticUpdate()
	{
		ProcessTimeoutQueue();
	}

	private static void OnSceneUnloaded(Scene scene)
	{
		ClearEmitterPool();
	}

	private static void ClearEmitterPool()
	{
		foreach (AkGameObj item in emitterPool)
		{
			if ((bool)item)
			{
				UnityEngine.Object.Destroy(item.gameObject);
			}
		}
		emitterPool.Clear();
	}

	public static void EmitSoundServer(NetworkSoundEventIndex networkSoundEventIndex, Vector3 position)
	{
		sharedMessage.soundEventIndex = networkSoundEventIndex;
		sharedMessage.position = position;
		NetworkServer.SendByChannelToAll(72, sharedMessage, channel.intVal);
	}

	[NetworkMessageHandler(client = true, server = false, msgType = 72)]
	private static void HandleMessage(NetworkMessage netMsg)
	{
		netMsg.ReadMessage(sharedMessage);
		EmitSoundLocal(NetworkSoundEventCatalog.GetAkIdFromNetworkSoundEventIndex(sharedMessage.soundEventIndex), sharedMessage.position);
	}

	private static void ProcessTimeoutQueue()
	{
		float num = Time.unscaledTime - cvPointSoundManagerTimeout.value;
		while (timeouts.Count > 0)
		{
			TimeoutInfo timeoutInfo = timeouts.Peek();
			if (!(num <= timeoutInfo.startTime))
			{
				timeouts.Dequeue();
				FreeEmitter(timeoutInfo.emitter);
				continue;
			}
			break;
		}
	}
}
