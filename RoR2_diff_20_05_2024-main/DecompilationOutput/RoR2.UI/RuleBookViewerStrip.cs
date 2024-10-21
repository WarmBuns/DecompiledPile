using System.Collections.Generic;
using UnityEngine;

namespace RoR2.UI;

public class RuleBookViewerStrip : MonoBehaviour
{
	public GameObject choicePrefab;

	public GameObject tutorialPrefab;

	public RectTransform choiceContainer;

	public RectTransform.Axis movementAxis = RectTransform.Axis.Vertical;

	public float movementDuration = 0.1f;

	public readonly List<RuleChoiceController> choiceControllers = new List<RuleChoiceController>();

	public int currentDisplayChoiceIndex;

	private float velocity;

	private float currentPosition;

	private RuleChoiceController CreateChoice()
	{
		GameObject obj = Object.Instantiate(choicePrefab, choiceContainer);
		obj.SetActive(value: true);
		RuleChoiceController component = obj.GetComponent<RuleChoiceController>();
		component.strip = this;
		return component;
	}

	private void DestroyChoice(RuleChoiceController choiceController)
	{
		Object.Destroy(choiceController.gameObject);
	}

	public void SetData(List<RuleChoiceDef> newChoices, int choiceIndex)
	{
		AllocateChoices(newChoices.Count);
		int num = currentDisplayChoiceIndex;
		int count = newChoices.Count;
		bool canVote = count > 1;
		for (int i = 0; i < count; i++)
		{
			choiceControllers[i].canVote = canVote;
			choiceControllers[i].SetChoice(newChoices[i]);
			if (newChoices[i].localIndex == choiceIndex)
			{
				bool flag = false;
				if (choiceIndex < newChoices.Count && (bool)PreGameController.instance && PreGameController.GameModeConVar.instance != null)
				{
					num = i;
					flag = newChoices[choiceIndex].difficultyIndex == DifficultyIndex.Easy && RoR2Application.isInSinglePlayer && PreGameController.GameModeConVar.instance.GetString() == "ClassicRun";
				}
				tutorialPrefab.SetActive(flag);
				if (!flag)
				{
					Console.instance.SubmitCmd(null, "enable_tutorial 0");
				}
			}
		}
		currentDisplayChoiceIndex = num;
	}

	private void AllocateChoices(int desiredCount)
	{
		while (choiceControllers.Count > desiredCount)
		{
			int index = choiceControllers.Count - 1;
			DestroyChoice(choiceControllers[index]);
			choiceControllers.RemoveAt(index);
		}
		while (choiceControllers.Count < desiredCount)
		{
			choiceControllers.Add(CreateChoice());
		}
	}

	public void Update()
	{
		if (choiceControllers.Count != 0)
		{
			if (currentDisplayChoiceIndex >= choiceControllers.Count)
			{
				currentDisplayChoiceIndex = choiceControllers.Count - 1;
			}
			Vector3 localPosition = choiceControllers[currentDisplayChoiceIndex].transform.localPosition;
			float target = 0f;
			switch (movementAxis)
			{
			case RectTransform.Axis.Horizontal:
				target = 0f - localPosition.x;
				break;
			case RectTransform.Axis.Vertical:
				target = 0f - localPosition.y;
				break;
			}
			currentPosition = Mathf.SmoothDamp(currentPosition, target, ref velocity, movementDuration);
			UpdatePosition();
		}
	}

	private void OnEnable()
	{
		UpdatePosition();
	}

	private void UpdatePosition()
	{
		Vector3 localPosition = choiceContainer.localPosition;
		switch (movementAxis)
		{
		case RectTransform.Axis.Horizontal:
			localPosition.x = currentPosition;
			break;
		case RectTransform.Axis.Vertical:
			localPosition.y = currentPosition;
			break;
		}
		if ((bool)choiceContainer && localPosition.x != float.NaN && localPosition.y != float.NaN && localPosition.z != float.NaN)
		{
			choiceContainer.localPosition = localPosition;
		}
	}
}
