using UnityEngine;
using UnityEngine.UI;
using System;

namespace Coffee.UIExtensions
{
	/// <summary>
	/// Transition effect.
	/// </summary>
	public class UITransitionEffect : UIEffectBase
	{
		//################################
		// Constant or Static Members.
		//################################
		static readonly ParameterTexture _ptex = new ParameterTexture(8, 128, "_ParamTex");

		/// <summary>
		/// Effect mode.
		/// </summary>
		public enum EffectMode
		{
			Fade = 1,
			Cutoff = 2,
			Dissolve = 3,
		}


		//################################
		// Serialize Members.
		//################################
		[SerializeField] EffectMode m_EffectMode = EffectMode.Cutoff;
		[SerializeField][Range(0, 1)] float m_EffectFactor = 1;
		[SerializeField] Texture m_TransitionTexture;
		[SerializeField] EffectArea m_EffectArea;
		[SerializeField] bool m_KeepAspectRatio;
		[SerializeField] [Range(0, 1)] float m_DissolveWidth = 0.5f;
		[SerializeField] [Range(0, 1)] float m_DissolveSoftness = 0.5f;
		[SerializeField] [ColorUsage(false)] Color m_DissolveColor = new Color(0.0f, 0.25f, 1.0f);


		//################################
		// Public Members.
		//################################
		/// <summary>
		/// Effect factor between 0(no effect) and 1(complete effect).
		/// </summary>
		public float effectFactor
		{
			get { return m_EffectFactor; }
			set
			{
				value = Mathf.Clamp(value, 0, 1);
				if (!Mathf.Approximately(m_EffectFactor, value))
				{
					m_EffectFactor = value;
					SetDirty();
				}
			}
		}

		/// <summary>
		/// Transition texture.
		/// </summary>
		public Texture transitionTexture
		{
			get { return m_TransitionTexture ?? (m_EffectMaterial ? m_EffectMaterial.GetTexture("_TransitionTexture") : (Texture)null); }
			set
			{
				if (m_TransitionTexture != value)
				{
					m_TransitionTexture = value;
					if (graphic)
					{
						ModifyMaterial(effectHash);
					}
				}
			}
		}

		/// <summary>
		/// Effect mode.
		/// </summary>
		public EffectMode effectMode { get { return m_EffectMode; } }

		/// <summary>
		/// Keep aspect ratio.
		/// </summary>
		public bool keepAspectRatio
		{
			get { return m_KeepAspectRatio; }
			set
			{
				if (m_KeepAspectRatio != value)
				{
					m_KeepAspectRatio = value;
					targetGraphic.SetVerticesDirty();
				}
			}
		}

		/// Gets the parameter texture.
		/// </summary>
		public override ParameterTexture ptex { get { return _ptex; } }

		/// <summary>
		/// Gets hash for effect.
		/// </summary>
		protected override ulong effectHash { get { return ((ulong)4 << 0) + ((ulong)m_EffectMode << 4) + (m_TransitionTexture ? (uint)m_TransitionTexture.GetInstanceID() << 8 : 0); } }

		/// <summary>
		/// Dissolve edge width.
		/// </summary>
		public float dissolveWidth
		{
			get { return m_DissolveWidth; }
			set
			{
				value = Mathf.Clamp(value, 0, 1);
				if (!Mathf.Approximately(m_DissolveWidth, value))
				{
					m_DissolveWidth = value;
					SetDirty();
				}
			}
		}

		/// <summary>
		/// Dissolve edge softness.
		/// </summary>
		public float dissolveSoftness
		{
			get { return m_DissolveSoftness; }
			set
			{
				value = Mathf.Clamp(value, 0, 1);
				if (!Mathf.Approximately(m_DissolveSoftness, value))
				{
					m_DissolveSoftness = value;
					SetDirty();
				}
			}
		}

		/// <summary>
		/// Dissolve edge color.
		/// </summary>
		public Color dissolveColor
		{
			get { return m_DissolveColor; }
			set
			{
				if (m_DissolveColor != value)
				{
					m_DissolveColor = value;
					SetDirty();
				}
			}
		}

		/// <summary>
		/// Modifies the material.
		/// </summary>
		public override void ModifyMaterial(ulong hash, Func<Material> onCreate = null)
		{
			base.ModifyMaterial(hash, () =>
				{
					var mat = new Material(m_EffectMaterial);
					mat.name += "_" + hash;
					mat.EnableKeyword(m_EffectMode.ToString().ToUpper());

					if(m_TransitionTexture)
					{
						mat.SetTexture("_TransitionTexture", m_TransitionTexture);
					}

					ptex.RegisterMaterial(mat);
					return mat;
				});
		}

		/// <summary>
		/// Modifies the mesh.
		/// </summary>
		public override void ModifyMesh(VertexHelper vh)
		{
			if (!isActiveAndEnabled)
			{
				return;
			}

			// rect.
			var tex = transitionTexture;
			var aspectRatio = m_KeepAspectRatio && tex ? ((float)tex.width) / tex.height : -1;
			Rect rect = m_EffectArea.GetEffectArea(vh, graphic, aspectRatio);

			// Set prameters to vertex.
			float normalizedIndex = ptex.GetNormalizedIndex(this);
			UIVertex vertex = default(UIVertex);
			bool effectEachCharacter = graphic is Text && m_EffectArea == EffectArea.Character;
			float x, y;
			int count = vh.currentVertCount;
			for (int i = 0; i < count; i++)
			{
				vh.PopulateUIVertex(ref vertex, i);

				if (effectEachCharacter)
				{
					x = splitedCharacterPosition[i % 4].x;
					y = splitedCharacterPosition[i % 4].y;
				}
				else
				{
					x = Mathf.Clamp01(vertex.position.x / rect.width + 0.5f);
					y = Mathf.Clamp01(vertex.position.y / rect.height + 0.5f);
				}

				vertex.uv0 = new Vector2(
					Packer.ToFloat(vertex.uv0.x, vertex.uv0.y),
					Packer.ToFloat(x, y, normalizedIndex)
				);
				vh.SetUIVertex(vertex, i);
			}
		}

		//################################
		// Protected Members.
		//################################

		protected override void SetDirty()
		{
			ptex.SetData(this, 0, m_EffectFactor);	// param1.x : effect factor
			if (m_EffectMode == EffectMode.Dissolve)
			{
				ptex.SetData(this, 1, m_DissolveWidth);		// param1.y : width
				ptex.SetData(this, 2, m_DissolveSoftness);	// param1.z : softness
				ptex.SetData(this, 4, m_DissolveColor.r);	// param2.x : red
				ptex.SetData(this, 5, m_DissolveColor.g);	// param2.y : green
				ptex.SetData(this, 6, m_DissolveColor.b);	// param2.z : blue
			}
		}

#if UNITY_EDITOR
		/// <summary>
		/// Gets the material.
		/// </summary>
		/// <returns>The material.</returns>
#endif

		//################################
		// Private Members.
		//################################
	}
}
