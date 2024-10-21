using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(CharacterBody))]
public class LoaderStaticChargeComponent : NetworkBehaviour, IOnDamageDealtServerReceiver, IOnTakeDamageServerReceiver
{
	private enum State
	{
		Idle,
		Drain
	}

	public float maxCharge = 100f;

	public float consumptionRate = 10f;

	[SyncVar]
	private float _charge;

	private CharacterBody characterBody;

	private State state;

	private static int kCmdCmdConsumeCharge;

	public float charge => _charge;

	public float chargeFraction => charge / maxCharge;

	public float Network_charge
	{
		get
		{
			return _charge;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref _charge, 1u);
		}
	}

	private void Awake()
	{
		characterBody = GetComponent<CharacterBody>();
	}

	public void OnDamageDealtServer(DamageReport damageReport)
	{
		AddChargeServer(damageReport.damageDealt);
	}

	public void OnTakeDamageServer(DamageReport damageReport)
	{
		AddChargeServer(damageReport.damageDealt);
	}

	[Server]
	private void AddChargeServer(float additionalCharge)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.LoaderStaticChargeComponent::AddChargeServer(System.Single)' called on client");
			return;
		}
		float num = _charge + additionalCharge;
		if (num > maxCharge)
		{
			num = maxCharge;
		}
		Network_charge = num;
	}

	[Server]
	private void ConsumeChargeInternal()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.LoaderStaticChargeComponent::ConsumeChargeInternal()' called on client");
		}
		else
		{
			SetState(State.Drain);
		}
	}

	public void ConsumeChargeAuthority()
	{
		if (NetworkServer.active)
		{
			ConsumeChargeInternal();
		}
		else
		{
			CallCmdConsumeCharge();
		}
	}

	private void SetState(State newState)
	{
		if (state != newState)
		{
			if (state == State.Drain && NetworkServer.active)
			{
				characterBody.RemoveBuff(JunkContent.Buffs.LoaderOvercharged);
			}
			state = newState;
			if (state == State.Drain && NetworkServer.active)
			{
				characterBody.AddBuff(JunkContent.Buffs.LoaderOvercharged);
			}
		}
	}

	private void FixedUpdate()
	{
		if (NetworkServer.active && state == State.Drain)
		{
			Network_charge = _charge - Time.fixedDeltaTime * consumptionRate;
			if (_charge <= 0f)
			{
				Network_charge = 0f;
				SetState(State.Idle);
			}
		}
	}

	[Command]
	private void CmdConsumeCharge()
	{
		ConsumeChargeInternal();
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeCmdCmdConsumeCharge(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdConsumeCharge called on client.");
		}
		else
		{
			((LoaderStaticChargeComponent)obj).CmdConsumeCharge();
		}
	}

	public void CallCmdConsumeCharge()
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdConsumeCharge called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdConsumeCharge();
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdConsumeCharge);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		SendCommandInternal(networkWriter, 0, "CmdConsumeCharge");
	}

	static LoaderStaticChargeComponent()
	{
		kCmdCmdConsumeCharge = -261598328;
		NetworkBehaviour.RegisterCommandDelegate(typeof(LoaderStaticChargeComponent), kCmdCmdConsumeCharge, InvokeCmdCmdConsumeCharge);
		NetworkCRC.RegisterBehaviour("LoaderStaticChargeComponent", 0);
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.Write(_charge);
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
			writer.Write(_charge);
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
			_charge = reader.ReadSingle();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			_charge = reader.ReadSingle();
		}
	}

	public override void PreStartClient()
	{
	}
}
