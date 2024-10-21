using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI;

public class SkillIcon : MonoBehaviour
{
	public SkillSlot targetSkillSlot;

	public PlayerCharacterMasterController playerCharacterMasterController;

	public GenericSkill targetSkill;

	public Image iconImage;

	public RawImage cooldownRemapPanel;

	public TextMeshProUGUI cooldownText;

	public TextMeshProUGUI stockText;

	public GameObject flashPanelObject;

	public GameObject isReadyPanelObject;

	public Animator animator;

	public string animatorStackString;

	public bool wasReady;

	public int previousStock;

	public TooltipProvider tooltipProvider;

	public int previousCoolDownInt;

	private static readonly StringBuilder sharedStringBuilder = new StringBuilder();

	private int previousSkillIndex = -1;

	private bool skillChanged;

	private void CheckAndRegisterSkillChange()
	{
		if (targetSkill.skillDef.skillIndex != previousSkillIndex)
		{
			skillChanged = previousSkillIndex != -1;
			previousStock = -1;
			previousSkillIndex = targetSkill.skillDef.skillIndex;
		}
	}

	private void Update()
	{
		if ((bool)targetSkill)
		{
			CheckAndRegisterSkillChange();
			if ((bool)tooltipProvider)
			{
				Color bodyColor = targetSkill.characterBody.bodyColor;
				SurvivorCatalog.GetSurvivorIndexFromBodyIndex(targetSkill.characterBody.bodyIndex);
				Color.RGBToHSV(bodyColor, out var H, out var S, out var V);
				V = ((V > 0.7f) ? 0.7f : V);
				bodyColor = Color.HSVToRGB(H, S, V);
				tooltipProvider.titleColor = bodyColor;
				tooltipProvider.titleToken = targetSkill.skillNameToken;
				tooltipProvider.bodyToken = targetSkill.skillDescriptionToken;
			}
			float cooldownRemaining = targetSkill.cooldownRemaining;
			float num = targetSkill.CalculateFinalRechargeInterval();
			int stock = targetSkill.stock;
			bool flag = stock > 0 || cooldownRemaining == 0f;
			bool flag2 = targetSkill.IsReady();
			bool flag3 = targetSkill.maxStock > 1 && !targetSkill.skillDef.hideStockCount;
			if (previousStock < stock && !skillChanged)
			{
				Util.PlaySound("Play_UI_cooldownRefresh", RoR2Application.instance.gameObject);
			}
			if ((bool)animator)
			{
				if (flag3)
				{
					animator.SetBool(animatorStackString, value: true);
				}
				else
				{
					animator.SetBool(animatorStackString, value: false);
				}
			}
			if ((bool)isReadyPanelObject)
			{
				isReadyPanelObject.SetActive(flag2);
			}
			if (!wasReady && flag && (bool)flashPanelObject)
			{
				flashPanelObject.SetActive(value: true);
			}
			if ((bool)cooldownText)
			{
				if (flag)
				{
					cooldownText.gameObject.SetActive(value: false);
				}
				else
				{
					int num2 = Mathf.CeilToInt(cooldownRemaining);
					if (previousCoolDownInt != num2)
					{
						previousCoolDownInt = num2;
						sharedStringBuilder.Clear();
						sharedStringBuilder.AppendInt(num2);
						cooldownText.SetText(sharedStringBuilder);
						cooldownText.gameObject.SetActive(value: true);
					}
				}
			}
			if ((bool)iconImage)
			{
				iconImage.enabled = true;
				iconImage.color = (flag2 ? Color.white : Color.gray);
				iconImage.sprite = targetSkill.icon;
			}
			if ((bool)cooldownRemapPanel)
			{
				float num3 = 1f;
				if (num >= Mathf.Epsilon)
				{
					num3 = 1f - cooldownRemaining / num;
				}
				float num4 = num3;
				cooldownRemapPanel.enabled = num4 < 1f;
				cooldownRemapPanel.color = new Color(1f, 1f, 1f, num3);
			}
			if ((bool)stockText)
			{
				if (flag3)
				{
					if (previousStock != stock)
					{
						stockText.gameObject.SetActive(value: true);
						sharedStringBuilder.Clear();
						sharedStringBuilder.AppendInt(stock);
						stockText.SetText(sharedStringBuilder);
					}
				}
				else
				{
					stockText.gameObject.SetActive(value: false);
				}
			}
			wasReady = flag;
			previousStock = stock;
			skillChanged = false;
		}
		else
		{
			if ((bool)tooltipProvider)
			{
				tooltipProvider.bodyColor = Color.gray;
				tooltipProvider.titleToken = "";
				tooltipProvider.bodyToken = "";
			}
			if ((bool)cooldownText)
			{
				cooldownText.gameObject.SetActive(value: false);
			}
			if ((bool)stockText)
			{
				stockText.gameObject.SetActive(value: false);
			}
			if ((bool)iconImage)
			{
				iconImage.enabled = false;
				iconImage.sprite = null;
			}
		}
	}
}
