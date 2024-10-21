using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI;

[RequireComponent(typeof(LayoutElement))]
[RequireComponent(typeof(RectTransform))]
public class AllyCardController : MonoBehaviour
{
	public HealthBar healthBar;

	public TextMeshProUGUI nameLabel;

	public RawImage portraitIconImage;

	private CharacterMaster cachedSourceMaster;

	private CharacterBody cachedSourceCharacterBody;

	private HealthComponent cachedHealthComponent;

	private string cachedBestName = "";

	private Texture cachedPortraitIcon;

	public bool shouldIndent { get; set; }

	public CharacterMaster sourceMaster { get; set; }

	public RectTransform rectTransform { get; private set; }

	public LayoutElement layoutElement { get; private set; }

	private void Awake()
	{
		rectTransform = (RectTransform)base.transform;
		layoutElement = GetComponent<LayoutElement>();
	}

	private void LateUpdate()
	{
		if (ShouldWeUpdate())
		{
			UpdateInfo();
		}
	}

	private bool ShouldWeUpdate()
	{
		if (sourceMaster != cachedSourceMaster)
		{
			return true;
		}
		CharacterBody body = sourceMaster.GetBody();
		if (body != cachedSourceCharacterBody)
		{
			return true;
		}
		if ((bool)body)
		{
			if (cachedHealthComponent != body.healthComponent || cachedPortraitIcon != body.portraitIcon)
			{
				return true;
			}
			if (cachedBestName != Util.GetBestBodyName(body.gameObject))
			{
				return true;
			}
		}
		else if (cachedBestName != Util.GetBestMasterName(sourceMaster))
		{
			return true;
		}
		return false;
	}

	private void UpdateInfo()
	{
		HealthComponent source = null;
		string text = "";
		Texture texture = null;
		if ((bool)sourceMaster)
		{
			cachedSourceMaster = sourceMaster;
			CharacterBody body = sourceMaster.GetBody();
			if ((bool)body)
			{
				texture = body.portraitIcon;
				source = body.healthComponent;
				text = Util.GetBestBodyName(body.gameObject);
				cachedSourceCharacterBody = body;
			}
			else
			{
				text = Util.GetBestMasterName(sourceMaster);
				cachedSourceCharacterBody = null;
			}
		}
		else
		{
			cachedSourceMaster = null;
		}
		healthBar.source = source;
		nameLabel.text = text;
		portraitIconImage.texture = texture;
		portraitIconImage.enabled = texture != null;
		cachedHealthComponent = source;
		cachedBestName = text;
		cachedPortraitIcon = texture;
	}
}
