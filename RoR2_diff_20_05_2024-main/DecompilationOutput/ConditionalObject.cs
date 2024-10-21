using UnityEngine;

[DefaultExecutionOrder(-5)]
public class ConditionalObject : MonoBehaviour
{
	public bool enabledOnSwitch = true;

	public bool enabledOnXbox = true;

	public bool enabledOnPlaystation = true;

	public bool disableInProduction;

	public bool enabledOnSteam = true;

	public bool enabledOnEGS = true;

	public bool disableIfNoActiveRun;

	private void CheckConditions()
	{
		bool flag = false || enabledOnEGS || enabledOnSteam;
		base.gameObject.SetActive(flag && !disableInProduction);
	}

	private void Awake()
	{
		CheckConditions();
	}
}
