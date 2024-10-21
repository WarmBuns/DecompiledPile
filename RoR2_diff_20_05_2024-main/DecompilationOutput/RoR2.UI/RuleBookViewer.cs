using UnityEngine;

namespace RoR2.UI;

public class RuleBookViewer : MonoBehaviour
{
	[Tooltip("The prefab to use for categories.")]
	public GameObject categoryPrefab;

	public RectTransform categoryContainer;

	private UIElementAllocator<RuleCategoryController> categoryElementAllocator;

	private RuleChoiceMask cachedRuleChoiceMask;

	private RuleBook cachedRuleBook;

	private void Awake()
	{
		cachedRuleBook = new RuleBook();
		cachedRuleChoiceMask = new RuleChoiceMask();
	}

	private void Start()
	{
		categoryElementAllocator = new UIElementAllocator<RuleCategoryController>(categoryContainer, categoryPrefab);
		AllocateCategories(RuleCatalog.categoryCount);
	}

	private void Update()
	{
		if ((bool)PreGameController.instance)
		{
			SetData(PreGameController.instance.resolvedRuleChoiceMask, PreGameController.instance.readOnlyRuleBook);
		}
	}

	private void AllocateCategories(int desiredCount)
	{
		categoryElementAllocator.AllocateElements(desiredCount);
	}

	private void SetData(RuleChoiceMask choiceAvailability, RuleBook ruleBook)
	{
		if (!choiceAvailability.Equals(cachedRuleChoiceMask) || !ruleBook.Equals(cachedRuleBook))
		{
			cachedRuleChoiceMask.Copy(choiceAvailability);
			cachedRuleBook.Copy(ruleBook);
			for (int i = 0; i < RuleCatalog.categoryCount; i++)
			{
				RuleCategoryController ruleCategoryController = categoryElementAllocator.elements[i];
				ruleCategoryController.SetData(RuleCatalog.GetCategoryDef(i), cachedRuleChoiceMask, cachedRuleBook);
				ruleCategoryController.gameObject.SetActive(!ruleCategoryController.shouldHide);
			}
		}
	}
}
