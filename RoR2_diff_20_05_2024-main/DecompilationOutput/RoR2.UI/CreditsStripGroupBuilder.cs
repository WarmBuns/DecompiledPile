using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace RoR2.UI;

public class CreditsStripGroupBuilder : MonoBehaviour
{
	[Multiline]
	public string source;

	public GameObject stripPrefab;

	public static string EnglishRoleToToken(string englishRoleString)
	{
		StringBuilder stringBuilder = new StringBuilder(englishRoleString);
		stringBuilder.Replace("&", "AND");
		stringBuilder.Replace(",", "");
		stringBuilder.Replace("(", "");
		stringBuilder.Replace(")", "");
		for (int num = stringBuilder.Length - 1; num >= 0; num--)
		{
			if (char.IsWhiteSpace(stringBuilder[num]))
			{
				stringBuilder[num] = '_';
			}
		}
		for (int num2 = stringBuilder.Length - 1; num2 >= 0; num2--)
		{
			char c = stringBuilder[num2];
			if (!char.IsLetterOrDigit(c) && c != '_' && c != '/')
			{
				stringBuilder.Remove(num2, 1);
			}
		}
		stringBuilder.Insert(0, "CREDITS_ROLE_");
		return stringBuilder.ToString().ToUpper(CultureInfo.InvariantCulture);
	}

	public List<(string name, string englishRoleName)> GetNamesAndEnglishRoles()
	{
		string text = source.Replace("\r", "").Replace("\n", "\t");
		List<(string, string)> list = new List<(string, string)>();
		string[] array = text.Split('\t');
		for (int i = 0; i < array.Length; i += 2)
		{
			list.Add((array[i], array[i + 1]));
		}
		return list;
	}

	[ContextMenu("Build")]
	private void Build()
	{
		List<(string, string)> namesAndEnglishRoles = GetNamesAndEnglishRoles();
		List<Transform> list = new List<Transform>(base.transform.childCount);
		int i = 0;
		for (int childCount = base.transform.childCount; i < childCount; i++)
		{
			list.Add(base.transform.GetChild(i));
		}
		for (int num = list.Count - 1; num >= 0; num--)
		{
			Transform transform = list[num];
			if (EditorUtil.PrefabUtilityGetNearestPrefabInstanceRoot(transform.gameObject) != stripPrefab)
			{
				UnityEngine.Object.DestroyImmediate(transform.gameObject);
				list.RemoveAt(num);
			}
		}
		while (list.Count < namesAndEnglishRoles.Count)
		{
			GameObject gameObject = (GameObject)EditorUtil.PrefabUtilityInstantiatePrefab(stripPrefab, base.transform);
			list.Add(gameObject.transform);
		}
		while (list.Count > namesAndEnglishRoles.Count)
		{
			int index = list.Count - 1;
			UnityEngine.Object.DestroyImmediate(list[index].gameObject);
			list.RemoveAt(index);
		}
		int j = 0;
		for (int num2 = Math.Min(namesAndEnglishRoles.Count, list.Count); j < num2; j++)
		{
			(string, string) tuple = namesAndEnglishRoles[j];
			SetStripValues(list[j].gameObject, tuple.Item1, tuple.Item2);
		}
		static void SetStripValues(GameObject stripInstance, string name, string rawRoleText)
		{
			stripInstance.transform.Find("CreditNameLabel").GetComponent<HGTextMeshProUGUI>().SetText(name);
			stripInstance.transform.Find("CreditRoleLabel").GetComponent<LanguageTextMeshController>().token = EnglishRoleToToken(rawRoleText);
			stripInstance.transform.Find("CreditRoleLabel").GetComponent<HGTextMeshProUGUI>().SetText(rawRoleText);
		}
	}
}
