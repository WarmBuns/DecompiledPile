using UnityEngine;
using UnityEngine.Events;

namespace RoR2.EntityLogic;

public class Counter : MonoBehaviour
{
	public int value;

	public int threshold;

	public UnityEvent onTrigger;

	public void Add(int valueToAdd)
	{
		value += valueToAdd;
		if (value >= threshold)
		{
			onTrigger.Invoke();
		}
	}

	public void SetValue(int newValue)
	{
		value = newValue;
	}
}
