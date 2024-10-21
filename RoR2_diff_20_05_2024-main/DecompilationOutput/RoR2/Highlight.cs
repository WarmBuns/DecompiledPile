using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace RoR2;

public class Highlight : MonoBehaviour
{
	public enum HighlightColor
	{
		interactive,
		teleporter,
		pickup,
		custom,
		unavailable
	}

	private static List<Highlight> highlightList = new List<Highlight>();

	private static ReadOnlyCollection<Highlight> _readonlyHighlightList = new ReadOnlyCollection<Highlight>(highlightList);

	private IDisplayNameProvider displayNameProvider;

	[HideInInspector]
	public PickupIndex pickupIndex;

	public Renderer targetRenderer;

	public float strength = 1f;

	public HighlightColor highlightColor;

	public Color CustomColor = Color.magenta;

	public bool isOn;

	public static ReadOnlyCollection<Highlight> readonlyHighlightList => _readonlyHighlightList;

	private void Awake()
	{
		displayNameProvider = GetComponent<IDisplayNameProvider>();
	}

	private void RemoveHighlightIfInactive()
	{
		if (targetRenderer != null && !targetRenderer.gameObject.activeInHierarchy && highlightList.Contains(this))
		{
			highlightList.Remove(this);
		}
	}

	public Color GetColor()
	{
		return highlightColor switch
		{
			HighlightColor.interactive => ColorCatalog.GetColor(ColorCatalog.ColorIndex.Interactable), 
			HighlightColor.teleporter => ColorCatalog.GetColor(ColorCatalog.ColorIndex.Teleporter), 
			HighlightColor.pickup => PickupCatalog.GetPickupDef(pickupIndex)?.baseColor ?? PickupCatalog.invalidPickupColor, 
			HighlightColor.custom => CustomColor, 
			_ => Color.magenta, 
		};
	}

	private void Update()
	{
		RemoveHighlightIfInactive();
	}

	public void OnDisable()
	{
		highlightList.Remove(this);
	}

	public void OnEnable()
	{
		highlightList.Add(this);
	}

	public void ResetHighlight()
	{
	}
}
