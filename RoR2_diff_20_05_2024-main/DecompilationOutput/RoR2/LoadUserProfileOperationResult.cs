using System;

namespace RoR2;

public struct LoadUserProfileOperationResult
{
	public string fileName;

	public long fileLength;

	public UserProfile userProfile;

	public Exception exception;

	public string failureContents;
}
