using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[Serializable]
public struct HurtBoxReference : IEquatable<HurtBoxReference>
{
	public GameObject rootObject;

	public byte hurtBoxIndexPlusOne;

	public static HurtBoxReference FromHurtBox(HurtBox hurtBox)
	{
		if (!hurtBox)
		{
			return default(HurtBoxReference);
		}
		HurtBoxReference result = default(HurtBoxReference);
		result.rootObject = (hurtBox.healthComponent ? hurtBox.healthComponent.gameObject : null);
		result.hurtBoxIndexPlusOne = (byte)(hurtBox.indexInGroup + 1);
		return result;
	}

	public static HurtBoxReference FromRootObject(GameObject rootObject)
	{
		HurtBoxReference result = default(HurtBoxReference);
		result.rootObject = rootObject;
		result.hurtBoxIndexPlusOne = 0;
		return result;
	}

	public GameObject ResolveGameObject()
	{
		if (hurtBoxIndexPlusOne == 0)
		{
			return rootObject;
		}
		if ((bool)rootObject)
		{
			ModelLocator component = rootObject.GetComponent<ModelLocator>();
			if ((bool)component && (bool)component.modelTransform)
			{
				HurtBoxGroup component2 = component.modelTransform.GetComponent<HurtBoxGroup>();
				if ((bool)component2 && component2.hurtBoxes != null)
				{
					int num = hurtBoxIndexPlusOne - 1;
					if (num < component2.hurtBoxes.Length)
					{
						return component2.hurtBoxes[num].gameObject;
					}
				}
			}
		}
		return null;
	}

	public HurtBox ResolveHurtBox()
	{
		GameObject gameObject = ResolveGameObject();
		if (!gameObject)
		{
			return null;
		}
		return gameObject.GetComponent<HurtBox>();
	}

	public void Write(NetworkWriter writer)
	{
		writer.Write(rootObject);
		writer.Write(hurtBoxIndexPlusOne);
	}

	public void Read(NetworkReader reader)
	{
		rootObject = reader.ReadGameObject();
		hurtBoxIndexPlusOne = reader.ReadByte();
	}

	public bool Equals(HurtBoxReference other)
	{
		if (object.Equals(rootObject, other.rootObject))
		{
			return hurtBoxIndexPlusOne == other.hurtBoxIndexPlusOne;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is HurtBoxReference other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (((rootObject != null) ? rootObject.GetHashCode() : 0) * 397) ^ hurtBoxIndexPlusOne.GetHashCode();
	}
}
