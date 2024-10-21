using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RoR2.RemoteGameBrowser;

public abstract class BaseAsyncRemoteGameProvider : IRemoteGameProvider, IDisposable
{
	public struct SearchFilters : IEquatable<SearchFilters>
	{
		public bool allowMismatchedMods;

		public bool Equals(SearchFilters other)
		{
			return allowMismatchedMods == other.allowMismatchedMods;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			if (obj is SearchFilters other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return allowMismatchedMods.GetHashCode();
		}
	}

	protected struct TaskInfo
	{
		public Task task;

		public CancellationTokenSource cancellationTokenSource;

		public int taskNumber;

		public void Cancel()
		{
			cancellationTokenSource.Cancel();
		}
	}

	private RemoteGameInfo[] gameInfos = Array.Empty<RemoteGameInfo>();

	private readonly object gameInfosLock = new object();

	private readonly object isDirtyLock = new object();

	private readonly object disposedLock = new object();

	private readonly List<TaskInfo> activeTasks = new List<TaskInfo>();

	private int taskNumberProvider;

	protected int maxTasks = 2;

	private bool isDirty;

	public bool refreshOnFiltersChanged = true;

	protected SearchFilters searchFilters = new SearchFilters
	{
		allowMismatchedMods = false
	};

	protected bool disposed { get; private set; }

	public event Action onNewInfoAvailable;

	public SearchFilters GetSearchFilters()
	{
		return searchFilters;
	}

	public void SetSearchFilters(SearchFilters newSearchFilters)
	{
		if (!searchFilters.Equals(newSearchFilters))
		{
			searchFilters = newSearchFilters;
			if (refreshOnFiltersChanged)
			{
				RequestRefresh();
			}
			SetDirty();
		}
	}

	protected void SetDirty()
	{
		lock (disposedLock)
		{
			if (disposed)
			{
				return;
			}
			lock (isDirtyLock)
			{
				if (!isDirty)
				{
					isDirty = true;
					RoR2Application.onNextUpdate += DirtyUpdate;
				}
			}
		}
	}

	protected void DirtyUpdate()
	{
		lock (disposedLock)
		{
			if (disposed)
			{
				return;
			}
			lock (activeTasks)
			{
				if (activeTasks.Count >= maxTasks)
				{
					return;
				}
				lock (isDirtyLock)
				{
					isDirty = false;
					GenerateNewTask();
				}
			}
		}
	}

	private void GenerateNewTask()
	{
		CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
		CancellationToken cancellationToken = cancellationTokenSource.Token;
		Task<RemoteGameInfo[]> innerTask = CreateTask(cancellationToken);
		lock (activeTasks)
		{
			Task task = Task.Run(async delegate
			{
				try
				{
					innerTask.Start();
					RemoteGameInfo[] array = await innerTask;
					lock (gameInfosLock)
					{
						cancellationToken.ThrowIfCancellationRequested();
						gameInfos = array;
						lock (activeTasks)
						{
							for (int i = 0; i < activeTasks.Count; i++)
							{
								TaskInfo taskInfo = activeTasks[i];
								if (taskInfo.cancellationTokenSource == cancellationTokenSource)
								{
									OnTaskComplete(taskInfo);
									break;
								}
							}
						}
					}
				}
				finally
				{
					lock (activeTasks)
					{
						for (int num = activeTasks.Count - 1; num >= 0; num--)
						{
							if (activeTasks[num].cancellationTokenSource == cancellationTokenSource)
							{
								activeTasks.RemoveAt(num);
								break;
							}
						}
					}
				}
			}, cancellationToken);
			activeTasks.Add(new TaskInfo
			{
				task = task,
				cancellationTokenSource = cancellationTokenSource,
				taskNumber = taskNumberProvider++
			});
		}
	}

	protected abstract Task<RemoteGameInfo[]> CreateTask(CancellationToken cancellationToken);

	protected void OnTaskComplete(TaskInfo taskInfo)
	{
		lock (activeTasks)
		{
			for (int num = activeTasks.Count - 1; num >= 0; num--)
			{
				if (activeTasks[num].taskNumber < taskInfo.taskNumber)
				{
					activeTasks[num].Cancel();
				}
			}
		}
		this.onNewInfoAvailable?.Invoke();
	}

	public virtual void Dispose()
	{
		lock (disposedLock)
		{
			disposed = true;
			lock (activeTasks)
			{
				foreach (TaskInfo activeTask in activeTasks)
				{
					activeTask.Cancel();
				}
			}
		}
	}

	public abstract bool RequestRefresh();

	public RemoteGameInfo[] GetKnownGames()
	{
		return gameInfos;
	}

	public virtual bool IsBusy()
	{
		return activeTasks.Count > 0;
	}
}
