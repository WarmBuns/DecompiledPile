using System.Collections.Generic;
using System.Collections.ObjectModel;
using TMPro;
using UnityEngine;

namespace RoR2;

public class DamageDisplay : MonoBehaviour
{
	private static List<DamageDisplay> instancesList;

	private static ReadOnlyCollection<DamageDisplay> _readOnlyInstancesList;

	public TextMeshPro textMeshComponent;

	public AnimationCurve magnitudeCurve;

	public float maxLife = 3f;

	public float gravity = 9.81f;

	public float magnitude = 3f;

	public float offset = 20f;

	private Vector3 velocity;

	public float textMagnitude = 0.01f;

	private float vel;

	private float life;

	private float scale = 1f;

	[HideInInspector]
	public Color baseColor = Color.white;

	[HideInInspector]
	public Color baseOutlineColor = Color.gray;

	private GameObject victim;

	private GameObject attacker;

	private TeamIndex victimTeam;

	private TeamIndex attackerTeam;

	private bool crit;

	private bool heal;

	private Vector3 internalPosition;

	public static ReadOnlyCollection<DamageDisplay> readOnlyInstancesList => _readOnlyInstancesList;

	static DamageDisplay()
	{
		instancesList = new List<DamageDisplay>();
		_readOnlyInstancesList = new ReadOnlyCollection<DamageDisplay>(instancesList);
		UICamera.onUICameraPreCull += OnUICameraPreCull;
		RoR2Application.onUpdate += UpdateAll;
	}

	private void Start()
	{
		velocity = Vector3.Normalize(Vector3.up + new Vector3(Random.Range(0f - offset, offset), 0f, Random.Range(0f - offset, offset))) * magnitude;
		instancesList.Add(this);
		internalPosition = base.transform.position;
	}

	private void OnDestroy()
	{
		instancesList.Remove(this);
	}

	public void SetValues(GameObject victim, GameObject attacker, float damage, bool crit, DamageColorIndex damageColorIndex)
	{
		victimTeam = TeamIndex.Neutral;
		attackerTeam = TeamIndex.Neutral;
		scale = 1f;
		this.victim = victim;
		this.attacker = attacker;
		this.crit = crit;
		baseColor = DamageColor.FindColor(damageColorIndex);
		string text = Mathf.CeilToInt(Mathf.Abs(damage)).ToString();
		heal = damage < 0f;
		if (heal)
		{
			damage = 0f - damage;
			base.transform.parent = victim.transform;
			text = "+" + text;
			baseColor = DamageColor.FindColor(DamageColorIndex.Heal);
			baseOutlineColor = baseColor * Color.gray;
		}
		if ((bool)victim)
		{
			TeamComponent component = victim.GetComponent<TeamComponent>();
			if ((bool)component)
			{
				victimTeam = component.teamIndex;
			}
		}
		if ((bool)attacker)
		{
			TeamComponent component2 = attacker.GetComponent<TeamComponent>();
			if ((bool)component2)
			{
				attackerTeam = component2.teamIndex;
			}
		}
		if (crit)
		{
			text += "!";
			baseOutlineColor = Color.red;
		}
		textMeshComponent.text = text;
		UpdateMagnitude();
	}

	private void UpdateMagnitude()
	{
		float fontSize = magnitudeCurve.Evaluate(life / maxLife) * textMagnitude * scale;
		textMeshComponent.fontSize = fontSize;
	}

	private static void UpdateAll()
	{
		for (int num = instancesList.Count - 1; num >= 0; num--)
		{
			instancesList[num].DoUpdate();
		}
	}

	private void DoUpdate()
	{
		UpdateMagnitude();
		life += Time.deltaTime;
		if (life >= maxLife)
		{
			Object.Destroy(base.gameObject);
			return;
		}
		velocity += gravity * Vector3.down * Time.deltaTime;
		internalPosition += velocity * Time.deltaTime;
	}

	private static void OnUICameraPreCull(UICamera uiCamera)
	{
		GameObject gameObject = null;
		TeamIndex teamIndex = TeamIndex.Neutral;
		Camera camera = uiCamera.camera;
		Camera sceneCam = uiCamera.cameraRigController.sceneCam;
		gameObject = uiCamera.cameraRigController.target;
		teamIndex = uiCamera.cameraRigController.targetTeamIndex;
		for (int i = 0; i < instancesList.Count; i++)
		{
			DamageDisplay damageDisplay = instancesList[i];
			Color color = Color.white;
			if (!damageDisplay.heal)
			{
				if (teamIndex == damageDisplay.victimTeam)
				{
					color = new Color(0.5568628f, 0.29411766f, 0.6039216f);
				}
				else if (teamIndex == damageDisplay.attackerTeam && gameObject != damageDisplay.attacker)
				{
					color = Color.gray;
				}
			}
			damageDisplay.textMeshComponent.color = Color.Lerp(Color.white, damageDisplay.baseColor * color, damageDisplay.life / 0.2f);
			damageDisplay.textMeshComponent.outlineColor = Color.Lerp(Color.white, damageDisplay.baseOutlineColor * color, damageDisplay.life / 0.2f);
			Vector3 position = damageDisplay.internalPosition;
			Vector3 position2 = sceneCam.WorldToScreenPoint(position);
			position2.z = ((position2.z > 0f) ? 1f : (-1f));
			Vector3 position3 = camera.ScreenToWorldPoint(position2);
			damageDisplay.transform.position = position3;
		}
	}
}
