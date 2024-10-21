using System;
using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI;

public class CarouselNavigationController : MonoBehaviour
{
	public struct DisplayData : IEquatable<DisplayData>
	{
		public readonly int pageCount;

		public readonly int pageIndex;

		public DisplayData(int pageCount, int pageIndex)
		{
			this.pageCount = pageCount;
			this.pageIndex = pageIndex;
		}

		public bool Equals(DisplayData other)
		{
			if (pageCount == other.pageCount)
			{
				return pageIndex == other.pageIndex;
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			if (obj is DisplayData other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (pageCount * 397) ^ pageIndex;
		}
	}

	public GameObject buttonPrefab;

	public RectTransform container;

	public MPButton leftButton;

	public MPButton rightButton;

	public bool allowLooping;

	public UIElementAllocator<MPButton> buttonAllocator;

	private int currentPageIndex;

	private DisplayData displayData = new DisplayData(0, 0);

	public event Action<int> onPageChangeSubmitted;

	public event Action onUnderflow;

	public event Action onOverflow;

	private void Awake()
	{
		buttonAllocator = new UIElementAllocator<MPButton>(container, buttonPrefab);
	}

	private void Start()
	{
		if ((bool)leftButton)
		{
			leftButton.onClick.AddListener(NavigatePreviousPage);
		}
		if ((bool)rightButton)
		{
			rightButton.onClick.AddListener(NavigateNextPage);
		}
	}

	private void OnEnable()
	{
		Rebuild();
	}

	public void SetDisplayData(DisplayData newDisplayData)
	{
		if (!newDisplayData.Equals(displayData))
		{
			displayData = newDisplayData;
			Rebuild();
		}
	}

	private void Rebuild()
	{
		buttonAllocator.AllocateElements(displayData.pageCount);
		int i = 0;
		for (int count = buttonAllocator.elements.Count; i < count; i++)
		{
			MPButton mPButton = buttonAllocator.elements[i];
			ColorBlock colors = mPButton.colors;
			colors.colorMultiplier = 1f;
			mPButton.colors = colors;
			mPButton.onClick.RemoveAllListeners();
			DisplayData buttonDisplayData = new DisplayData(displayData.pageCount, i);
			mPButton.onClick.AddListener(delegate
			{
				SetDisplayData(buttonDisplayData);
				this.onPageChangeSubmitted?.Invoke(displayData.pageIndex);
			});
		}
		if (displayData.pageIndex >= 0 && displayData.pageIndex < displayData.pageCount)
		{
			MPButton mPButton2 = buttonAllocator.elements[displayData.pageIndex];
			ColorBlock colors2 = mPButton2.colors;
			colors2.colorMultiplier = 2f;
			mPButton2.colors = colors2;
		}
		bool flag = displayData.pageCount > 1;
		bool interactable = flag && (allowLooping || displayData.pageIndex > 0);
		bool interactable2 = flag && (allowLooping || displayData.pageIndex < displayData.pageCount - 1);
		if ((bool)leftButton)
		{
			leftButton.gameObject.SetActive(flag);
			leftButton.interactable = interactable;
		}
		if ((bool)rightButton)
		{
			rightButton.gameObject.SetActive(flag);
			rightButton.interactable = interactable2;
		}
	}

	public void NavigateNextPage()
	{
		if (displayData.pageCount <= 0)
		{
			return;
		}
		int num = displayData.pageIndex + 1;
		bool flag = num >= displayData.pageCount;
		if (flag)
		{
			if (!allowLooping)
			{
				return;
			}
			num = 0;
		}
		Debug.LogFormat("NavigateNextPage() desiredPageIndex={0} overflow={1}", num, flag);
		SetDisplayData(new DisplayData(displayData.pageCount, num));
		this.onPageChangeSubmitted?.Invoke(num);
		if (flag)
		{
			this.onOverflow?.Invoke();
		}
	}

	public void NavigatePreviousPage()
	{
		if (displayData.pageCount <= 0)
		{
			return;
		}
		int num = displayData.pageIndex - 1;
		bool flag = num < 0;
		if (flag)
		{
			if (!allowLooping)
			{
				return;
			}
			num = displayData.pageCount - 1;
		}
		Debug.LogFormat("NavigatePreviousPage() desiredPageIndex={0} underflow={1}", num, flag);
		SetDisplayData(new DisplayData(displayData.pageCount, num));
		this.onPageChangeSubmitted?.Invoke(num);
		if (flag)
		{
			this.onUnderflow?.Invoke();
		}
	}
}
