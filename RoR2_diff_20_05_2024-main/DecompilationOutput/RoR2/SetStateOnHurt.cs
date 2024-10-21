using EntityStates;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[DisallowMultipleComponent]
public class SetStateOnHurt : NetworkBehaviour, IOnTakeDamageServerReceiver
{
	[Tooltip("The percentage of their max HP they need to take to get stunned. Ranges from 0-1.")]
	public float hitThreshold = 0.1f;

	[Tooltip("The state machine to set the state of when this character is hurt.")]
	public EntityStateMachine targetStateMachine;

	[Tooltip("The state machine to set to idle when this character is hurt.")]
	public EntityStateMachine[] idleStateMachine;

	[Tooltip("The state to enter when this character is hurt.")]
	public SerializableEntityStateType hurtState;

	public bool canBeHitStunned = true;

	public bool canBeStunned = true;

	public bool canBeFrozen = true;

	private bool hasEffectiveAuthority = true;

	private static readonly float stunChanceOnHitBaseChancePercent;

	private static int kRpcRpcSetStun;

	private static int kRpcRpcSetFrozen;

	private static int kRpcRpcSetShock;

	private static int kRpcRpcSetPain;

	private static int kRpcRpcCleanse;

	private bool spawnedOverNetwork => base.isServer;

	public static void SetStunOnObject(GameObject target, float duration)
	{
		SetStateOnHurt component = target.GetComponent<SetStateOnHurt>();
		if ((bool)component)
		{
			component.SetStun(duration);
		}
	}

	public override void OnStartAuthority()
	{
		base.OnStartAuthority();
		UpdateAuthority();
	}

	public override void OnStopAuthority()
	{
		base.OnStopAuthority();
		UpdateAuthority();
	}

	private void UpdateAuthority()
	{
		hasEffectiveAuthority = Util.HasEffectiveAuthority(base.gameObject);
	}

	private void Start()
	{
		UpdateAuthority();
	}

	public void OnTakeDamageServer(DamageReport damageReport)
	{
		if (!targetStateMachine || !spawnedOverNetwork)
		{
			return;
		}
		HealthComponent victim = damageReport.victim;
		DamageInfo damageInfo = damageReport.damageInfo;
		CharacterMaster attackerMaster = damageReport.attackerMaster;
		int num = (attackerMaster ? attackerMaster.inventory.GetItemCount(RoR2Content.Items.StunChanceOnHit) : 0);
		if (num > 0 && Util.CheckRoll(Util.ConvertAmplificationPercentageIntoReductionPercentage(stunChanceOnHitBaseChancePercent * (float)num * damageReport.damageInfo.procCoefficient), attackerMaster))
		{
			EffectManager.SimpleImpactEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ImpactEffects/ImpactStunGrenade"), damageInfo.position, -damageInfo.force, transmit: true);
			SetStun(2f);
		}
		bool flag = damageInfo.procCoefficient >= Mathf.Epsilon;
		float damageDealt = damageReport.damageDealt;
		if (flag && canBeFrozen && (ulong)(damageInfo.damageType & DamageType.Freeze2s) != 0L)
		{
			SetFrozen(2f * damageInfo.procCoefficient);
		}
		else if (!victim.isInFrozenState)
		{
			if (flag && canBeStunned && (ulong)(damageInfo.damageType & DamageType.Shock5s) != 0L)
			{
				SetShock(5f * damageReport.damageInfo.procCoefficient);
			}
			else if (flag && canBeStunned && (ulong)(damageInfo.damageType & DamageType.Stun1s) != 0L)
			{
				SetStun(1f);
			}
			else if (canBeHitStunned && damageDealt > victim.fullCombinedHealth * hitThreshold)
			{
				SetPain();
			}
		}
	}

	[Server]
	public void SetStun(float duration)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.SetStateOnHurt::SetStun(System.Single)' called on client");
		}
		else if (canBeStunned)
		{
			if (hasEffectiveAuthority)
			{
				SetStunInternal(duration);
			}
			else
			{
				CallRpcSetStun(duration);
			}
		}
	}

	[Server]
	public void SetFrozen(float duration)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.SetStateOnHurt::SetFrozen(System.Single)' called on client");
		}
		else if (canBeFrozen)
		{
			if (hasEffectiveAuthority)
			{
				SetFrozenInternal(duration);
			}
			else
			{
				CallRpcSetFrozen(duration);
			}
		}
	}

	[Server]
	public void SetShock(float duration)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.SetStateOnHurt::SetShock(System.Single)' called on client");
		}
		else if (canBeStunned)
		{
			if (hasEffectiveAuthority)
			{
				SetShockInternal(duration);
			}
			else
			{
				CallRpcSetShock(duration);
			}
		}
	}

	[Server]
	public void SetPain()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.SetStateOnHurt::SetPain()' called on client");
		}
		else if (canBeHitStunned)
		{
			if (hasEffectiveAuthority)
			{
				SetPainInternal();
			}
			else
			{
				CallRpcSetPain();
			}
		}
	}

	[Server]
	public void Cleanse()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.SetStateOnHurt::Cleanse()' called on client");
		}
		else if (hasEffectiveAuthority)
		{
			CleanseInternal();
		}
		else
		{
			CallRpcCleanse();
		}
	}

	[ClientRpc]
	private void RpcSetStun(float duration)
	{
		if (hasEffectiveAuthority)
		{
			SetStunInternal(duration);
		}
	}

	private void SetStunInternal(float duration)
	{
		if ((bool)targetStateMachine)
		{
			if (targetStateMachine.state is StunState)
			{
				(targetStateMachine.state as StunState).ExtendStun(duration);
			}
			else
			{
				StunState stunState = new StunState();
				stunState.stunDuration = duration;
				targetStateMachine.SetInterruptState(stunState, InterruptPriority.Stun);
			}
		}
		EntityStateMachine[] array = idleStateMachine;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetNextStateToMain();
		}
	}

	[ClientRpc]
	private void RpcSetFrozen(float duration)
	{
		if (hasEffectiveAuthority)
		{
			SetFrozenInternal(duration);
		}
	}

	private void SetFrozenInternal(float duration)
	{
		if ((bool)targetStateMachine)
		{
			FrozenState frozenState = new FrozenState();
			frozenState.freezeDuration = duration;
			targetStateMachine.SetInterruptState(frozenState, InterruptPriority.Frozen);
		}
		EntityStateMachine[] array = idleStateMachine;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetNextState(new Idle());
		}
	}

	[ClientRpc]
	private void RpcSetShock(float duration)
	{
		if (hasEffectiveAuthority)
		{
			SetShockInternal(duration);
		}
	}

	private void SetShockInternal(float duration)
	{
		if ((bool)targetStateMachine)
		{
			ShockState shockState = new ShockState();
			shockState.shockDuration = duration;
			targetStateMachine.SetInterruptState(shockState, InterruptPriority.Pain);
		}
		EntityStateMachine[] array = idleStateMachine;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetNextStateToMain();
		}
	}

	[ClientRpc]
	private void RpcSetPain()
	{
		if (hasEffectiveAuthority)
		{
			SetPainInternal();
		}
	}

	private void SetPainInternal()
	{
		if ((bool)targetStateMachine)
		{
			targetStateMachine.SetInterruptState(EntityStateCatalog.InstantiateState(ref hurtState), InterruptPriority.Pain);
		}
		EntityStateMachine[] array = idleStateMachine;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetNextStateToMain();
		}
	}

	[ClientRpc]
	private void RpcCleanse()
	{
		if (hasEffectiveAuthority)
		{
			CleanseInternal();
		}
	}

	private void CleanseInternal()
	{
		if ((bool)targetStateMachine && (targetStateMachine.state is FrozenState || targetStateMachine.state is StunState || targetStateMachine.state is ShockState))
		{
			targetStateMachine.SetInterruptState(EntityStateCatalog.InstantiateState(ref targetStateMachine.mainStateType), InterruptPriority.Frozen);
		}
	}

	static SetStateOnHurt()
	{
		stunChanceOnHitBaseChancePercent = 5f;
		kRpcRpcSetStun = 788834249;
		NetworkBehaviour.RegisterRpcDelegate(typeof(SetStateOnHurt), kRpcRpcSetStun, InvokeRpcRpcSetStun);
		kRpcRpcSetFrozen = 1781279215;
		NetworkBehaviour.RegisterRpcDelegate(typeof(SetStateOnHurt), kRpcRpcSetFrozen, InvokeRpcRpcSetFrozen);
		kRpcRpcSetShock = -1316305549;
		NetworkBehaviour.RegisterRpcDelegate(typeof(SetStateOnHurt), kRpcRpcSetShock, InvokeRpcRpcSetShock);
		kRpcRpcSetPain = 788726245;
		NetworkBehaviour.RegisterRpcDelegate(typeof(SetStateOnHurt), kRpcRpcSetPain, InvokeRpcRpcSetPain);
		kRpcRpcCleanse = -339360280;
		NetworkBehaviour.RegisterRpcDelegate(typeof(SetStateOnHurt), kRpcRpcCleanse, InvokeRpcRpcCleanse);
		NetworkCRC.RegisterBehaviour("SetStateOnHurt", 0);
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeRpcRpcSetStun(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcSetStun called on server.");
		}
		else
		{
			((SetStateOnHurt)obj).RpcSetStun(reader.ReadSingle());
		}
	}

	protected static void InvokeRpcRpcSetFrozen(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcSetFrozen called on server.");
		}
		else
		{
			((SetStateOnHurt)obj).RpcSetFrozen(reader.ReadSingle());
		}
	}

	protected static void InvokeRpcRpcSetShock(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcSetShock called on server.");
		}
		else
		{
			((SetStateOnHurt)obj).RpcSetShock(reader.ReadSingle());
		}
	}

	protected static void InvokeRpcRpcSetPain(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcSetPain called on server.");
		}
		else
		{
			((SetStateOnHurt)obj).RpcSetPain();
		}
	}

	protected static void InvokeRpcRpcCleanse(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcCleanse called on server.");
		}
		else
		{
			((SetStateOnHurt)obj).RpcCleanse();
		}
	}

	public void CallRpcSetStun(float duration)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcSetStun called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcSetStun);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.Write(duration);
		SendRPCInternal(networkWriter, 0, "RpcSetStun");
	}

	public void CallRpcSetFrozen(float duration)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcSetFrozen called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcSetFrozen);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.Write(duration);
		SendRPCInternal(networkWriter, 0, "RpcSetFrozen");
	}

	public void CallRpcSetShock(float duration)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcSetShock called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcSetShock);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.Write(duration);
		SendRPCInternal(networkWriter, 0, "RpcSetShock");
	}

	public void CallRpcSetPain()
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcSetPain called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcSetPain);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		SendRPCInternal(networkWriter, 0, "RpcSetPain");
	}

	public void CallRpcCleanse()
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcCleanse called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcCleanse);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		SendRPCInternal(networkWriter, 0, "RpcCleanse");
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
