using System;
using UnityEngine;

namespace RoR2;

[CreateAssetMenu(menuName = "RoR2/SimpleSpriteAnimation")]
public class SimpleSpriteAnimation : ScriptableObject
{
	[Serializable]
	public struct Frame
	{
		public Sprite sprite;

		public int duration;
	}

	public float frameRate = 60f;

	public Frame[] frames = Array.Empty<Frame>();
}
