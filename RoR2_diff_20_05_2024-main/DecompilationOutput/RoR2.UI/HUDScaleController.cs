using System.Collections.Generic;
using RoR2.ConVar;
using UnityEngine;

namespace RoR2.UI;

public class HUDScaleController : MonoBehaviour
{
	private class HUDScaleConVar : BaseConVar
	{
		public static HUDScaleConVar instance = new HUDScaleConVar("hud_scale", ConVarFlags.Archive, "100", "Scales the size of HUD elements in-game. Defaults to 100.");

		private int intValue;

		private HUDScaleConVar(string name, ConVarFlags flags, string defaultValue, string helpText)
			: base(name, flags, defaultValue, helpText)
		{
		}

		public override void SetString(string newValue)
		{
			if (!TextSerialization.TryParseInvariant(newValue, out int result) || result == 0)
			{
				return;
			}
			intValue = result;
			foreach (HUDScaleController instances in instancesList)
			{
				instances.SetScale();
			}
		}

		public override string GetString()
		{
			return TextSerialization.ToStringInvariant(intValue);
		}
	}

	public RectTransform[] rectTransforms;

	private static List<HUDScaleController> instancesList = new List<HUDScaleController>();

	public void OnEnable()
	{
		instancesList.Add(this);
	}

	public void OnDisable()
	{
		instancesList.Remove(this);
	}

	private void Start()
	{
		SetScale();
	}

	private void SetScale()
	{
		BaseConVar baseConVar = Console.instance.FindConVar("hud_scale");
		if (baseConVar != null && TextSerialization.TryParseInvariant(baseConVar.GetString(), out float result))
		{
			Vector3 localScale = new Vector3(result / 100f, result / 100f, result / 100f);
			RectTransform[] array = rectTransforms;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].localScale = localScale;
			}
		}
	}
}
