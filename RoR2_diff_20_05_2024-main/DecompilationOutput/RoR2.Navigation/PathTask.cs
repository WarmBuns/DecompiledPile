namespace RoR2.Navigation;

public class PathTask
{
	public enum TaskStatus
	{
		NotStarted,
		Running,
		Complete
	}

	public TaskStatus status;

	public bool wasReachable;

	public Path path { get; private set; }

	public PathTask(Path path)
	{
		this.path = path;
	}

	public void Wait()
	{
	}
}
