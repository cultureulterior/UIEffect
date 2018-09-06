﻿using System;
using UnityEngine;
using UnityEngine.UI;

namespace Coffee.UIExtensions
{
	/// <summary>
	/// Dissolve effect for uGUI.
	/// </summary>
	[ExecuteInEditMode]
	public class UIDissolve : UIEffectBase
	{
		//################################
		// Constant or Static Members.
		//################################
		static readonly ParameterTexture _ptex = new ParameterTexture(8, 128, "_ParamTex");


		//################################
		// Serialize Members.
		//################################
		[SerializeField] [Range(0, 1)] float m_Location = 0.5f;
		[SerializeField] [Range(0, 1)] float m_Width = 0.5f;
		[SerializeField] [Range(0, 1)] float m_Softness = 0.5f;
		[SerializeField] [ColorUsage(false)] Color m_Color = new Color(0.0f, 0.25f, 1.0f);
		[SerializeField] ColorMode m_ColorMode = ColorMode.Add;
		[SerializeField] Texture m_NoiseTexture;
		[SerializeField] protected EffectArea m_EffectArea;
		[SerializeField] bool m_KeepAspectRatio;

		[Header("Effect Runner")]
		[SerializeField] EffectRunner m_Runner;

		[Header("Play Effect")]
		[Obsolete][HideInInspector]
		[SerializeField] bool m_Play = false;
		[Obsolete][HideInInspector]
		[SerializeField][Range(0.1f, 10)] float m_Duration = 1;
		[Obsolete][HideInInspector]
		[SerializeField] AnimatorUpdateMode m_UpdateMode = AnimatorUpdateMode.Normal;


		//################################
		// Public Members.
		//################################

		/// <summary>
		/// Current location[0-1] for dissolve effect. 0 is not dissolved, 1 is completely dissolved.
		/// </summary>
		public float location
		{
			get { return m_Location; }
			set
			{
				value = Mathf.Clamp(value, 0, 1);
				if (!Mathf.Approximately(m_Location, value))
				{
					m_Location = value;
					SetDirty();
				}
			}
		}

		/// <summary>
		/// Edge width.
		/// </summary>
		public float width
		{
			get { return m_Width; }
			set
			{
				value = Mathf.Clamp(value, 0, 1);
				if (!Mathf.Approximately(m_Width, value))
				{
					m_Width = value;
					SetDirty();
				}
			}
		}

		/// <summary>
		/// Edge softness.
		/// </summary>
		public float softness
		{
			get { return m_Softness; }
			set
			{
				value = Mathf.Clamp(value, 0, 1);
				if (!Mathf.Approximately(m_Softness, value))
				{
					m_Softness = value;
					SetDirty();
				}
			}
		}

		/// <summary>
		/// Edge color.
		/// </summary>
		public Color color
		{
			get { return m_Color; }
			set
			{
				if (m_Color != value)
				{
					m_Color = value;
					SetDirty();
				}
			}
		}

		/// <summary>
		/// Noise texture.
		/// </summary>
		public Texture noiseTexture
		{
			get { return m_NoiseTexture ?? graphic.material.GetTexture("_NoiseTex"); }
			set
			{
				if (m_NoiseTexture != value)
				{
					m_NoiseTexture = value;
					if (targetGraphic)
					{
						ModifyMaterial(effectHash);
					}
				}
			}
		}

		/// <summary>
		/// The area for effect.
		/// </summary>
		public EffectArea effectArea
		{
			get { return m_EffectArea; }
			set
			{
				if (m_EffectArea != value)
				{
					m_EffectArea = value;
					SetDirty();
				}
			}
		}

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
					SetDirty();
				}
			}
		}

		/// <summary>
		/// Color effect mode.
		/// </summary>
		public ColorMode colorMode { get { return m_ColorMode; } }

		/// <summary>
		/// Play effect on enable.
		/// </summary>
		public bool play { get { return m_Runner.running; } set { m_Runner.running = value; } }

		/// <summary>
		/// Play effect loop.
		/// </summary>
		public bool loop { get { return m_Runner.loop; } set { m_Runner.loop = value; } }

		/// <summary>
		/// The duration for playing effect.
		/// </summary>
		public float duration { get { return m_Runner.duration; } set { m_Runner.duration = Mathf.Max(value, 0.1f); } }

		/// <summary>
		/// Delay on loop effect.
		/// </summary>
		public float loopDelay { get { return m_Runner.loopDelay; } set { m_Runner.loopDelay = Mathf.Max(value, 0); } }

		/// <summary>
		/// Update mode for playing effect.
		/// </summary>
		public AnimatorUpdateMode updateMode { get { return m_Runner.updateMode; } set { m_Runner.updateMode = value; } }

		/// <summary>
		/// Gets the parameter texture.
		/// </summary>
		public override ParameterTexture ptex { get { return _ptex; } }

		/// <summary>
		/// Gets hash for effect.
		/// </summary>
		protected override ulong effectHash { get { return ((ulong)2 << 0) + ((ulong)m_ColorMode << 4) + (m_NoiseTexture ? (uint)m_NoiseTexture.GetInstanceID() << 8 : 0); } }

		/// <summary>
		/// Modifies the material.
		/// </summary>
		public override void ModifyMaterial(ulong hash, Func<Material> onCreate = null)
		{
			base.ModifyMaterial(hash, () =>
				{
					var mat = new Material(m_EffectMaterial);
					mat.name += "_" + hash;
					mat.EnableKeyword(m_ColorMode.ToString().ToUpper());

					if(m_NoiseTexture)
					{
						mat.SetTexture("_NoiseTex", m_NoiseTexture);
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
				return;

			float normalizedIndex = ptex.GetNormalizedIndex(this);

			// rect.
			var tex = noiseTexture;
			var aspectRatio = m_KeepAspectRatio && tex ? ((float)tex.width) / tex.height : -1;
			Rect rect = m_EffectArea.GetEffectArea(vh, graphic, aspectRatio);

			// Calculate vertex position.
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

		protected override void SetDirty()
		{
			ptex.SetData(this, 0, m_Location);	// param1.x : location
			ptex.SetData(this, 1, m_Width);		// param1.y : width
			ptex.SetData(this, 2, m_Softness);	// param1.z : softness
			ptex.SetData(this, 4, m_Color.r);	// param2.x : red
			ptex.SetData(this, 5, m_Color.g);	// param2.y : green
			ptex.SetData(this, 6, m_Color.b);	// param2.z : blue
		}

		/// <summary>
		/// Play effect.
		/// </summary>
		public void Play()
		{
			m_Runner.Play();
		}


		//################################
		// Protected Members.
		//################################
		/// <summary>
		/// This function is called when the object becomes enabled and active.
		/// </summary>
		protected override void OnEnable()
		{
			base.OnEnable();
			if (m_Runner == null)
			{
				m_Runner = new EffectRunner();
			}
			m_Runner.OnEnable(f => location = f);
		}

		/// <summary>
		/// This function is called when the behaviour becomes disabled.
		/// </summary>
		protected override void OnDisable()
		{
			m_Runner.OnDisable();
			base.OnDisable();
		}

#if UNITY_EDITOR
		#pragma warning disable 0612
		/// <summary>
		/// Upgrade instance if needed.
		/// </summary>
		protected override void UpgradeIfNeeded()
		{
			// Upgrade for v3.0.0
			if (IsShouldUpgrade(300))
			{
				m_Runner.running = m_Play;
				m_Runner.duration = m_Duration;
				m_Runner.loop = false;
				m_Runner.loopDelay = 1;
				m_Runner.updateMode = m_UpdateMode;
			}
		}
		#pragma warning restore 0612
#endif
	}
}
