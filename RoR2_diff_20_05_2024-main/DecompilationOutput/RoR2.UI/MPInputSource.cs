using System;
using Rewired.UI;
using UnityEngine;

namespace RoR2.UI;

public class MPInputSource : IMouseInputSource
{
	public int playerId { get; }

	public bool enabled { get; }

	public bool locked { get; }

	public int buttonCount { get; }

	public Vector2 screenPosition { get; }

	public Vector2 screenPositionDelta { get; }

	public Vector2 wheelDelta { get; }

	public bool GetButtonDown(int button)
	{
		throw new NotImplementedException();
	}

	public bool GetButtonUp(int button)
	{
		throw new NotImplementedException();
	}

	public bool GetButton(int button)
	{
		throw new NotImplementedException();
	}
}
