using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace RoR2.UI;

[CreateAssetMenu(menuName = "RoR2/UI/HealthBarStyle")]
public class HealthBarStyle : ScriptableObject
{
	[Serializable]
	public struct BarStyle
	{
		public bool enabled;

		public Color baseColor;

		public Sprite sprite;

		public Image.Type imageType;

		public float sizeDelta;
	}

	public GameObject barPrefab;

	public bool flashOnHealthCritical;

	[FormerlySerializedAs("trailingBarStyle")]
	public BarStyle trailingUnderHealthBarStyle;

	[FormerlySerializedAs("healthBarStyle")]
	public BarStyle instantHealthBarStyle;

	public BarStyle trailingOverHealthBarStyle;

	public BarStyle shieldBarStyle;

	public BarStyle curseBarStyle;

	public BarStyle barrierBarStyle;

	public BarStyle flashBarStyle;

	public BarStyle cullBarStyle;

	public BarStyle magneticStyle;

	public BarStyle ospStyle;

	public BarStyle lowHealthOverStyle;

	public BarStyle lowHealthUnderStyle;
}
