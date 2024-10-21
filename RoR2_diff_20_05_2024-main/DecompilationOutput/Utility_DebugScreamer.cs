using UnityEngine;

public class Utility_DebugScreamer : MonoBehaviour
{
	[SerializeField]
	private bool screamOnFixedTime;

	[SerializeField]
	private bool screamOnUpdate;

	[SerializeField]
	private Color _screamColor = new Color(49f / 51f, 36f / 85f, 0.007843138f);

	private string colorHex;

	private float timeBorn;

	private float fixedTimeBorn;

	private float timeDisabled;

	private float fixedTimeDisabled;

	private string _goName;

	private void Awake()
	{
		timeBorn = Time.time;
		fixedTimeBorn = Time.fixedTime;
		_goName = base.gameObject.name;
		colorHex = "#" + ColorUtility.ToHtmlStringRGB(_screamColor);
	}

	private void FixedUpdate()
	{
		_ = screamOnFixedTime;
	}

	private void Update()
	{
		_ = screamOnUpdate;
	}

	private void OnDisable()
	{
		timeDisabled = Time.time;
		fixedTimeDisabled = Time.fixedTime;
		string text = _goName + " Screamer: <color = " + colorHex + " > DISABLED</color> ";
		if (screamOnFixedTime)
		{
			text = text + " (FixedTime of Disable: " + fixedTimeDisabled + " / " + (fixedTimeDisabled - fixedTimeBorn) + "s alive)";
		}
		if (screamOnUpdate)
		{
			text = text + " (Time of Disable: " + timeDisabled + " / " + (timeDisabled - timeBorn) + "s alive)";
		}
	}

	private void OnDestroy()
	{
		float time = Time.time;
		float fixedTime = Time.fixedTime;
		string text = _goName + " Screamer: <color = " + colorHex + " > DISABLED</color> ";
		if (screamOnFixedTime)
		{
			text = text + " (FixedTime of Destroy: " + fixedTime + " / " + (fixedTime - fixedTimeBorn) + "s alive)";
		}
		if (screamOnUpdate)
		{
			text = text + " (Time of Destroy: " + time + " / " + (time - timeBorn) + "s alive)";
		}
	}
}
