using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JetBrains.Annotations;
using UnityEngine;

namespace RoR2.UI;

public class UIElementAllocator<T> where T : Component
{
	public delegate void ElementOperationDelegate(int index, T element);

	public readonly RectTransform containerTransform;

	public readonly GameObject elementPrefab;

	public readonly bool markElementsUnsavable;

	[NotNull]
	private List<T> elementControllerComponentsList;

	[NotNull]
	public readonly ReadOnlyCollection<T> elements;

	[CanBeNull]
	public ElementOperationDelegate onCreateElement;

	[CanBeNull]
	public ElementOperationDelegate onDestroyElement;

	public UIElementAllocator([NotNull] RectTransform containerTransform, [NotNull] GameObject elementPrefab, bool markElementsUnsavable = true, bool acquireExistingChildren = false)
	{
		this.containerTransform = containerTransform;
		this.elementPrefab = elementPrefab;
		this.markElementsUnsavable = markElementsUnsavable;
		elementControllerComponentsList = new List<T>();
		elements = new ReadOnlyCollection<T>(elementControllerComponentsList);
		if (!acquireExistingChildren)
		{
			return;
		}
		for (int i = 0; i < containerTransform.childCount; i++)
		{
			T component = containerTransform.GetChild(i).GetComponent<T>();
			if ((bool)component && component.gameObject.activeInHierarchy)
			{
				elementControllerComponentsList.Add(component);
			}
		}
	}

	private void DestroyElementAt(int i)
	{
		T val = elementControllerComponentsList[i];
		onDestroyElement?.Invoke(i, val);
		GameObject gameObject = val.gameObject;
		if (Application.isPlaying)
		{
			UnityEngine.Object.Destroy(gameObject);
		}
		else
		{
			UnityEngine.Object.DestroyImmediate(gameObject);
		}
		elementControllerComponentsList.RemoveAt(i);
	}

	public void AllocateElements(int desiredCount)
	{
		if (desiredCount < 0)
		{
			throw new ArgumentOutOfRangeException();
		}
		if (!containerTransform.gameObject.scene.IsValid())
		{
			return;
		}
		for (int num = elementControllerComponentsList.Count - 1; num >= desiredCount; num--)
		{
			DestroyElementAt(num);
		}
		for (int i = elementControllerComponentsList.Count; i < desiredCount; i++)
		{
			T component = UnityEngine.Object.Instantiate(elementPrefab, containerTransform).GetComponent<T>();
			elementControllerComponentsList.Add(component);
			GameObject gameObject = component.gameObject;
			if (markElementsUnsavable)
			{
				gameObject.hideFlags |= HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
			}
			gameObject.SetActive(value: true);
			onCreateElement?.Invoke(i, component);
		}
	}

	public void MoveElementsToContainerEnd()
	{
		int num = containerTransform.childCount - elementControllerComponentsList.Count;
		for (int num2 = elementControllerComponentsList.Count - 1; num2 >= 0; num2--)
		{
			elementControllerComponentsList[num2].transform.SetSiblingIndex(num + num2);
		}
	}
}
