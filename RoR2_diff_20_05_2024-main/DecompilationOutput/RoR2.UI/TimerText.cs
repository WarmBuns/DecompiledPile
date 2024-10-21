using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace RoR2.UI;

[RequireComponent(typeof(RectTransform))]
public class TimerText : MonoBehaviour
{
	[FormerlySerializedAs("targetText")]
	public TextMeshProUGUI targetLabel;

	[SerializeField]
	private TimerStringFormatter _format;

	[SerializeField]
	private double _seconds;

	private static readonly StringBuilder sharedStringBuilder = new StringBuilder();

	private int currentMinutes = -1;

	private int currentSeconds = -1;

	public TimerStringFormatter format
	{
		get
		{
			return _format;
		}
		set
		{
			if (!(_format == value))
			{
				_format = value;
				Rebuild();
			}
		}
	}

	public double seconds
	{
		get
		{
			return _seconds;
		}
		set
		{
			if (_seconds != value)
			{
				_seconds = value;
				Rebuild();
			}
		}
	}

	private void Awake()
	{
		Rebuild();
	}

	private void Rebuild()
	{
		if ((bool)targetLabel)
		{
			sharedStringBuilder.Clear();
			if ((bool)format)
			{
				format.AppendToStringBuilder(seconds, sharedStringBuilder);
			}
			targetLabel.SetText(sharedStringBuilder);
		}
	}

	private void OnValidate()
	{
		Rebuild();
		if (!targetLabel)
		{
			Debug.LogErrorFormat(this, "TimerText does not specify a target label.");
		}
	}
}
