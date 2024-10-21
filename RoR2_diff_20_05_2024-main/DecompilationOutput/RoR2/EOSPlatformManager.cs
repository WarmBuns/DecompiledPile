using System;
using Epic.OnlineServices;
using Epic.OnlineServices.Logging;
using Epic.OnlineServices.Platform;
using UnityEngine;

namespace RoR2;

public class EOSPlatformManager : PlatformManager
{
	private static PlatformInterface _platformInterface;

	private string _productName = Application.productName;

	private string _productVersion = Application.version;

	private string _productId = "3bf8fc77540f41b5bb253d9563eec679";

	private string _sandboxId = "0dfcaeede0214b80b597f08fd7d64b1b";

	private string _deploymentId = "f2ca672e660a4792a17b124291408566";

	private string _clientId;

	private string _clientSecret;

	private EOSLibraryManager libManager;

	public EOSPlatformManager()
	{
		libManager = new EOSLibraryManager();
		InitializePlatformInterface();
		SetupLogging();
		CreatePlatformInterface();
	}

	public override void InitializePlatformManager()
	{
		RoR2Application.onShutDown = (Action)Delegate.Combine(RoR2Application.onShutDown, new Action(Shutdown));
		RoR2Application.onUpdate += UpdatePlatformManager;
	}

	public static PlatformInterface GetPlatformInterface()
	{
		if (_platformInterface == null)
		{
			throw new Exception("_platformInterface has not been set. Initialize EOSPlatformManager before attempting to access _platformInterface.");
		}
		return _platformInterface;
	}

	private void InitializePlatformInterface()
	{
		InitializeOptions initializeOptions = default(InitializeOptions);
		initializeOptions.ProductName = _productName;
		initializeOptions.ProductVersion = _productVersion;
		InitializeOptions options = initializeOptions;
		Result result = PlatformInterface.Initialize(ref options);
		if (result != 0 && result != Result.AlreadyConfigured)
		{
			throw new Exception("Failed to initialize platform: " + result);
		}
	}

	private void SetupLogging()
	{
		LoggingInterface.SetLogLevel(LogCategory.AllCategories, LogLevel.VeryVerbose);
		LoggingInterface.SetCallback(delegate(ref LogMessage logMessage)
		{
			System.Console.WriteLine(logMessage.Message);
		});
	}

	private void CreatePlatformInterface()
	{
		Options options = default(Options);
		options.ProductId = _productId;
		options.SandboxId = _sandboxId;
		options.DeploymentId = _deploymentId;
		options.ClientCredentials = new ClientCredentials
		{
			ClientId = "xyza7891fuF5aodDdJ1ITnkYeRbyJbnT",
			ClientSecret = "QMwgx1LJIkRt25iA9YjXldcMD/8aAeQTke7a5FVU3no"
		};
		Options options2 = options;
		_platformInterface = PlatformInterface.Create(ref options2);
		if (_platformInterface == null)
		{
			throw new Exception("Failed to create platform");
		}
	}

	private void Shutdown()
	{
		if (_platformInterface != null)
		{
			_platformInterface.Release();
			_platformInterface = null;
			PlatformInterface.Shutdown();
		}
		libManager.Shutdown();
	}

	protected override void UpdatePlatformManager()
	{
		if (_platformInterface != null)
		{
			_platformInterface.Tick();
		}
	}
}
