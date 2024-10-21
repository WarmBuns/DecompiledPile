using System;
using TMPro;
using UnityEngine;

namespace RoR2.UI;

public class LanguageTextMeshController : MonoBehaviour
{
	[SerializeField]
	private string _token;

	private string previousToken;

	private string resolvedString;

	private TMP_Text textMeshPro;

	private object[] _formatArgs = Array.Empty<object>();

	public string token
	{
		get
		{
			return _token;
		}
		set
		{
			if (value != previousToken)
			{
				_token = value;
				ResolveString();
				UpdateLabel();
			}
		}
	}

	public object[] formatArgs
	{
		get
		{
			return _formatArgs;
		}
		set
		{
			_formatArgs = value;
			ResolveString();
			UpdateLabel();
		}
	}

	private void OnEnable()
	{
		ResolveString();
		UpdateLabel();
	}

	public void ResolveString()
	{
		previousToken = _token;
		if (formatArgs.Length == 0)
		{
			resolvedString = Language.GetString(_token);
		}
		else
		{
			resolvedString = Language.GetStringFormatted(_token, formatArgs);
		}
	}

	private void CacheComponents()
	{
		textMeshPro = GetComponent<TMP_Text>();
		if (!textMeshPro)
		{
			textMeshPro = GetComponentInChildren<TMP_Text>();
		}
	}

	private void Awake()
	{
		CacheComponents();
	}

	private void OnValidate()
	{
		CacheComponents();
	}

	private void Start()
	{
	}

	private void UpdateLabel()
	{
		if ((bool)textMeshPro)
		{
			textMeshPro.text = resolvedString;
		}
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		Language.onCurrentLanguageChanged += OnCurrentLanguageChanged;
	}

	private static void OnCurrentLanguageChanged()
	{
		LanguageTextMeshController[] array = UnityEngine.Object.FindObjectsOfType<LanguageTextMeshController>();
		foreach (LanguageTextMeshController obj in array)
		{
			obj.ResolveString();
			obj.UpdateLabel();
		}
	}
}
