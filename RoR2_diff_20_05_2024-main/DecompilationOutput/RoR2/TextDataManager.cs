using System;

namespace RoR2;

public abstract class TextDataManager
{
	public abstract bool InitializedConfigFiles { get; }

	public abstract bool InitializedLocFiles { get; }

	public abstract string GetConfFile(string fileName, string path);

	public abstract void GetLocFiles(string folderPath, Action<string[]> callback);
}
