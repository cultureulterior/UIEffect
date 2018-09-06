using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System;

namespace Coffee.UIExtensions
{
	public interface IParameterTexture
	{
		int parameterIndex { get; set; }

		ParameterTexture ptex { get; }

		Material ptexMaterial { get; }
	}

	/// <summary>
	/// Parameter texture.
	/// </summary>
	[System.Serializable]
	public class ParameterTexture
	{

		//################################
		// Public Members.
		//################################

		/// <summary>
		/// Initializes a new instance of the <see cref="Coffee.UIExtensions.ParameterTexture"/> class.
		/// </summary>
		/// <param name="channels">Channels.</param>
		/// <param name="instanceLimit">Instance limit.</param>
		/// <param name="propertyName">Property name.</param>
		public ParameterTexture(int channels, int instanceLimit, string propertyName)
		{
			_propertyName = propertyName;
			_channels = ((channels - 1) / 4 + 1) * 4;
			_instanceLimit = ((instanceLimit - 1) / 2 + 1) * 2;
			_data = new byte[_channels * _instanceLimit];

			_stack = new Stack<int>(_instanceLimit);
			for (int i = 1; i < _instanceLimit + 1; i++)
			{
				_stack.Push(i);
			}
			_matrix.SetTRS(Vector3.zero, Quaternion.identity, new Vector3(1, 1.0f / _instanceLimit));
		}


		/// <summary>
		/// Register the specified target.
		/// </summary>
		/// <param name="target">Target.</param>
		public void Register(IParameterTexture target)
		{
			if (!_material && target.ptexMaterial)
			{
				_material = target.ptexMaterial;
			}
			Initialize();
			if (target.parameterIndex <= 0 && 0 < _stack.Count)
			{
				target.parameterIndex = _stack.Pop();
//				Debug.LogFormat("<color=green>@@@ Register {0} : {1}</color>", target, target.parameterIndex);
			}
		}

		/// <summary>
		/// Unregister the specified target.
		/// </summary>
		/// <param name="target">Target.</param>
		public void Unregister(IParameterTexture target)
		{
			if (0 < target.parameterIndex)
			{
//				Debug.LogFormat("<color=red>@@@ Unregister {0} : {1}</color>", target, target.parameterIndex);
				_stack.Push(target.parameterIndex);
				target.parameterIndex = 0;
			}
		}

		/// <summary>
		/// Sets the data.
		/// </summary>
		/// <param name="target">Target.</param>
		/// <param name="channelId">Channel identifier.</param>
		/// <param name="value">Value.</param>
		public void SetData(IParameterTexture target, int channelId, byte value)
		{
			int index = (target.parameterIndex - 1) * _channels + channelId;
			if (0 < target.parameterIndex && _data[index] != value)
			{
				_data[index] = value;
//				_needUpload = true;
				_updateIndices.Add((target.parameterIndex - 1) * (_channels / 4) + (channelId / 4));
			}
		}

		/// <summary>
		/// Sets the data.
		/// </summary>
		/// <param name="target">Target.</param>
		/// <param name="channelId">Channel identifier.</param>
		/// <param name="value">Value.</param>
		public void SetData(IParameterTexture target, int channelId, float value)
		{
			SetData(target, channelId, (byte)(Mathf.Clamp01(value) * 255));
		}

		/// <summary>
		/// Registers the material.
		/// </summary>
		/// <param name="mat">Mat.</param>
		public void RegisterMaterial(Material mat)
		{
			if (_propertyId == 0)
			{
				_propertyId = Shader.PropertyToID(_propertyName);
			}
			if (mat)
			{
//				mat.SetTexture(_propertyId, _texture);
				mat.SetTexture(_propertyId, _rt);
			}
		}

		/// <summary>
		/// Gets the index of the normalized.
		/// </summary>
		/// <returns>The normalized index.</returns>
		/// <param name="target">Target.</param>
		public float GetNormalizedIndex(IParameterTexture target)
		{
			return ((float)target.parameterIndex - 0.5f) / _instanceLimit;
		}


		//################################
		// Private Members.
		//################################

		Texture2D _texture;
		public RenderTexture _rt;
//		bool _needUpload;
		int _propertyId;
		readonly string _propertyName;
		readonly int _channels;
		readonly int _instanceLimit;
		readonly byte[] _data;
		readonly Stack<int> _stack;
		Mesh _mesh;
		HashSet<int> _updateIndices = new HashSet<int>();
		static readonly List<Vector3> _positions = new List<Vector3>();
		static readonly List<Color32> _colors = new List<Color32>();
		static readonly List<int> _indices = new List<int>();
		static Material _material;
		Matrix4x4 _matrix = default(Matrix4x4);
		CommandBuffer _cb;
		// = new CommandBuffer();
		static List<Action> updates;

		/// <summary>
		/// Initialize this instance.
		/// </summary>
		void Initialize()
		{
#if UNITY_EDITOR
			if (!UnityEditor.EditorApplication.isPlaying && UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
			{
				return;
			}
#endif
			if (updates == null)
			{
				updates = new List<Action>();
				Canvas.willRenderCanvases += () =>
				{
					var count = updates.Count;
					for (int i = 0; i < count; i++)
					{
						updates[i].Invoke();
					}
				};
			}

//			if (!_texture)
//			{
//				_texture = new Texture2D(_channels / 4, _instanceLimit, TextureFormat.RGBA32, false, false);
//				_texture.filterMode = FilterMode.Point;
//				_texture.wrapMode = TextureWrapMode.Clamp;
//
//				// Update dispatcher
//				Canvas.willRenderCanvases += () =>
//				{
//					if (_needUpload && _texture)
//					{
//						_needUpload = false;
//						_texture.LoadRawTextureData(_data);
//						_texture.Apply(false, false);
//					}
//				};
//			}

			if (!_rt)
			{
				var width = _channels / 4;
				var height = _instanceLimit;
				_rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
				_rt.filterMode = FilterMode.Point;
				_rt.useMipMap = false;
				_rt.wrapMode = TextureWrapMode.Clamp;
				_rt.hideFlags = HideFlags.HideAndDontSave;
				_rt.autoGenerateMips = false;
				_rt.useMipMap = false;
				_rt.anisoLevel = 0;

				updates.Add(UpdateParameterTexture);
			}
		}

		void UpdateParameterTexture()
		{
			if (_updateIndices.Count == 0)
			{
				return;
			}

			if (!_mesh)
			{
				_mesh = new Mesh();
				_mesh.MarkDynamic();

				if (_cb != null)
				{
					_cb.Dispose();
					_cb = null;
				}

				_cb = new CommandBuffer();
				_cb.SetRenderTarget(new RenderTargetIdentifier(_rt));
				_cb.DrawMesh(_mesh, _matrix, _material);
				//						_cb.DrawMesh(_mesh, _matrix, null);
			}

			_mesh.Clear();

			_positions.Clear();
			_colors.Clear();
			_indices.Clear();

			var width = _channels / 4;
			var height = _instanceLimit;
			foreach (int index in _updateIndices)
			{
				var x = index % width;
				var y = index / width;

				float yMin = (y + 0) / (float)height * 2 - 1;
				float yMax = (y + 1) / (float)height * 2 - 1;
				float xMin = (x + 0) / (float)width * 2 - 1 + 0.5f / width;
				float xMax = (x + 1) / (float)width * 2 - 1 + 0.5f / width;
				_positions.Add(new Vector3(xMin, yMin));
				_positions.Add(new Vector3(xMin, yMax));
				_positions.Add(new Vector3(xMax, yMax));

				_indices.Add(_indices.Count);
				_indices.Add(_indices.Count);
				_indices.Add(_indices.Count);

				var color = new Color32(_data[index * 4 + 0], _data[index * 4 + 1], _data[index * 4 + 2], _data[index * 4 + 3]);

				_colors.Add(color);
				_colors.Add(color);
				_colors.Add(color);
			}

			_mesh.SetVertices(_positions);
			_mesh.SetColors(_colors);
			_mesh.SetTriangles(_indices, 0);
			//_mesh.UploadMeshData(false);

			_updateIndices.Clear();

			Graphics.ExecuteCommandBuffer(_cb);
		}
	}
}