using UnityEngine;

namespace RoR2.UI;

[RequireComponent(typeof(RectTransform))]
public class ServerBrowserStripController : MonoBehaviour
{
	private RectTransform rectTransform;

	public HGTextMeshProUGUI nameLabel;

	public HGTextMeshProUGUI addressLabel;

	public HGTextMeshProUGUI playerCountLabel;

	public HGTextMeshProUGUI latencyLabel;

	private void Awake()
	{
		rectTransform = (RectTransform)base.transform;
	}
}
