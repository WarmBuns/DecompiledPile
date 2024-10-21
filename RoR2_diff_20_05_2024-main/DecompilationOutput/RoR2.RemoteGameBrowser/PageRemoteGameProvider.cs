using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;

namespace RoR2.RemoteGameBrowser;

public class PageRemoteGameProvider : BaseAsyncRemoteGameProvider
{
	private int gamesPerPage = 1;

	private int pageIndex;

	private int maxPages;

	private readonly IRemoteGameProvider source;

	public PageRemoteGameProvider([NotNull] IRemoteGameProvider source)
	{
		this.source = source;
		source.onNewInfoAvailable += OnSourceNewInfoAvailable;
		maxTasks = 1;
	}

	public override void Dispose()
	{
		source.onNewInfoAvailable -= OnSourceNewInfoAvailable;
		base.Dispose();
	}

	private void OnSourceNewInfoAvailable()
	{
		SetDirty();
	}

	public void SetGamesPerPage(int newGamesPerPage)
	{
		if (newGamesPerPage < 1)
		{
			newGamesPerPage = 1;
		}
		if (newGamesPerPage != gamesPerPage)
		{
			gamesPerPage = newGamesPerPage;
			SetDirty();
		}
	}

	public bool CanGoToNextPage()
	{
		return pageIndex + 1 < maxPages;
	}

	public bool CanGoToPreviousPage()
	{
		return pageIndex - 1 >= 0;
	}

	public bool GoToNextPage()
	{
		lock (this)
		{
			if (!CanGoToNextPage())
			{
				return false;
			}
			pageIndex++;
			SetDirty();
			return true;
		}
	}

	public bool GoToPreviousPage()
	{
		lock (this)
		{
			if (!CanGoToPreviousPage())
			{
				return false;
			}
			pageIndex--;
			SetDirty();
			return true;
		}
	}

	public void GetCurrentPageInfo(out int pageIndex, out int maxPages)
	{
		lock (this)
		{
			pageIndex = this.pageIndex;
			maxPages = this.maxPages;
		}
	}

	protected override Task<RemoteGameInfo[]> CreateTask(CancellationToken cancellationToken)
	{
		return new Task<RemoteGameInfo[]>(delegate
		{
			RemoteGameInfo[] knownGames = source.GetKnownGames();
			lock (this)
			{
				maxPages = (knownGames.Length + gamesPerPage - 1) / gamesPerPage;
				pageIndex = Math.Max(Math.Min(pageIndex, maxPages - 1), 0);
				int num = Math.Min(gamesPerPage * pageIndex, knownGames.Length);
				int num2 = Mathf.Min(num + gamesPerPage, knownGames.Length);
				if (num2 == num)
				{
					return Array.Empty<RemoteGameInfo>();
				}
				RemoteGameInfo[] array = new RemoteGameInfo[num2 - num];
				int i = num;
				int num3 = 0;
				for (; i < num2; i++)
				{
					array[num3++] = knownGames[i];
				}
				return array;
			}
		});
	}

	public override bool RequestRefresh()
	{
		return source.RequestRefresh();
	}

	public override bool IsBusy()
	{
		if (!base.IsBusy())
		{
			return source?.IsBusy() ?? false;
		}
		return true;
	}
}
