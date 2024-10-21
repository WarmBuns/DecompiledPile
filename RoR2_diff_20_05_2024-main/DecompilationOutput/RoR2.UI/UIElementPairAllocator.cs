using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JetBrains.Annotations;
using UnityEngine;

namespace RoR2.UI;

public class UIElementPairAllocator<T, D> where T : Component where D : new()
{
	public delegate void ElementOperationDelegate(int index, T element);

	public readonly RectTransform containerTransform;

	public readonly GameObject elementPrefab;

	[NotNull]
	private List<T> elementControllerComponentsList;

	[NotNull]
	public readonly ReadOnlyCollection<T> elements;

	[NotNull]
	private List<D> elementControllerDataList;

	[NotNull]
	public readonly ReadOnlyCollection<D> elementsData;

	[CanBeNull]
	public ElementOperationDelegate onCreateElement;

	[CanBeNull]
	public ElementOperationDelegate onDestroyElement;

	public UIElementPairAllocator([NotNull] RectTransform containerTransform, [NotNull] GameObject elementPrefab)
	{
		this.containerTransform = containerTransform;
		this.elementPrefab = elementPrefab;
		elementControllerComponentsList = new List<T>();
		elements = new ReadOnlyCollection<T>(elementControllerComponentsList);
		elementControllerDataList = new List<D>();
		elementsData = new ReadOnlyCollection<D>(elementControllerDataList);
	}

	public void SetData(D val, int index)
	{
		elementControllerDataList[index] = val;
	}

	public void AllocateElements(int desiredCount)
	{
		if (desiredCount < 0)
		{
			throw new ArgumentOutOfRangeException();
		}
		for (int num = elementControllerComponentsList.Count - 1; num >= desiredCount; num--)
		{
			T val = elementControllerComponentsList[num];
			onDestroyElement?.Invoke(num, val);
			UnityEngine.Object.Destroy(val.gameObject);
			elementControllerComponentsList.RemoveAt(num);
			elementControllerDataList.RemoveAt(num);
		}
		for (int i = elementControllerComponentsList.Count; i < desiredCount; i++)
		{
			T component = UnityEngine.Object.Instantiate(elementPrefab, containerTransform).GetComponent<T>();
			elementControllerComponentsList.Add(component);
			component.gameObject.SetActive(value: true);
			elementControllerDataList.Add(new D());
			onCreateElement?.Invoke(i, component);
		}
	}
}
