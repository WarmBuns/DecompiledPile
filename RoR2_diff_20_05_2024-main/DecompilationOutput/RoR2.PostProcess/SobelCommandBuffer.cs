using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RoR2.PostProcess;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class SobelCommandBuffer : MonoBehaviour
{
	public CameraEvent cameraEvent = CameraEvent.BeforeLighting;

	private Camera camera;

	private RenderTexture sobelInfoTex;

	private CommandBuffer sobelCommandBuffer;

	private CameraEvent sobelCommandBufferSubscribedEvent;

	private Material sobelBufferMaterial;

	private bool sobelBufferReferencesReady;

	private static Shader sobelShader;

	private static Action callbacksFromStaticMethod;

	private static bool currentlyPreloadingShader;

	[SystemInitializer(new Type[] { })]
	public static void PreloadShaderReference()
	{
		if (currentlyPreloadingShader || sobelShader != null)
		{
			if (sobelShader != null)
			{
				SetShaderReference(sobelShader);
			}
			return;
		}
		if (LegacyShaderAPI.TryToGetPreloadedShader("Hopoo Games/Internal/SobelBuffer", out var cachedShader))
		{
			SetShaderReference(cachedShader);
			return;
		}
		AsyncOperationHandle<Shader> asyncOperationHandle = Addressables.LoadAssetAsync<Shader>("780f79b2a62a0df439dedf59c533eee6");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<Shader> obj)
		{
			SetShaderReference(obj.Result);
		};
		currentlyPreloadingShader = true;
	}

	private static void SetShaderReference(Shader _shader)
	{
		if (_shader != null)
		{
			sobelShader = _shader;
			callbacksFromStaticMethod?.Invoke();
			callbacksFromStaticMethod = null;
			currentlyPreloadingShader = false;
		}
	}

	private void Awake()
	{
		Initialize();
	}

	private void OnDestroy()
	{
		DestroyTemporaryAsset(sobelBufferMaterial);
		sobelBufferReferencesReady = false;
	}

	private void OnEnable()
	{
		if (!Application.isPlaying)
		{
			Initialize();
		}
		camera.AddCommandBuffer(cameraEvent, sobelCommandBuffer);
		sobelCommandBufferSubscribedEvent = cameraEvent;
	}

	private void OnDisable()
	{
		DestroyTemporaryAsset(sobelInfoTex);
		camera.RemoveCommandBuffer(sobelCommandBufferSubscribedEvent, sobelCommandBuffer);
		sobelCommandBuffer.Clear();
	}

	private void OnPreRender()
	{
		Rebuild();
	}

	private void Initialize()
	{
		if (sobelShader == null)
		{
			callbacksFromStaticMethod = (Action)Delegate.Combine(callbacksFromStaticMethod, new Action(Initialize));
			PreloadShaderReference();
			return;
		}
		camera = GetComponent<Camera>();
		sobelBufferMaterial = new Material(sobelShader);
		sobelCommandBuffer = new CommandBuffer();
		sobelCommandBuffer.name = "Sobel Command Buffer";
		sobelBufferReferencesReady = true;
	}

	private void Rebuild()
	{
		if (!sobelBufferReferencesReady)
		{
			Initialize();
			return;
		}
		Vector2Int vector2Int = new Vector2Int(camera.pixelWidth, camera.pixelHeight);
		RenderTexture renderTexture = sobelInfoTex;
		if ((bool)sobelInfoTex && new Vector2Int(sobelInfoTex.width, sobelInfoTex.height) != vector2Int)
		{
			DestroyTemporaryAsset(sobelInfoTex);
			sobelInfoTex = null;
		}
		if (!sobelInfoTex && vector2Int.x > 0 && vector2Int.y > 0)
		{
			sobelInfoTex = new RenderTexture(vector2Int.x, vector2Int.y, 0, GraphicsFormat.R8_UNorm, 0);
			sobelInfoTex.name = "Sobel Outline Information";
		}
		if (renderTexture != sobelInfoTex)
		{
			int nameID = Shader.PropertyToID("_SobelTex");
			RenderTargetIdentifier renderTargetIdentifier = new RenderTargetIdentifier(sobelInfoTex);
			RenderTargetIdentifier source = new RenderTargetIdentifier(BuiltinRenderTextureType.ResolvedDepth);
			RenderTargetIdentifier renderTarget = new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget);
			sobelCommandBuffer.Clear();
			sobelCommandBuffer.Blit(source, renderTargetIdentifier, sobelBufferMaterial);
			sobelCommandBuffer.SetGlobalTexture(nameID, renderTargetIdentifier);
			sobelCommandBuffer.SetRenderTarget(renderTarget);
		}
	}

	private void DestroyTemporaryAsset(UnityEngine.Object temporaryAsset)
	{
		if (Application.isPlaying)
		{
			UnityEngine.Object.Destroy(temporaryAsset);
		}
		else
		{
			UnityEngine.Object.DestroyImmediate(temporaryAsset);
		}
	}
}
