using UnityEngine;

namespace RoR2.WwiseUtils;

public class RtpcSetBehavior : MonoBehaviour
{
	[SerializeField]
	private string valueName;

	[SerializeField]
	private float initialValue;

	private RtpcSetter rtpcSetter;

	public float value
	{
		get
		{
			return rtpcSetter.value;
		}
		set
		{
			rtpcSetter.value = value;
			rtpcSetter.FlushIfChanged();
		}
	}

	private void Start()
	{
		rtpcSetter = new RtpcSetter(valueName, base.gameObject);
		rtpcSetter.value = initialValue;
		rtpcSetter.FlushIfChanged();
	}
}
