using System;

namespace RoR2;

internal class TagManager
{
	public Action<string> onTagsStringUpdated;

	public string tagsString { get; protected set; } = string.Empty;
}
