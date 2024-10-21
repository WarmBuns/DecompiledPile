using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace RoR2.Projectile;

[RequireComponent(typeof(ProjectileController))]
[RequireComponent(typeof(ProjectileOverlapAttack))]
public class ProjectileOverlapLimitHits : NetworkBehaviour
{
	[SerializeField]
	[Tooltip("How many hits this projectile can make before it will destroy itself.")]
	public int hitLimit;

	[SerializeField]
	[Tooltip("Announce in the console when the hit limit has been reached")]
	private bool debugMessages;

	private ProjectileOverlapAttack projectileOverlapAttack;

	[SerializeField]
	[Tooltip("Run when the projectile has one hit remaining.")]
	public UnityEvent OnOneHitRemaining;

	private int hitCount;

	private static int kCmdCmdLastHit;

	private static int kRpcRpcLastHit;

	private void Awake()
	{
		projectileOverlapAttack = GetComponent<ProjectileOverlapAttack>();
		_ = projectileOverlapAttack == null;
	}

	private void OnEnable()
	{
		projectileOverlapAttack.onServerHit.AddListener(CountOverlapHits);
	}

	private void CountOverlapHits()
	{
		hitCount++;
		if (hitCount == hitLimit - 1)
		{
			HandleNetworkedLastHit();
		}
		if (hitCount >= hitLimit)
		{
			DestroyProjectile();
		}
	}

	private void HandleNetworkedLastHit()
	{
		if (NetworkServer.active)
		{
			CallRpcLastHit();
		}
		else
		{
			CallCmdLastHit();
		}
	}

	[Command]
	private void CmdLastHit()
	{
		CallRpcLastHit();
	}

	[ClientRpc]
	private void RpcLastHit()
	{
		OnOneHitRemaining?.Invoke();
	}

	public bool IsLastHit()
	{
		return hitCount == hitLimit - 1;
	}

	private void DestroyProjectile()
	{
		if (NetworkServer.active)
		{
			_ = debugMessages;
			EffectManagerHelper component = GetComponent<EffectManagerHelper>();
			if ((bool)component && component.OwningPool != null)
			{
				component.OwningPool.ReturnObject(component);
			}
			else
			{
				Object.Destroy(base.gameObject);
			}
		}
	}

	private void OnDisable()
	{
		if (NetworkServer.active)
		{
			projectileOverlapAttack.onServerHit.RemoveListener(CountOverlapHits);
		}
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeCmdCmdLastHit(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdLastHit called on client.");
		}
		else
		{
			((ProjectileOverlapLimitHits)obj).CmdLastHit();
		}
	}

	public void CallCmdLastHit()
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdLastHit called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdLastHit();
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdLastHit);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		SendCommandInternal(networkWriter, 0, "CmdLastHit");
	}

	protected static void InvokeRpcRpcLastHit(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcLastHit called on server.");
		}
		else
		{
			((ProjectileOverlapLimitHits)obj).RpcLastHit();
		}
	}

	public void CallRpcLastHit()
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcLastHit called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcLastHit);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		SendRPCInternal(networkWriter, 0, "RpcLastHit");
	}

	static ProjectileOverlapLimitHits()
	{
		kCmdCmdLastHit = 809020640;
		NetworkBehaviour.RegisterCommandDelegate(typeof(ProjectileOverlapLimitHits), kCmdCmdLastHit, InvokeCmdCmdLastHit);
		kRpcRpcLastHit = -716657590;
		NetworkBehaviour.RegisterRpcDelegate(typeof(ProjectileOverlapLimitHits), kRpcRpcLastHit, InvokeRpcRpcLastHit);
		NetworkCRC.RegisterBehaviour("ProjectileOverlapLimitHits", 0);
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
