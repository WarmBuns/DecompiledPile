using System;
using UnityEngine;

namespace RoR2;

[Serializable]
public struct SerializablePickupIndex
{
	[SerializeField]
	public string pickupName;

	public static explicit operator PickupIndex(SerializablePickupIndex serializablePickupIndex)
	{
		return PickupCatalog.FindPickupIndex(serializablePickupIndex.pickupName);
	}
}
