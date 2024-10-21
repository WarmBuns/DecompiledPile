using System;
using UnityEngine;

namespace RoR2;

[Serializable]
public struct GameObjectsToModify
{
	public GameObject[] objects;

	public Material idleMaterial;

	public Material eventMaterial;
}
