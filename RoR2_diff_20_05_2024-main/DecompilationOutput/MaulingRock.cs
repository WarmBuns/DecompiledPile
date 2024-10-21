using System.Runtime.InteropServices;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

public class MaulingRock : NetworkBehaviour
{
	private HealthComponent myHealthComponent;

	public GameObject deathEffect;

	public float blastRadius = 1f;

	public float damage = 10f;

	public float damageCoefficient = 1f;

	public float scaleVarianceLow = 0.8f;

	public float scaleVarianceHigh = 1.2f;

	public float verticalOffset;

	[SyncVar]
	private float scale;

	public float Networkscale
	{
		get
		{
			return scale;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref scale, 1u);
		}
	}

	public override void OnStartServer()
	{
		base.OnStartServer();
		Networkscale = Random.Range(scaleVarianceLow, scaleVarianceHigh);
	}

	private void Start()
	{
		myHealthComponent = GetComponent<HealthComponent>();
		base.transform.localScale = new Vector3(scale, scale, scale);
	}

	private void FixedUpdate()
	{
		if (NetworkServer.active && myHealthComponent != null && !myHealthComponent.alive)
		{
			if (deathEffect != null)
			{
				EffectManager.SpawnEffect(deathEffect, new EffectData
				{
					origin = base.transform.position,
					scale = blastRadius
				}, transmit: true);
			}
			Object.Destroy(base.gameObject);
		}
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.Write(scale);
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
			writer.Write(scale);
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
			scale = reader.ReadSingle();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			scale = reader.ReadSingle();
		}
	}

	public override void PreStartClient()
	{
	}
}
