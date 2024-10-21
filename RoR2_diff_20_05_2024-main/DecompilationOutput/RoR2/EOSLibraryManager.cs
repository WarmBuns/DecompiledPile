using System;
using System.Runtime.InteropServices;

namespace RoR2;

public class EOSLibraryManager
{
	private IntPtr _libraryPointer;

	private bool _initialized;

	[DllImport("Kernel32.dll")]
	private static extern IntPtr LoadLibrary(string lpLibFileName);

	[DllImport("Kernel32.dll")]
	private static extern int FreeLibrary(IntPtr hLibModule);

	[DllImport("Kernel32.dll")]
	private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

	public EOSLibraryManager()
	{
		Initialize();
	}

	private void Initialize()
	{
		if (!_initialized)
		{
			_initialized = true;
		}
	}

	public void Shutdown()
	{
		_initialized = false;
	}
}
