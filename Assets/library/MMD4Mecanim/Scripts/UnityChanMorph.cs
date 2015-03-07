using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using IMorph = MMD4MecanimAnim.IMorph;

public class UnityChanMorph : MonoBehaviour
{
	[System.Serializable]
	public class SkinnedMesh
	{
		public string 						name = "";
		public Transform					transform = null;
		public SkinnedMeshRenderer			skinnedMeshRenderer = null;
		public Mesh							sharedMesh;
	}

	[System.Serializable]
	public class VertexMorphData
	{
		public string 						skinnedMeshName = "";
		public SkinnedMesh					skinnedMesh = null;
		public string						blendShapeName = "";
		public int							blendShapeIndex = -1;
		public float						weightRate = 1.0f;

		public VertexMorphData( string skinnedMeshName, string blendShapeName, float weightRate )
		{
			this.skinnedMeshName = skinnedMeshName;
			this.skinnedMesh = null;
			this.blendShapeName = blendShapeName;
			this.blendShapeIndex = -1;
			this.weightRate = weightRate;
		}
	}
	
	[System.Serializable]
	public class Morph : IMorph
	{
		public string						name = "";
		public float						weight = 0.0f;
		public VertexMorphData[]			vertexMorphData;

		public Morph( string name, VertexMorphData[] vertexMorphData )
		{
			this.name = name;
			this.vertexMorphData = vertexMorphData;
		}

		string IMorph.name {
			get { return this.name; }
		}
		
		float IMorph.weight {
			get { return this.weight; }
			set { this.weight = value; }
		}
	}

	public static readonly string[] SkinnedMeshNames = new string[] {
		"BLW_DEF",
		"EYE_DEF",
		"EL_DEF",
		"MTH_DEF",
	};

	public static readonly Morph[] PresetMorphs = new Morph[] {

		new Morph( "\u771f\u9762\u76ee", new VertexMorphData[] { // Ma-Ji-Me
			new VertexMorphData("BLW_DEF", "blendShape3.BLW_ANG1", 0.5f),
		} ),
		new Morph( "\u6012\u308a", new VertexMorphData[] { // Ika-Ri
			new VertexMorphData("BLW_DEF", "blendShape3.BLW_ANG2", 1.0f),
		} ),
		new Morph( "\u304b\u306a\u308a\u4e0b", new VertexMorphData[] { // Ka-Na-Ri-Shita
			new VertexMorphData("BLW_DEF", "blendShape3.BLW_CONF", 0.5f),
		} ),
		new Morph( "\u304b\u306a\u308a\u56f0\u308b", new VertexMorphData[] { // Ka-Na-Ri-Koma-Ru
			new VertexMorphData("BLW_DEF", "blendShape3.BLW_CONF", 1.0f),
		} ),
		new Morph( "\uff7c\uff6c\uff77\uff70\uff9d", new VertexMorphData[] { // sh-a-ki-i-n
			new VertexMorphData("BLW_DEF", "blendShape3.BLW_ANG2", 0.7f),
		} ),
		new Morph( "\u4e0b", new VertexMorphData[] { // Shita
			new VertexMorphData("BLW_DEF", "blendShape3.BLW_CONF", 0.5f),
		} ),
		new Morph( "\u56f0\u308b", new VertexMorphData[] { // Koma-Ru
			new VertexMorphData("BLW_DEF", "blendShape3.BLW_CONF", 1.0f),
		} ),
		new Morph( "\u4e0a", new VertexMorphData[] { // Ue
			new VertexMorphData("BLW_DEF", "blendShape3.BLW_SAP", 1.0f),
		} ),

		new Morph( "\u307e\u3070\u305f\u304d", new VertexMorphData[] { // Ma-Ba-Ta-Ki
			new VertexMorphData("EYE_DEF", "blendShape2.EYE_DEF_C", 1.0f),
			new VertexMorphData("EL_DEF", "blendShape2.EYE_DEF_C", 1.0f),
		} ),
		new Morph( "\u4e0b1", new VertexMorphData[] { // Shita-1
			new VertexMorphData("EYE_DEF", "blendShape2.EYE_ANG1", 1.0f),
			new VertexMorphData("EL_DEF", "blendShape2.EYE_ANG1", 1.0f),
		} ),
		new Morph( "\u4e0b2", new VertexMorphData[] { // Shita-2
			new VertexMorphData("EYE_DEF", "blendShape2.EYE_ANG1", 1.0f),
			new VertexMorphData("EL_DEF", "blendShape2.EYE_ANG1", 1.0f),
		} ),
		new Morph( "\u7b11\u3044", new VertexMorphData[] { // Wara-i
			new VertexMorphData("EYE_DEF", "blendShape2.EYE_SMILE1", 1.0f),
			new VertexMorphData("EL_DEF", "blendShape2.EYE_SMILE1", 1.0f),
		} ),
		new Morph( "\u3058\u3068\u76ee", new VertexMorphData[] { // Ji-To-Me
			new VertexMorphData("EYE_DEF", "blendShape2.EYE_DEF_C", 0.25f),
			new VertexMorphData("EL_DEF", "blendShape2.EYE_DEF_C", 0.25f),
		} ),

		new Morph( "\u25b2", new VertexMorphData[] { // Sankaku(Upper)
			new VertexMorphData("MTH_DEF", "blendShape1.MTH_SMILE1", 0.3f),
			new VertexMorphData("MTH_DEF", "blendShape1.MTH_ANG1", 0.8f),
		} ),
		new Morph( "\u3042", new VertexMorphData[] { // a
			new VertexMorphData("MTH_DEF", "blendShape1.MTH_SMILE1", 0.8f),
		} ),
		new Morph( "\u3044", new VertexMorphData[] { // i
			new VertexMorphData("MTH_DEF", "blendShape1.MTH_SMILE1", 0.3f),
		} ),
		new Morph( "\u3046", new VertexMorphData[] { // u
			new VertexMorphData("MTH_DEF", "blendShape1.MTH_SMILE1", 0.3f),
			new VertexMorphData("MTH_DEF", "blendShape1.MTH_ANG1", 0.3f),
		} ),
		new Morph( "\u3048", new VertexMorphData[] { // e
			new VertexMorphData("MTH_DEF", "blendShape1.MTH_SMILE1", 0.3f),
			new VertexMorphData("MTH_DEF", "blendShape1.MTH_ANG2", 0.1f),
		} ),
		new Morph( "\u304a", new VertexMorphData[] { // o
			new VertexMorphData("MTH_DEF", "blendShape1.MTH_SMILE1", 0.3f),
			new VertexMorphData("MTH_DEF", "blendShape1.MTH_SMILE2", 0.3f),
			new VertexMorphData("MTH_DEF", "blendShape1.MTH_SAP", 0.5f),
		} ),
		new Morph( "\u306b\u3063\u3053\u308a", new VertexMorphData[] { // Ni-Tsu-Ko-Ri
			new VertexMorphData("MTH_DEF", "blendShape1.MTH_SMILE2", 1.0f),
		} ),
		new Morph( "\u306b\u3084\u308a", new VertexMorphData[] { // Ni-Ya-Ri
			new VertexMorphData("MTH_DEF", "blendShape1.MTH_SMILE2", 1.0f),
		} ),
		new Morph( "\u30ef", new VertexMorphData[] { // Wa
			new VertexMorphData("MTH_DEF", "blendShape1.MTH_SMILE1", 1.0f),
		} ),
		new Morph( "\u7b11\u3046", new VertexMorphData[] { // Wara-U
			new VertexMorphData("MTH_DEF", "blendShape1.MTH_SMILE2", 1.0f),
		} ),
	};

	public SkinnedMesh[] skinnedMeshes;
	public Morph[] morphs = PresetMorphs;

	public SkinnedMesh GetSkinnedMesh( string skinnedMeshName )
	{
		if( skinnedMeshes != null && skinnedMeshName != null ) {
			foreach( SkinnedMesh skinnedMesh in skinnedMeshes ) {
				if( skinnedMesh.name == skinnedMeshName ) {
					return skinnedMesh;
				}
			}
		}

		return null;
	}

	public Morph GetMorph( string morphName )
	{
		return GetMorph( morphName, false );
	}

	public Morph GetMorph( string morphName, bool isStartsWith )
	{
		if( this.morphs != null && !string.IsNullOrEmpty( morphName ) ) {
			foreach( Morph morph in this.morphs ) {
				if( morph.name == morphName ) {
					return morph;
				}
			}
			if( isStartsWith ) {
				foreach( Morph morph in this.morphs ) {
					if( morph.name != null && morph.name.StartsWith( morphName ) ) {
						return morph;
					}
				}
			}
		}

		return null;
	}

	static Transform FindChildRecursively( Transform tranform, string name )
	{
		if( tranform.name == name ) {
			return tranform;
		}

		foreach( Transform childTransform in tranform ) {
			Transform r = FindChildRecursively( childTransform, name );
			if( r != null ) {
				return r;
			}
		}

		return null;
	}

	public void InitializeSkinnedMeshes()
	{
		if( this.skinnedMeshes != null && this.skinnedMeshes.Length == SkinnedMeshNames.Length ) {
			bool notFoundAnything = false;
			foreach( SkinnedMesh skinnedMesh in this.skinnedMeshes ) {
				if( skinnedMesh.transform == null ) {
					notFoundAnything = true;
					break;
				}
			}
			if( !notFoundAnything ) {
				return;
			}
		}

		this.skinnedMeshes = new SkinnedMesh[SkinnedMeshNames.Length];
		for( int i = 0; i < SkinnedMeshNames.Length; ++i ) {
			SkinnedMesh skinnedMesh = new SkinnedMesh();
			skinnedMesh.name = SkinnedMeshNames[i];
			skinnedMesh.transform = FindChildRecursively( this.transform, skinnedMesh.name );
			if( skinnedMesh.transform != null ) {
				skinnedMesh.skinnedMeshRenderer = skinnedMesh.transform.gameObject.GetComponent<SkinnedMeshRenderer>();
				if( skinnedMesh.skinnedMeshRenderer != null ) {
					skinnedMesh.sharedMesh = skinnedMesh.skinnedMeshRenderer.sharedMesh;
				}
			}
			this.skinnedMeshes[i] = skinnedMesh;
		}
	}

	void Awake()
	{
		InitializeSkinnedMeshes();

		if( this.morphs != null ) {
			foreach( Morph morph in this.morphs ) {
				if( morph.vertexMorphData != null ) {
					foreach( var vertexMorphData in morph.vertexMorphData ) {
						if( vertexMorphData.skinnedMesh == null || string.IsNullOrEmpty(vertexMorphData.skinnedMesh.name) ) {
							vertexMorphData.skinnedMesh = GetSkinnedMesh( vertexMorphData.skinnedMeshName );
							vertexMorphData.blendShapeIndex = -1;
						}
						if( vertexMorphData.skinnedMesh != null ) {
							if( vertexMorphData.blendShapeIndex < 0 ) {
								Mesh mesh = vertexMorphData.skinnedMesh.sharedMesh;
								if( mesh != null ) {
									for( int i = 0; i < mesh.blendShapeCount; ++i ) {
										if( vertexMorphData.blendShapeName == mesh.GetBlendShapeName( i ) ) {
											vertexMorphData.blendShapeIndex = i;
											break;
										}
									}
								}
							}
						}
					}
				}
			}
		}
	}

	void LateUpdate()
	{
		if( this.skinnedMeshes != null ) {
			foreach( SkinnedMesh skinnedMesh in this.skinnedMeshes ) {
				if( skinnedMesh.skinnedMeshRenderer != null && skinnedMesh.sharedMesh != null ) {
					for( int i = 0; i < skinnedMesh.sharedMesh.blendShapeCount; ++i ) {
						skinnedMesh.skinnedMeshRenderer.SetBlendShapeWeight( i, 0.0f );
					}
				}
			}
		}

		if( this.morphs != null ) {
			foreach( Morph morph in this.morphs ) {
				if( morph.weight > 0.0f && morph.vertexMorphData != null ) {
					float f_weight = morph.weight * 100.0f;
					foreach( var vertexMorphData in morph.vertexMorphData ) {
						if( vertexMorphData.skinnedMesh != null &&
						    vertexMorphData.skinnedMesh.skinnedMeshRenderer != null ) {
							if( vertexMorphData.blendShapeIndex >= 0 ) {
								float weight = vertexMorphData.skinnedMesh.skinnedMeshRenderer.GetBlendShapeWeight( vertexMorphData.blendShapeIndex );
								weight += f_weight * vertexMorphData.weightRate;
								vertexMorphData.skinnedMesh.skinnedMeshRenderer.SetBlendShapeWeight( vertexMorphData.blendShapeIndex, weight );
							}
						}
					}
				}
			}
		}
	}
}
