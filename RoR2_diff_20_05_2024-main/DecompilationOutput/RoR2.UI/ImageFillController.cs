using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI;

public class ImageFillController : MonoBehaviour
{
	[SerializeField]
	private Image[] images;

	[SerializeField]
	private float fillScalar;

	public void OnEnable()
	{
		SetTValue(0f);
	}

	public void SetTValue(float t)
	{
		float fillAmount = fillScalar * t;
		Image[] array = images;
		foreach (Image image in array)
		{
			if ((bool)image)
			{
				image.fillAmount = fillAmount;
			}
		}
	}
}
