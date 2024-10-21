using UnityEngine;
using UnityEngine.Events;

namespace RoR2.EntityLogic;

public class Repeat : MonoBehaviour
{
	public UnityEvent repeatedEvent;

	public void CallRepeat(int repeatNumber)
	{
		while (repeatNumber > 0)
		{
			repeatNumber--;
			repeatedEvent?.Invoke();
		}
	}
}
