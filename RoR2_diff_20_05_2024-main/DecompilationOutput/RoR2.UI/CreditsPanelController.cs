using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EntityStates;
using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class CreditsPanelController : MonoBehaviour
{
	public abstract class BaseCreditsPanelState : BaseState
	{
		protected CreditsPanelController creditsPanelController { get; private set; }

		protected abstract bool enableSkipButton { get; }

		public override void OnEnter()
		{
			base.OnEnter();
			creditsPanelController = GetComponent<CreditsPanelController>();
			if ((bool)creditsPanelController && (bool)creditsPanelController.skipButton)
			{
				creditsPanelController.skipButton.gameObject.SetActive(enableSkipButton);
			}
		}

		protected void SetScroll(float scroll)
		{
			if ((bool)creditsPanelController)
			{
				creditsPanelController.SetScroll(scroll);
			}
		}

		protected void SetFade(float fade)
		{
			fade = Mathf.Clamp01(fade);
			if ((bool)creditsPanelController && (bool)creditsPanelController.fadePanel)
			{
				Color color = creditsPanelController.fadePanel.color;
				color.a = fade;
				creditsPanelController.fadePanel.color = color;
			}
		}
	}

	public class IntroState : BaseCreditsPanelState
	{
		protected override bool enableSkipButton => false;

		public override void OnEnter()
		{
			base.OnEnter();
			SetScroll(0f);
			SetFade(base.creditsPanelController.introFadeCurve.Evaluate(0f));
			base.creditsPanelController.StartCoroutine(PrepareContent());
		}

		public override void Update()
		{
			base.Update();
			float num = Mathf.Clamp01(base.age / base.creditsPanelController.introDuration);
			SetFade(1f - num);
			if (num >= 1f)
			{
				outer.SetNextState(new MainScrollState());
			}
		}

		private IEnumerator PrepareContent()
		{
			SetIgnoreLayout(value: false);
			yield return null;
			VerticalLayoutGroup component = base.creditsPanelController.content.GetComponent<VerticalLayoutGroup>();
			if (component != null)
			{
				component.enabled = false;
			}
			ContentSizeFitter component2 = base.creditsPanelController.content.GetComponent<ContentSizeFitter>();
			if (component2 != null)
			{
				component2.enabled = false;
			}
			yield return null;
			SetIgnoreLayout(value: true);
		}

		private void SetIgnoreLayout(bool value)
		{
			for (int i = 0; i < base.creditsPanelController.content.childCount; i++)
			{
				LayoutElement component = base.creditsPanelController.content.GetChild(i).GetComponent<LayoutElement>();
				if (component != null)
				{
					component.ignoreLayout = value;
				}
			}
		}
	}

	public class MainScrollState : BaseCreditsPanelState
	{
		public static AnimationCurve scrollCurve;

		protected override bool enableSkipButton => true;

		public override void OnEnter()
		{
			base.OnEnter();
			SetFade(0f);
			base.creditsPanelController.skipButton.gameObject.SetActive(value: true);
		}

		public override void Update()
		{
			base.Update();
			float num = Mathf.Clamp01(base.age / base.creditsPanelController.scrollDuration);
			SetScroll(scrollCurve.Evaluate(num));
			base.creditsPanelController.DisableInvisibleElements();
			if (num >= 1f)
			{
				outer.SetNextState(new OutroState());
			}
		}
	}

	public class OutroState : BaseCreditsPanelState
	{
		protected override bool enableSkipButton => true;

		public override void OnEnter()
		{
			base.OnEnter();
			SetScroll(1f);
			SetFade(base.creditsPanelController.outroFadeCurve.Evaluate(0f));
		}

		public override void Update()
		{
			base.Update();
			float num = Mathf.Clamp01(base.age / base.creditsPanelController.outroDuration);
			SetFade(num);
			if (num >= 1f)
			{
				EntityState.Destroy(base.gameObject);
			}
		}
	}

	public RectTransform content;

	public ScrollRect scrollRect;

	public float introDuration;

	public AnimationCurve introFadeCurve;

	public float outroDuration;

	public AnimationCurve outroFadeCurve;

	public float scrollDuration;

	public RawImage fadePanel;

	public MPButton skipButton;

	public VoteInfoPanelController voteInfoPanel;

	[Range(0f, 1f)]
	public float editorScroll;

	private Dictionary<Canvas, List<CanvasRenderer>> canvases = new Dictionary<Canvas, List<CanvasRenderer>>();

	private const float SCREEN_HEIGHT = 1080f;

	private void OnEnable()
	{
		InstanceTracker.Add(this);
		GameObject[] array = GameObject.FindGameObjectsWithTag("CreditsGroup");
		for (int i = 0; i < array.Length; i++)
		{
			Canvas component = array[i].GetComponent<Canvas>();
			if (component != null)
			{
				canvases.Add(component, new List<CanvasRenderer>(component.GetComponentsInChildren<CanvasRenderer>()));
			}
		}
	}

	private void OnDisable()
	{
		InstanceTracker.Remove(this);
	}

	private void SetScroll(float scroll)
	{
		if ((bool)scrollRect)
		{
			scrollRect.verticalNormalizedPosition = 1f - scroll;
		}
	}

	private void Update()
	{
		if (!Application.IsPlaying(this))
		{
			SetScroll(editorScroll);
		}
	}

	public void DisableInvisibleElements()
	{
		float height = scrollRect.viewport.rect.height;
		float num = 100f;
		for (int i = 1; i < scrollRect.content.childCount; i++)
		{
			RectTransform component = scrollRect.content.GetChild(i).GetComponent<RectTransform>();
			float num2 = component.sizeDelta.y * 0.5f + num;
			float num3 = Mathf.Abs(component.localPosition.y) - num2;
			float num4 = Mathf.Abs(component.localPosition.y) + num2;
			float y = ((RectTransform)content.transform).anchoredPosition.y;
			bool active = num4 > y && num3 < y + height;
			component.gameObject.SetActive(active);
		}
	}

	[ContextMenu("Generate Credits Roles JSON")]
	private void GenerateCreditsRoles()
	{
		CreditsStripGroupBuilder[] componentsInChildren = GetComponentsInChildren<CreditsStripGroupBuilder>();
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		CreditsStripGroupBuilder[] array = componentsInChildren;
		for (int i = 0; i < array.Length; i++)
		{
			foreach (var namesAndEnglishRole in array[i].GetNamesAndEnglishRoles())
			{
				string text = CreditsStripGroupBuilder.EnglishRoleToToken(namesAndEnglishRole.englishRoleName);
				if (dictionary.TryGetValue(text, out var value))
				{
					if (!string.Equals(namesAndEnglishRole.englishRoleName, value, StringComparison.Ordinal))
					{
						Debug.LogError("Conflict in role \"" + text + "\": a=\"" + value + "\" b=\"" + namesAndEnglishRole.englishRoleName + "\"");
					}
				}
				else
				{
					dictionary.Add(text, namesAndEnglishRole.englishRoleName);
				}
			}
		}
		List<KeyValuePair<string, string>> list = dictionary.OrderBy((KeyValuePair<string, string> kv) => kv.Key).ToList();
		StringBuilder stringBuilder = new StringBuilder();
		foreach (KeyValuePair<string, string> item in list)
		{
			stringBuilder.Append("\"").Append(item.Key).Append("\": \"")
				.Append(item.Value)
				.Append("\",")
				.AppendLine();
		}
		GUIUtility.systemCopyBuffer = stringBuilder.ToString();
	}

	[ConCommand(commandName = "credits_start", flags = ConVarFlags.None, helpText = "Begins the credits sequence.")]
	private static void CCCreditsStart(ConCommandArgs args)
	{
		if (InstanceTracker.GetInstancesList<CreditsPanelController>().Count != 0)
		{
			throw new ConCommandException("Already in credits sequence.");
		}
		CreditsPanelController creditsPanelController = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/UI/Credits/CreditsPanel"), RoR2Application.instance.mainCanvas.transform).GetComponent<CreditsPanelController>();
		creditsPanelController.skipButton.onClick.AddListener(delegate
		{
			UnityEngine.Object.Destroy(creditsPanelController.gameObject);
		});
	}
}
