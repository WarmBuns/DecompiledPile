using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RoR2;

[RequireComponent(typeof(Camera))]
[RequireComponent(typeof(SceneCamera))]
public class OutlineHighlight : MonoBehaviour
{
	private enum Passes
	{
		FillPass,
		Blit
	}

	public enum RTResolution
	{
		Quarter = 4,
		Half = 2,
		Full = 1
	}

	public struct HighlightInfo
	{
		public Color color;

		public Renderer renderer;
	}

	public RTResolution m_resolution = RTResolution.Full;

	public readonly Queue<HighlightInfo> highlightQueue = new Queue<HighlightInfo>();

	private Material highlightMaterial;

	private CommandBuffer commandBuffer;

	private int m_RTWidth = 512;

	private int m_RTHeight = 512;

	public Material outlineMaterial;

	public Material outlineMaterial_Skinned;

	public static Action<OutlineHighlight> onPreRenderOutlineHighlight;

	public SceneCamera sceneCamera { get; private set; }

	private void InitOutlineMaterials()
	{
	}

	private void Awake()
	{
		sceneCamera = GetComponent<SceneCamera>();
		CreateBuffers();
		CreateMaterials();
		m_RTWidth = (int)((float)Screen.width / (float)m_resolution);
		m_RTHeight = (int)((float)Screen.height / (float)m_resolution);
		InitOutlineMaterials();
	}

	private void CreateBuffers()
	{
		commandBuffer = new CommandBuffer();
	}

	private void ClearCommandBuffers()
	{
		commandBuffer.Clear();
	}

	private void CreateMaterials()
	{
		highlightMaterial = new Material(LegacyShaderAPI.Find("Hopoo Games/Internal/Outline Highlight"));
	}

	private void RenderHighlights(RenderTexture rt)
	{
		RenderTargetIdentifier renderTarget = new RenderTargetIdentifier(rt);
		commandBuffer.SetRenderTarget(renderTarget);
		foreach (Highlight readonlyHighlight in Highlight.readonlyHighlightList)
		{
			if (readonlyHighlight.isOn && (bool)readonlyHighlight.targetRenderer)
			{
				highlightQueue.Enqueue(new HighlightInfo
				{
					color = readonlyHighlight.GetColor() * readonlyHighlight.strength,
					renderer = readonlyHighlight.targetRenderer
				});
			}
		}
		onPreRenderOutlineHighlight?.Invoke(this);
		while (highlightQueue.Count > 0)
		{
			HighlightInfo highlightInfo = highlightQueue.Dequeue();
			if ((bool)highlightInfo.renderer)
			{
				highlightMaterial.SetColor("_Color", highlightInfo.color);
				commandBuffer.DrawRenderer(highlightInfo.renderer, highlightMaterial, 0, 0);
				RenderTexture.active = rt;
				Graphics.ExecuteCommandBuffer(commandBuffer);
				RenderTexture.active = null;
				ClearCommandBuffers();
			}
		}
	}

	private bool NeedsHighlight()
	{
		foreach (Highlight readonlyHighlight in Highlight.readonlyHighlightList)
		{
			if (readonlyHighlight.isOn && (bool)readonlyHighlight.targetRenderer)
			{
				return true;
			}
		}
		return false;
	}

	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		if (!NeedsHighlight())
		{
			Graphics.Blit(source, destination);
			return;
		}
		int width = (int)((float)destination.width / (float)m_resolution);
		int height = (int)((float)destination.height / (float)m_resolution);
		RenderTexture renderTexture = (RenderTexture.active = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32));
		GL.Clear(clearDepth: true, clearColor: true, Color.clear);
		RenderTexture.active = null;
		ClearCommandBuffers();
		RenderHighlights(renderTexture);
		highlightMaterial.SetTexture("_OutlineMap", renderTexture);
		highlightMaterial.SetColor("_Color", Color.white);
		Graphics.Blit(source, destination, highlightMaterial, 1);
		RenderTexture.ReleaseTemporary(renderTexture);
	}
}
