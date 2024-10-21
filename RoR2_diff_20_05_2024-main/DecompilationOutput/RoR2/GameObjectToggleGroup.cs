using System;
using UnityEngine;

namespace RoR2;

[Serializable]
public struct GameObjectToggleGroup
{
	public GameObject[] objects;

	public int minEnabled;

	public int maxEnabled;
}
