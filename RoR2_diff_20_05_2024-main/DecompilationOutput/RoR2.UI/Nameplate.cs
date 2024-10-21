using TMPro;
using UnityEngine;

namespace RoR2.UI;

public class Nameplate : MonoBehaviour
{
	public TextMeshPro label;

	private CharacterBody body;

	public GameObject aliveObject;

	public GameObject deadObject;

	public SpriteRenderer criticallyHurtSpriteRenderer;

	public SpriteRenderer[] coloredSprites;

	public Color baseColor;

	public Color combatColor;

	public void SetBody(CharacterBody body)
	{
		this.body = body;
	}

	private void LateUpdate()
	{
		string text = "";
		Color color = baseColor;
		bool flag = true;
		bool flag2 = false;
		bool flag3 = false;
		if ((bool)body)
		{
			text = body.GetDisplayName();
			flag = body.healthComponent.alive;
			flag2 = !body.outOfCombat || !body.outOfDanger;
			flag3 = body.healthComponent.isHealthLow;
			CharacterMaster master = body.master;
			if ((bool)master)
			{
				PlayerCharacterMasterController component = master.GetComponent<PlayerCharacterMasterController>();
				if ((bool)component)
				{
					GameObject networkUserObject = component.networkUserObject;
					if ((bool)networkUserObject)
					{
						NetworkUser component2 = networkUserObject.GetComponent<NetworkUser>();
						if ((bool)component2)
						{
							text = component2.userName;
						}
					}
				}
				else
				{
					text = Language.GetString(body.baseNameToken);
				}
			}
		}
		color = (flag2 ? combatColor : baseColor);
		aliveObject.SetActive(flag);
		deadObject.SetActive(!flag);
		if ((bool)criticallyHurtSpriteRenderer)
		{
			criticallyHurtSpriteRenderer.enabled = flag3 && flag;
			criticallyHurtSpriteRenderer.color = HealthBar.GetCriticallyHurtColor();
		}
		if ((bool)label)
		{
			label.text = text;
			label.color = color;
		}
		SpriteRenderer[] array = coloredSprites;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].color = color;
		}
	}
}
