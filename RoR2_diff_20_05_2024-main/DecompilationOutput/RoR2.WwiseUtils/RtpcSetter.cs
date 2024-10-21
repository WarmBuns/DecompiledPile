using UnityEngine;

namespace RoR2.WwiseUtils;

public struct RtpcSetter
{
	private readonly string name;

	private readonly uint id;

	private readonly GameObject gameObject;

	private float expectedEngineValue;

	public float value;

	private bool gameObjectIsNull;

	public RtpcSetter(string name, GameObject gameObject = null)
	{
		this.name = name;
		id = AkSoundEngine.GetIDFromString(name);
		this.gameObject = gameObject;
		gameObjectIsNull = gameObject == null;
		expectedEngineValue = float.NegativeInfinity;
		value = expectedEngineValue;
	}

	public void FlushIfChanged()
	{
		if (!expectedEngineValue.Equals(value))
		{
			expectedEngineValue = value;
			if (gameObjectIsNull)
			{
				AkSoundEngine.SetRTPCValue(id, value, ulong.MaxValue);
			}
			else
			{
				AkSoundEngine.SetRTPCValue(id, value, gameObject);
			}
		}
	}
}
