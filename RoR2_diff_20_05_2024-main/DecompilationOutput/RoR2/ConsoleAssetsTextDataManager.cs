using System;
using System.Collections.Generic;
using System.IO;

namespace RoR2;

public class ConsoleAssetsTextDataManager : TextDataManager
{
	public override bool InitializedConfigFiles => true;

	public override bool InitializedLocFiles => true;

	public override string GetConfFile(string fileName, string path)
	{
		MemoryStream configData = PlatformSystems.saveSystem.configData;
		if (configData != null)
		{
			MemoryStream memoryStream = new MemoryStream();
			configData.CopyTo(memoryStream);
			using TextReader textReader = new StreamReader(memoryStream);
			memoryStream.Seek(0L, SeekOrigin.Begin);
			return textReader.ReadToEnd();
		}
		return "";
	}

	public override void GetLocFiles(string folderPath, Action<string[]> callback)
	{
		List<string> list = new List<string>();
		foreach (string item in Directory.EnumerateFiles(folderPath))
		{
			if (string.Compare(System.IO.Path.GetFileName(item), "language.json", StringComparison.OrdinalIgnoreCase) != 0)
			{
				string extension = System.IO.Path.GetExtension(item);
				if (MatchesExtension(extension, ".txt") || MatchesExtension(extension, ".json"))
				{
					list.Add(item);
				}
			}
		}
		callback?.Invoke(list.ConvertAll((string x) => File.ReadAllText(x)).ToArray());
		static bool MatchesExtension(string fileExtension, string testExtension)
		{
			return string.Compare(fileExtension, testExtension, StringComparison.OrdinalIgnoreCase) == 0;
		}
	}
}
