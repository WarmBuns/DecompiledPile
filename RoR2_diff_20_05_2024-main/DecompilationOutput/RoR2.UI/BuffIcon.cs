using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI;

[RequireComponent(typeof(RectTransform))]
public class BuffIcon : MonoBehaviour
{
	public BuffDef buffDef;

	private BuffDef lastBuffDef;

	public int buffCount;

	private int lastBuffCount;

	public Image iconImage;

	public TextMeshProUGUI stackCount;

	private float stopwatch;

	private const float flashDuration = 0.25f;

	private static readonly StringBuilder sharedStringBuilder = new StringBuilder();

	public RectTransform rectTransform { get; private set; }

	private void Awake()
	{
		rectTransform = GetComponent<RectTransform>();
	}

	private void OnEnable()
	{
		UpdateIcon();
	}

	private void OnDisable()
	{
		ResetBuffIcon();
	}

	public void ResetBuffIcon()
	{
		buffDef = null;
		lastBuffCount = -1;
	}

	public void Flash()
	{
		iconImage.color = Color.white;
		iconImage.CrossFadeColor(buffDef.buffColor, 0.25f, ignoreTimeScale: true, useAlpha: false);
	}

	public void UpdateIcon()
	{
		if (buffDef == lastBuffDef && buffCount == lastBuffCount)
		{
			return;
		}
		lastBuffDef = buffDef;
		lastBuffCount = buffCount;
		if ((bool)buffDef)
		{
			iconImage.enabled = true;
			iconImage.sprite = buffDef.iconSprite;
			iconImage.color = buffDef.buffColor;
			if (buffDef.canStack)
			{
				sharedStringBuilder.Clear();
				sharedStringBuilder.Append("x");
				sharedStringBuilder.AppendInt(buffCount);
				stackCount.enabled = true;
				stackCount.SetText(sharedStringBuilder);
			}
			else
			{
				stackCount.enabled = false;
			}
		}
		else
		{
			iconImage.sprite = null;
			iconImage.enabled = false;
			stackCount.enabled = false;
		}
	}
}
