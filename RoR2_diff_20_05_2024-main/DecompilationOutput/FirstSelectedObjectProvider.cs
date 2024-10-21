using System.Collections.Generic;
using RoR2.UI;
using UnityEngine;
using UnityEngine.UI;

public class FirstSelectedObjectProvider : MonoBehaviour
{
	public GameObject firstSelectedObject;

	public GameObject[] fallBackFirstSelectedObjects;

	private GameObject lastSelected;

	public GameObject[] enforceCurrentSelectionIsInList;

	public bool takeAbsolutePriority;

	public static FirstSelectedObjectProvider priorityHolder;

	private void OnEnable()
	{
		if (takeAbsolutePriority)
		{
			priorityHolder = this;
		}
	}

	private void OnDisable()
	{
		if (takeAbsolutePriority && priorityHolder == this)
		{
			priorityHolder = null;
		}
	}

	private GameObject getInteractableFirstSelectedObject()
	{
		if ((bool)firstSelectedObject && firstSelectedObject.GetComponent<Selectable>().interactable && firstSelectedObject.activeInHierarchy)
		{
			return firstSelectedObject;
		}
		if (fallBackFirstSelectedObjects == null)
		{
			return null;
		}
		for (int i = 0; i < fallBackFirstSelectedObjects.Length; i++)
		{
			if (fallBackFirstSelectedObjects[i].GetComponent<Selectable>().interactable && fallBackFirstSelectedObjects[i].activeInHierarchy)
			{
				return fallBackFirstSelectedObjects[i];
			}
		}
		return null;
	}

	public void EnsureSelectedObject()
	{
		if (priorityHolder != null && priorityHolder != this)
		{
			return;
		}
		GameObject interactableFirstSelectedObject = getInteractableFirstSelectedObject();
		if (!(interactableFirstSelectedObject != null))
		{
			return;
		}
		MPEventSystemLocator component = interactableFirstSelectedObject.GetComponent<MPEventSystemLocator>();
		if (!component)
		{
			return;
		}
		MPEventSystem eventSystem = component.eventSystem;
		if (!eventSystem)
		{
			return;
		}
		GameObject currentSelectedGameObject = eventSystem.currentSelectedGameObject;
		if (currentSelectedGameObject == null)
		{
			if (lastSelected != null && lastSelected.GetComponent<Selectable>().interactable)
			{
				eventSystem.SetSelectedGameObject(lastSelected);
			}
			else
			{
				if (eventSystem.firstSelectedGameObject == null)
				{
					eventSystem.firstSelectedGameObject = interactableFirstSelectedObject;
				}
				eventSystem.SetSelectedGameObject(interactableFirstSelectedObject);
			}
		}
		else
		{
			Selectable component2 = currentSelectedGameObject.GetComponent<Selectable>();
			bool flag = true;
			if (enforceCurrentSelectionIsInList != null && enforceCurrentSelectionIsInList.Length != 0)
			{
				flag = false;
				GameObject[] array = enforceCurrentSelectionIsInList;
				for (int i = 0; i < array.Length; i++)
				{
					if (array[i] == currentSelectedGameObject)
					{
						flag = true;
					}
				}
			}
			if ((bool)component2 && (!component2.interactable || !component2.isActiveAndEnabled))
			{
				flag = false;
			}
			if (!flag && (bool)component2 && (!component2.interactable || !component2.isActiveAndEnabled))
			{
				eventSystem.SetSelectedGameObject(interactableFirstSelectedObject);
			}
		}
		if (component.eventSystem.currentSelectedGameObject != null)
		{
			lastSelected = component.eventSystem.currentSelectedGameObject;
		}
	}

	public void ForceSelectFirstInteractable()
	{
		GameObject interactableFirstSelectedObject = getInteractableFirstSelectedObject();
		if (!(interactableFirstSelectedObject != null))
		{
			return;
		}
		MPEventSystemLocator component = interactableFirstSelectedObject.GetComponent<MPEventSystemLocator>();
		if (!component)
		{
			return;
		}
		MPEventSystem eventSystem = component.eventSystem;
		if ((bool)eventSystem)
		{
			if (eventSystem.firstSelectedGameObject == null)
			{
				eventSystem.firstSelectedGameObject = interactableFirstSelectedObject;
			}
			eventSystem.SetSelectedGameObject(interactableFirstSelectedObject);
		}
		if (component.eventSystem.currentSelectedGameObject != null)
		{
			lastSelected = component.eventSystem.currentSelectedGameObject;
		}
	}

	public void ResetLastSelected()
	{
		lastSelected = null;
	}

	public void AddObject(GameObject obj, bool enforceCurrentSelection = false)
	{
		if (firstSelectedObject == null)
		{
			firstSelectedObject = obj;
		}
		else if (fallBackFirstSelectedObjects == null)
		{
			fallBackFirstSelectedObjects = new GameObject[1];
			fallBackFirstSelectedObjects[0] = obj;
		}
		else if (fallBackFirstSelectedObjects.Length >= 0)
		{
			List<GameObject> list = new List<GameObject>(fallBackFirstSelectedObjects);
			list.Add(obj);
			fallBackFirstSelectedObjects = list.ToArray();
		}
		if (enforceCurrentSelection)
		{
			if (enforceCurrentSelectionIsInList == null)
			{
				enforceCurrentSelectionIsInList = new GameObject[1];
				enforceCurrentSelectionIsInList[0] = obj;
			}
			else if (enforceCurrentSelectionIsInList.Length >= 0)
			{
				List<GameObject> list2 = new List<GameObject>(enforceCurrentSelectionIsInList);
				list2.Add(obj);
				enforceCurrentSelectionIsInList = list2.ToArray();
			}
		}
	}
}
