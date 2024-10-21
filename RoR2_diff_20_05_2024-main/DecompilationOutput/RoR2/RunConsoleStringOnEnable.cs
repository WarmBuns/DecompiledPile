using UnityEngine;

namespace RoR2;

public class RunConsoleStringOnEnable : MonoBehaviour
{
	public string consoleString;

	private void OnEnable()
	{
		Console.instance.SubmitCmd(null, consoleString);
	}
}
