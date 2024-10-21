using Rewired;
using UnityEngine;
using UnityEngine.Events;

namespace RoR2;

public class InputResponse : MonoBehaviour
{
	public string[] inputActionNames;

	public UnityEvent onPress;

	private void Update()
	{
		foreach (Player player in ReInput.players.Players)
		{
			for (int i = 0; i < inputActionNames.Length; i++)
			{
				if (player.GetButtonDown(inputActionNames[i]))
				{
					onPress?.Invoke();
					break;
				}
			}
		}
	}
}
