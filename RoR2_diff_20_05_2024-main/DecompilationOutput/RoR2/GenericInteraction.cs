using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace RoR2;

public sealed class GenericInteraction : NetworkBehaviour, IInteractable
{
	[Serializable]
	public class InteractorUnityEvent : UnityEvent<Interactor>
	{
	}

	[SyncVar]
	public Interactability interactability = Interactability.Available;

	public bool shouldIgnoreSpherecastForInteractibility;

	public string contextToken;

	public bool shouldProximityHighlight = true;

	public InteractorUnityEvent onActivation;

	public bool shouldShowOnScanner = true;

	public Interactability Networkinteractability
	{
		get
		{
			return interactability;
		}
		[param: In]
		set
		{
			ulong newValueAsUlong = (ulong)value;
			ulong fieldValueAsUlong = (ulong)interactability;
			SetSyncVarEnum(value, newValueAsUlong, ref interactability, fieldValueAsUlong, 1u);
		}
	}

	[Server]
	public void SetInteractabilityAvailable()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.GenericInteraction::SetInteractabilityAvailable()' called on client");
		}
		else
		{
			Networkinteractability = Interactability.Available;
		}
	}

	[Server]
	public void SetInteractabilityConditionsNotMet()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.GenericInteraction::SetInteractabilityConditionsNotMet()' called on client");
		}
		else
		{
			Networkinteractability = Interactability.ConditionsNotMet;
		}
	}

	[Server]
	public void SetInteractabilityDisabled()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.GenericInteraction::SetInteractabilityDisabled()' called on client");
		}
		else
		{
			Networkinteractability = Interactability.Disabled;
		}
	}

	string IInteractable.GetContextString(Interactor activator)
	{
		if (contextToken == "")
		{
			return null;
		}
		return Language.GetString(contextToken);
	}

	public bool ShouldIgnoreSpherecastForInteractibility(Interactor activator)
	{
		return shouldIgnoreSpherecastForInteractibility;
	}

	Interactability IInteractable.GetInteractability(Interactor activator)
	{
		return interactability;
	}

	void IInteractable.OnInteractionBegin(Interactor activator)
	{
		onActivation.Invoke(activator);
	}

	private void OnEnable()
	{
		InstanceTracker.Add(this);
	}

	private void OnDisable()
	{
		InstanceTracker.Remove(this);
	}

	public bool ShouldShowOnScanner()
	{
		if (shouldShowOnScanner)
		{
			return interactability != Interactability.Disabled;
		}
		return false;
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
			writer.Write((int)interactability);
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
			writer.Write((int)interactability);
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
			interactability = (Interactability)reader.ReadInt32();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			interactability = (Interactability)reader.ReadInt32();
		}
	}

	public override void PreStartClient()
	{
	}
}
