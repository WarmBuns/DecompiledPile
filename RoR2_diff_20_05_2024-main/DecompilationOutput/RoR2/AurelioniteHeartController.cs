using System.Runtime.InteropServices;
using EntityStates.AurelioniteHeart;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public sealed class AurelioniteHeartController : NetworkBehaviour, IInteractable, IDisplayNameProvider
{
	public string displayNameToken = "AURELIONITE_HEART_NAME";

	public string contextToken;

	public uint expReward;

	[SerializeField]
	private EntityStateMachine entityStateMachine;

	public Animator heartAnimator;

	[HideInInspector]
	[SyncVar]
	public bool activated;

	public GameObject rebirthShrine;

	public bool shouldProximityHighlight = true;

	public bool Networkactivated
	{
		get
		{
			return activated;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref activated, 1u);
		}
	}

	private void Start()
	{
		if ((bool)Run.instance)
		{
			expReward = (uint)((float)expReward * Run.instance.difficultyCoefficient);
		}
	}

	public void OnInteractionBegin(Interactor activator)
	{
		if (!activated)
		{
			Networkactivated = true;
			if ((bool)entityStateMachine)
			{
				entityStateMachine.SetNextState(new AurelioniteHeartActivationState());
			}
			ExperienceManager.instance.AwardExperience(base.transform.position, activator.GetComponent<CharacterBody>(), expReward);
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
		if (activated)
		{
			return Interactability.Disabled;
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
		return !activated;
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
			writer.Write(activated);
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
			writer.Write(activated);
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
			activated = reader.ReadBoolean();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			activated = reader.ReadBoolean();
		}
	}

	public override void PreStartClient()
	{
	}
}
