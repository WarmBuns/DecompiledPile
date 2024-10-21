using System.Runtime.InteropServices;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class GeodeController : NetworkBehaviour, IInteractable, IDisplayNameProvider
{
	[SyncVar]
	public string displayNameToken = "GEODE_NAME";

	[SyncVar]
	public string contextToken = "GEODE_CONTEXT";

	public bool ShouldDropReward = true;

	public bool ShouldRegenerate;

	[SerializeField]
	private EntityStateMachine stateMachine;

	[SyncVar]
	private bool available = true;

	public Interactor lastInteractor;

	[SyncVar]
	public float geodeBuffRadius = 12f;

	public float barrierToHealthPercentage;

	[SerializeField]
	private bool isSecretMissionGeode;

	[SerializeField]
	private GeodeSecretMissionController geodeSecretMissionController;

	public bool shouldProximityHighlight = true;

	public float idleVFXRegenerationTimer;

	public float printTime;

	public float startingPrintHeight;

	public float maxPrintHeight;

	public AnimationCurve printCurve;

	public string NetworkdisplayNameToken
	{
		get
		{
			return displayNameToken;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref displayNameToken, 1u);
		}
	}

	public string NetworkcontextToken
	{
		get
		{
			return contextToken;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref contextToken, 2u);
		}
	}

	public bool Networkavailable
	{
		get
		{
			return available;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref available, 4u);
		}
	}

	public float NetworkgeodeBuffRadius
	{
		get
		{
			return geodeBuffRadius;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref geodeBuffRadius, 8u);
		}
	}

	public void OnInteractionBegin(Interactor activator)
	{
		SetAvailable(activeState: false);
		lastInteractor = activator;
		if (geodeSecretMissionController != null && isSecretMissionGeode)
		{
			geodeSecretMissionController.AdvanceGeodeSecretMission();
		}
		stateMachine.SetNextStateToMain();
	}

	[Server]
	public void SetAvailable(bool activeState)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.GeodeController::SetAvailable(System.Boolean)' called on client");
		}
		else
		{
			Networkavailable = activeState;
		}
	}

	public string GetContextString(Interactor activator)
	{
		return Language.GetString(contextToken);
	}

	public override int GetNetworkChannel()
	{
		return QosChannelIndex.defaultReliable.intVal;
	}

	public Interactability GetInteractability(Interactor activator)
	{
		if (!available)
		{
			return Interactability.Disabled;
		}
		if (!available)
		{
			return Interactability.ConditionsNotMet;
		}
		return Interactability.Available;
	}

	public string GetDisplayName()
	{
		return Language.GetString(displayNameToken);
	}

	public bool ShouldIgnoreSpherecastForInteractibility(Interactor activator)
	{
		return false;
	}

	public void OnEnable()
	{
		InstanceTracker.Add(this);
	}

	public void OnDisable()
	{
		InstanceTracker.Remove(this);
	}

	public bool ShouldShowOnScanner()
	{
		return !available;
	}

	public bool ShouldProximityHighlight()
	{
		return shouldProximityHighlight;
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.Write(displayNameToken);
			writer.Write(contextToken);
			writer.Write(available);
			writer.Write(geodeBuffRadius);
			return true;
		}
		bool flag = false;
		if ((base.syncVarDirtyBits & (true ? 1u : 0u)) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(displayNameToken);
		}
		if ((base.syncVarDirtyBits & 2u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(contextToken);
		}
		if ((base.syncVarDirtyBits & 4u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(available);
		}
		if ((base.syncVarDirtyBits & 8u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(geodeBuffRadius);
		}
		if (!flag)
		{
			writer.WritePackedUInt32(base.syncVarDirtyBits);
		}
		return flag;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
		if (initialState)
		{
			displayNameToken = reader.ReadString();
			contextToken = reader.ReadString();
			available = reader.ReadBoolean();
			geodeBuffRadius = reader.ReadSingle();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			displayNameToken = reader.ReadString();
		}
		if (((uint)num & 2u) != 0)
		{
			contextToken = reader.ReadString();
		}
		if (((uint)num & 4u) != 0)
		{
			available = reader.ReadBoolean();
		}
		if (((uint)num & 8u) != 0)
		{
			geodeBuffRadius = reader.ReadSingle();
		}
	}

	public override void PreStartClient()
	{
	}
}
