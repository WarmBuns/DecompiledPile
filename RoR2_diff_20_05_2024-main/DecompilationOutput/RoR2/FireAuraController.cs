using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class FireAuraController : NetworkBehaviour
{
	private const float fireAttackRadiusMin = 0.5f;

	private const float fireAttackRadiusMax = 6f;

	private const float fireDamageCoefficient = 1f;

	private const float fireProcCoefficient = 0.1f;

	private const float maxTimer = 8f;

	private float timer;

	private float attackStopwatch;

	[SyncVar]
	public GameObject owner;

	private NetworkInstanceId ___ownerNetId;

	public GameObject Networkowner
	{
		get
		{
			return owner;
		}
		[param: In]
		set
		{
			SetSyncVarGameObject(value, ref owner, 1u, ref ___ownerNetId);
		}
	}

	private void FixedUpdate()
	{
		timer += Time.fixedDeltaTime;
		CharacterBody characterBody = null;
		float num = 0f;
		if ((bool)owner)
		{
			characterBody = owner.GetComponent<CharacterBody>();
			num = (characterBody ? Mathf.Lerp(characterBody.radius * 0.5f, characterBody.radius * 6f, 1f - Mathf.Abs(-1f + 2f * timer / 8f)) : 0f);
			base.transform.position = owner.transform.position;
			base.transform.localScale = new Vector3(num, num, num);
		}
		if (!NetworkServer.active)
		{
			return;
		}
		if (!owner)
		{
			Object.Destroy(base.gameObject);
			return;
		}
		attackStopwatch += Time.fixedDeltaTime;
		if ((bool)characterBody && attackStopwatch >= 0.25f)
		{
			attackStopwatch = 0f;
			BlastAttack obj = new BlastAttack
			{
				attacker = owner,
				inflictor = base.gameObject
			};
			obj.teamIndex = TeamComponent.GetObjectTeam(obj.attacker);
			obj.position = base.transform.position;
			obj.procCoefficient = 0.1f;
			obj.radius = num;
			obj.baseForce = 0f;
			obj.baseDamage = 1f * characterBody.damage;
			obj.bonusForce = Vector3.zero;
			obj.crit = false;
			obj.damageType = DamageType.Generic;
			obj.attackerFiltering = AttackerFiltering.NeverHitSelf;
			obj.Fire();
		}
		if (timer >= 8f)
		{
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
			writer.Write(owner);
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
			writer.Write(owner);
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
			___ownerNetId = reader.ReadNetworkId();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			owner = reader.ReadGameObject();
		}
	}

	public override void PreStartClient()
	{
		if (!___ownerNetId.IsEmpty())
		{
			Networkowner = ClientScene.FindLocalObject(___ownerNetId);
		}
	}
}
