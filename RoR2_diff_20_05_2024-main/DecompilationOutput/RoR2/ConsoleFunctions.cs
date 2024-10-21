using UnityEngine;

namespace RoR2;

public class ConsoleFunctions : MonoBehaviour
{
	public void SubmitCmd(string cmd)
	{
		Console.instance.SubmitCmd(null, cmd);
	}
}
