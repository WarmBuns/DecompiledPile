using System;
using EntityStates;
using UnityEngine;

namespace RoR2;

public class CharacterEmoteDefinitions : MonoBehaviour
{
	[Serializable]
	public struct EmoteDef
	{
		public string name;

		public string displayName;

		public EntityStateMachine targetStateMachine;

		public SerializableEntityStateType state;
	}

	public EmoteDef[] emoteDefinitions;

	public int FindEmoteIndex(string name)
	{
		for (int i = 0; i < emoteDefinitions.Length; i++)
		{
			if (emoteDefinitions[i].name == name)
			{
				return i;
			}
		}
		return -1;
	}
}
