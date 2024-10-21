using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class DamageDealtMessage : MessageBase
{
	public GameObject victim;

	public float damage;

	public GameObject attacker;

	public Vector3 position;

	public bool crit;

	public DamageTypeCombo damageType;

	public DamageColorIndex damageColorIndex;

	public bool hitLowHealth;

	public bool isSilent => (ulong)(damageType & DamageType.Silent) != 0;

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.Write(victim);
		writer.Write(damage);
		writer.Write(attacker);
		writer.Write(position);
		writer.Write(crit);
		writer.WriteDamageType(damageType);
		writer.Write(damageColorIndex);
		writer.Write(hitLowHealth);
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		victim = reader.ReadGameObject();
		damage = reader.ReadSingle();
		attacker = reader.ReadGameObject();
		position = reader.ReadVector3();
		crit = reader.ReadBoolean();
		damageType = reader.ReadDamageType();
		damageColorIndex = reader.ReadDamageColorIndex();
		hitLowHealth = reader.ReadBoolean();
	}
}
