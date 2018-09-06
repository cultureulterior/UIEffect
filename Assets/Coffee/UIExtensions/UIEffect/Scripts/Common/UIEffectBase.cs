using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.IO;
using System;

namespace Coffee.UIExtensions
{
	/// <summary>
	/// Abstract effect base for UI.
	/// </summary>
	[ExecuteInEditMode]
	[RequireComponent(typeof(Graphic))]
	[DisallowMultipleComponent]
	public abstract class UIEffectBase : BaseMeshEffect, IParameterTexture
#if UNITY_EDITOR
	, ISerializationCallbackReceiver
#endif
	{
		protected static readonly Vector2[] splitedCharacterPosition = { Vector2.up, Vector2.one, Vector2.right, Vector2.zero };
		protected static readonly List<UIVertex> tempVerts = new List<UIVertex>();

		[HideInInspector]
		[SerializeField] int m_Version;
		[SerializeField] protected Material m_EffectMaterial;
		[SerializeField] protected Material m_PtexMaterial;

		/// <summary>
		/// Gets or sets the parameter index.
		/// </summary>
		public int parameterIndex { get; set; }

		/// <summary>
		/// Gets the parameter texture.
		/// </summary>
		public virtual ParameterTexture ptex { get { return null; } }

		/// <summary>
		/// Gets the ptex material.
		/// </summary>
		public virtual Material ptexMaterial { get { return m_PtexMaterial; } }

		/// <summary>
		/// Gets target graphic for effect.
		/// </summary>
		public Graphic targetGraphic { get { return graphic; } }

		/// <summary>
		/// Gets material for effect.
		/// </summary>
		public Material effectMaterial { get { return m_EffectMaterial; } }

		/// <summary>
		/// Gets or sets the material cache.
		/// </summary>
		protected MaterialCache materialCache{ get; set; }

		/// <summary>
		/// Gets hash for effect.
		/// </summary>
		protected virtual ulong effectHash { get { return 0; } }


#if UNITY_EDITOR
		protected override void Reset()
		{
			m_Version = 300;
			OnValidate();
		}

		/// <summary>
		/// Raises the validate event.
		/// </summary>
		protected override void OnValidate()
		{
			SetupPtexMaterial();

			if (ptex != null)
			{
				ptex.Register(this);
			}

			ModifyMaterial(effectHash);
			targetGraphic.SetVerticesDirty();
			SetDirty();
		}

		public void OnBeforeSerialize()
		{
		}

		public void OnAfterDeserialize()
		{
			UnityEditor.EditorApplication.delayCall += SetupPtexMaterial;
			UnityEditor.EditorApplication.delayCall += UpgradeIfNeeded;
		}

		protected bool IsShouldUpgrade(int expectedVersion)
		{
			if (m_Version < expectedVersion)
			{
				Debug.LogFormat(gameObject, "<b>{0}({1})</b> has been upgraded: <i>version {2} -> {3}</i>", name, GetType().Name, m_Version, expectedVersion);
				m_Version = expectedVersion;

				//UnityEditor.EditorApplication.delayCall += () =>
				{
					UnityEditor.EditorUtility.SetDirty(this);
					if (!Application.isPlaying && gameObject && gameObject.scene.IsValid())
					{
						UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
					}
				}
				;
				return true;
			}
			return false;
		}

		protected virtual void UpgradeIfNeeded()
		{
		}

		/// <summary>
		/// Gets the material.
		/// </summary>
		/// <returns>The material.</returns>
		protected virtual Material GetMaterial()
		{
			return null;
		}
#endif

		protected void SetupPtexMaterial()
		{
			if (!m_PtexMaterial)
			{
			}
		}

		/// <summary>
		/// Modifies the material.
		/// </summary>
		public virtual void ModifyMaterial(ulong hash, Func<Material> onCreate = null)
		{
			if (!m_EffectMaterial)
			{
				var cache = MaterialCache.Register((uint)GetType().GetHashCode()<<32, ()=>Resources.Load<Material>(GetType().Name));
				m_EffectMaterial = cache.material;
			}

			if (materialCache != null && (materialCache.hash != hash || !isActiveAndEnabled || !m_EffectMaterial))
			{
				MaterialCache.Unregister(materialCache);
				materialCache = null;
			}

			if (!isActiveAndEnabled || !m_EffectMaterial || onCreate == null)
			{
				graphic.material = null;
			}
			else if (materialCache != null && materialCache.hash == hash)
			{
				graphic.material = materialCache.material;
			}
			else
			{
				materialCache = MaterialCache.Register(hash, onCreate);
				graphic.material = materialCache.material;
			}
		}

		/// <summary>
		/// This function is called when the object becomes enabled and active.
		/// </summary>
		protected override void OnEnable()
		{
			SetupPtexMaterial();
			if (ptex != null)
			{
				ptex.Register(this);
			}
			ModifyMaterial(effectHash);
			targetGraphic.SetVerticesDirty();
			SetDirty();
		}

		/// <summary>
		/// This function is called when the behaviour becomes disabled () or inactive.
		/// </summary>
		protected override void OnDisable()
		{
			MaterialCache.Unregister(materialCache);
			materialCache = null;
			ModifyMaterial(0);
			targetGraphic.SetVerticesDirty();
			if (ptex != null)
			{
				ptex.Unregister(this);
			}
		}

		/// <summary>
		/// Mark the effect as dirty.
		/// </summary>
		protected virtual void SetDirty()
		{
			targetGraphic.SetVerticesDirty();
		}

		protected override void OnDidApplyAnimationProperties()
		{
			SetDirty();
		}
	}
}
