using UnityEngine;

namespace RoR2;

public class PhaseCounter : MonoBehaviour
{
	private static PhaseCounter _instance;

	public static PhaseCounter instance => _instance;

	public int phase { get; private set; }

	private void OnEnable()
	{
		if (!_instance)
		{
			_instance = this;
		}
	}

	private void OnDisable()
	{
		if (_instance == this)
		{
			_instance = null;
		}
	}

	public void GoToNextPhase()
	{
		phase++;
		Debug.LogFormat("Entering phase {0}", phase);
	}
}
