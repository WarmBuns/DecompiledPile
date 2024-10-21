using System;
using System.Security.Cryptography;
using System.Text;
using EntityStates;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace RoR2;

public class PortalDialerController : NetworkBehaviour
{
	[Serializable]
	public struct DialedAction
	{
		public Sha256HashAsset hashAsset;

		public UnityEvent action;
	}

	private class PortalDialerBaseState : BaseState
	{
		protected PortalDialerController portalDialer { get; private set; }

		public override void OnEnter()
		{
			base.OnEnter();
			portalDialer = GetComponent<PortalDialerController>();
		}
	}

	private class PortalDialerIdleState : PortalDialerBaseState
	{
		private PurchaseInteraction dialerInteraction;

		public override void OnEnter()
		{
			base.OnEnter();
			PortalDialerButtonController[] buttons = base.portalDialer.buttons;
			for (int i = 0; i < buttons.Length; i++)
			{
				buttons[i].GetComponent<GenericInteraction>().Networkinteractability = Interactability.Available;
			}
			dialerInteraction = base.portalDialer.GetComponent<PurchaseInteraction>();
			if (NetworkServer.active)
			{
				dialerInteraction.SetAvailable(newAvailable: true);
				dialerInteraction.onPurchase.AddListener(OnActivationServer);
				if (Run.instance.GetEventFlag("NoArtifactWorld"))
				{
					outer.SetNextState(new PortalDialerDisabledState());
				}
			}
		}

		private void OnActivationServer(Interactor interactor)
		{
			outer.SetNextState(new PortalDialerPreDialState());
		}

		public override void OnExit()
		{
			if (NetworkServer.active)
			{
				dialerInteraction.SetAvailable(newAvailable: false);
				dialerInteraction.onPurchase.RemoveListener(OnActivationServer);
			}
			PortalDialerButtonController[] buttons = base.portalDialer.buttons;
			foreach (PortalDialerButtonController portalDialerButtonController in buttons)
			{
				if ((bool)portalDialerButtonController)
				{
					portalDialerButtonController.GetComponent<GenericInteraction>().Networkinteractability = Interactability.Disabled;
				}
			}
			base.OnExit();
		}
	}

	private class PortalDialerDisabledState : PortalDialerBaseState
	{
		private PurchaseInteraction dialerInteraction;

		public override void OnEnter()
		{
			base.OnEnter();
			PortalDialerButtonController[] buttons = base.portalDialer.buttons;
			for (int i = 0; i < buttons.Length; i++)
			{
				buttons[i].GetComponent<GenericInteraction>().Networkinteractability = Interactability.Disabled;
			}
			dialerInteraction = base.portalDialer.GetComponent<PurchaseInteraction>();
			if (NetworkServer.active)
			{
				dialerInteraction.SetAvailable(newAvailable: false);
			}
		}
	}

	private class PortalDialerPreDialState : PortalDialerBaseState
	{
		public static float baseDuration;

		public static GameObject portalPrespawnEffect;

		private float duration;

		private byte[] sequenceServer;

		public override void OnEnter()
		{
			base.OnEnter();
			duration = baseDuration;
			if (NetworkServer.active)
			{
				sequenceServer = new byte[base.portalDialer.buttons.Length];
			}
			if ((bool)portalPrespawnEffect)
			{
				EffectManager.SimpleEffect(portalPrespawnEffect, base.portalDialer.portalSpawnLocation.position, base.portalDialer.portalSpawnLocation.rotation, transmit: false);
			}
			Util.PlaySound("Play_env_hiddenLab_laptop_activate", base.gameObject);
		}

		public override void FixedUpdate()
		{
			base.FixedUpdate();
			if (NetworkServer.active && base.fixedAge >= duration)
			{
				outer.SetNextState(new PortalDialerDialDigitState
				{
					currentDigit = 0,
					sequenceServer = sequenceServer
				});
			}
		}
	}

	private class PortalDialerDialDigitState : PortalDialerBaseState
	{
		public static float baseDuration;

		public int currentDigit;

		private float duration;

		public byte[] sequenceServer;

		public override void OnEnter()
		{
			base.OnEnter();
			duration = baseDuration;
			PortalDialerButtonController portalDialerButtonController = base.portalDialer.dialingOrder[currentDigit];
			if (NetworkServer.active)
			{
				sequenceServer[Array.IndexOf(base.portalDialer.buttons, portalDialerButtonController)] = (byte)portalDialerButtonController.currentDigitDef.value;
				if ((bool)portalDialerButtonController)
				{
					portalDialerButtonController.NetworkcurrentDigitIndex = 0;
				}
			}
			Util.PlaySound("Play_env_hiddenLab_laptop_sequence_lock", base.gameObject);
		}

		public override void OnExit()
		{
			base.OnExit();
		}

		public override void FixedUpdate()
		{
			base.FixedUpdate();
			if (NetworkServer.active && base.fixedAge >= duration)
			{
				if (currentDigit >= base.portalDialer.buttons.Length - 1)
				{
					Debug.LogFormat("Submitting sequence {0}", SequenceToString(sequenceServer));
					outer.SetNextState(new PortalDialerPostDialState
					{
						sequenceServer = sequenceServer
					});
				}
				else
				{
					outer.SetNextState(new PortalDialerDialDigitState
					{
						currentDigit = currentDigit + 1,
						sequenceServer = sequenceServer
					});
				}
			}
		}

		public override void OnSerialize(NetworkWriter writer)
		{
			base.OnSerialize(writer);
			writer.Write((byte)currentDigit);
		}

		public override void OnDeserialize(NetworkReader reader)
		{
			base.OnDeserialize(reader);
			currentDigit = reader.ReadByte();
		}
	}

	private class PortalDialerPostDialState : PortalDialerBaseState
	{
		public static float baseDuration;

		public static GameObject portalSpawnEffect;

		private float duration;

		public byte[] sequenceServer;

		public override void OnEnter()
		{
			base.OnEnter();
			duration = baseDuration;
			if ((bool)portalSpawnEffect)
			{
				EffectManager.SimpleEffect(portalSpawnEffect, base.portalDialer.portalSpawnLocation.position, base.portalDialer.portalSpawnLocation.rotation, transmit: false);
			}
		}

		public override void OnExit()
		{
			base.OnExit();
		}

		public override void FixedUpdate()
		{
			base.FixedUpdate();
			if (NetworkServer.active && base.fixedAge >= duration)
			{
				outer.SetNextState(new PortalDialerPerformActionState
				{
					sequenceServer = sequenceServer
				});
			}
		}
	}

	private class PortalDialerPerformActionState : PortalDialerBaseState
	{
		public byte[] sequenceServer;

		public static NetworkSoundEventDef nseSuccess;

		public static NetworkSoundEventDef nseFail;

		public override void OnEnter()
		{
			base.OnEnter();
			if (NetworkServer.active)
			{
				if (base.portalDialer.PerformActionServer(sequenceServer))
				{
					EffectManager.SimpleSoundEffect(nseSuccess.index, base.portalDialer.portalSpawnLocation.position, transmit: true);
					return;
				}
				EffectManager.SimpleSoundEffect(nseFail.index, base.portalDialer.portalSpawnLocation.position, transmit: true);
				outer.SetNextState(new PortalDialerIdleState());
			}
		}
	}

	public PortalDialerButtonController[] buttons;

	public PortalDialerButtonController[] dialingOrder;

	public DialedAction[] actions;

	public Transform portalSpawnLocation;

	private SHA256 hasher;

	private byte[] dialedNumber;

	private void Awake()
	{
		if (NetworkServer.active)
		{
			hasher = SHA256.Create();
		}
	}

	private void OnDestroy()
	{
		if (hasher != null)
		{
			hasher.Dispose();
			hasher = null;
		}
	}

	private byte[] GetSequence()
	{
		byte[] array = new byte[buttons.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = (byte)buttons[i].currentDigitDef.value;
		}
		return array;
	}

	private Sha256Hash GetResult(byte[] sequence)
	{
		hasher.Initialize();
		return Sha256Hash.FromBytes(hasher.ComputeHash(sequence));
	}

	[Server]
	public bool PerformActionServer(byte[] sequence)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Boolean RoR2.PortalDialerController::PerformActionServer(System.Byte[])' called on client");
			return false;
		}
		Sha256Hash result = GetResult(sequence);
		Debug.LogFormat("Performing action for sequence={0} hash={1}", sequence, result.ToString());
		for (int i = 0; i < actions.Length; i++)
		{
			if (result.Equals(actions[i].hashAsset.value))
			{
				actions[i].action?.Invoke();
				return true;
			}
		}
		Debug.LogFormat("Could not find action to perform for {0}", result.ToString());
		return false;
	}

	private static string SequenceToString(byte[] bytes)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(bytes[0]);
		for (int i = 1; i < bytes.Length; i++)
		{
			stringBuilder.Append(":");
			stringBuilder.Append(bytes[i]);
		}
		return stringBuilder.ToString();
	}

	public void PrintResult()
	{
	}

	[Server]
	public void OpenArtifactPortalServer(ArtifactDef artifactDef)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.PortalDialerController::OpenArtifactPortalServer(RoR2.ArtifactDef)' called on client");
			return;
		}
		ArtifactTrialMissionController.trialArtifact = artifactDef;
		SpawnPortalServer(SceneCatalog.GetSceneDefFromSceneName(UnityEngine.Random.Range(0, 4) switch
		{
			1 => "artifactworld01", 
			2 => "artifactworld02", 
			3 => "artifactworld03", 
			_ => "artifactworld", 
		}));
		GetComponent<PurchaseInteraction>().SetAvailable(newAvailable: false);
	}

	[Server]
	public void SpawnPortalServer(SceneDef destination)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.PortalDialerController::SpawnPortalServer(RoR2.SceneDef)' called on client");
			return;
		}
		GameObject original = LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/PortalArtifactworld");
		if ((bool)destination.preferredPortalPrefab)
		{
			original = destination.preferredPortalPrefab;
		}
		GameObject obj = UnityEngine.Object.Instantiate(original, portalSpawnLocation.position, portalSpawnLocation.rotation);
		SceneExitController component = obj.GetComponent<SceneExitController>();
		component.useRunNextStageScene = false;
		component.destinationScene = destination;
		NetworkServer.Spawn(obj);
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		bool result = default(bool);
		return result;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
	}

	public override void PreStartClient()
	{
	}
}
