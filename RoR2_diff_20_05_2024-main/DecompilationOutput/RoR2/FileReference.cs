using System;
using System.Collections.Generic;
using Zio;

namespace RoR2;

public struct FileReference : IEquatable<FileReference>
{
	public IFileSystem fileSystem;

	public UPath path;

	public bool Equals(FileReference other)
	{
		if (fileSystem == other.fileSystem || fileSystem.Equals(other.fileSystem))
		{
			return path.Equals(other.path);
		}
		return false;
	}

	public override bool Equals(object other)
	{
		if (other is FileReference)
		{
			return Equals((FileReference)other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (-990633296 * -1521134295 + EqualityComparer<IFileSystem>.Default.GetHashCode(fileSystem)) * -1521134295 + EqualityComparer<UPath>.Default.GetHashCode(path);
	}
}
