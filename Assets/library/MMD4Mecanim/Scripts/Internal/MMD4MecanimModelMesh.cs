using UnityEngine;
using System.Collections;

using MorphType					= MMD4MecanimData.MorphType;
using MeshFlags					= MMD4MecanimData.MeshFlags;

public partial class MMD4MecanimModel
{
	bool	_updatedGlobalAmbient;
	Color 	_globalAmbient;

	//--------------------------------------------------------------------------------------------------------------------

	void _UpdateAmbientPreview()
	{
		if( _cloneMaterials != null ) {
			for( int i = 0; i != _cloneMaterials.Length; ++i ) {
				if( _cloneMaterials[i] != null ) {
					_UpdateAmbientPreviewInternal( _cloneMaterials[i].materials );
				}
			}
		} else {
			SkinnedMeshRenderer[] skinnedMeshRenderers = MMD4MecanimCommon.GetSkinnedMeshRenderers( this.gameObject );
			if( skinnedMeshRenderers != null ) {
				foreach( SkinnedMeshRenderer skinnedMeshRenderer in skinnedMeshRenderers ) {
					_UpdateAmbientPreviewInternal( skinnedMeshRenderer.sharedMaterials );
				}
			}
			MeshRenderer[] meshRenderers = MMD4MecanimCommon.GetMeshRenderers( this.gameObject );
			if( meshRenderers != null ) {
				foreach( MeshRenderer meshRenderer in meshRenderers ) {
					_UpdateAmbientPreviewInternal( meshRenderer.sharedMaterials );
				}
			}
		}
	}

	void _UpdateAmbientPreviewInternal( Material[] materials )
	{
		Color globalAmbient = RenderSettings.ambientLight;

		if( materials != null ) {
			for( int m = 0; m != materials.Length; ++m ) {
				Material material = materials[m];
				if( material != null && material.shader != null && material.shader.name != null && material.shader.name.StartsWith("MMD4Mecanim") ) {
					Color diffuse = material.GetColor("_Color");
					Color ambient = material.GetColor("_Ambient");
					
					Color tempAmbient = MMD4MecanimCommon.MMDLit_GetTempAmbient(globalAmbient, ambient);
					Color tempAmbientL = MMD4MecanimCommon.MMDLit_GetTempAmbientL(ambient);
					Color tempDiffuse = MMD4MecanimCommon.MMDLit_GetTempDiffuse(globalAmbient, ambient, diffuse);
					tempDiffuse.a = diffuse.a;
					
					MMD4MecanimCommon.WeakSetMaterialColor( material, "_TempAmbient", tempAmbient );
					MMD4MecanimCommon.WeakSetMaterialColor( material, "_TempAmbientL", tempAmbientL );
					MMD4MecanimCommon.WeakSetMaterialColor( material, "_TempDiffuse", tempDiffuse );
				}
			}
		}
	}

	//--------------------------------------------------------------------------------------------------------------------

	void _InitializeGlobalAmbient()
	{
		_updatedGlobalAmbient = true;
		_globalAmbient = RenderSettings.ambientLight;
	}

	void _UpdateGlobalAmbient()
	{
		_updatedGlobalAmbient |= (_globalAmbient != RenderSettings.ambientLight);
		if( _updatedGlobalAmbient && _cloneMaterials != null ) {
			_globalAmbient = RenderSettings.ambientLight;
			foreach( CloneMaterial cloneMaterial in _cloneMaterials ) {
				if( cloneMaterial.updateMaterialData != null && cloneMaterial.materialData != null && cloneMaterial.materials != null ) {
					for( int i = 0; i < cloneMaterial.updateMaterialData.Length; ++i ) {
						cloneMaterial.updateMaterialData[i] = true;
					}
				}
			}
		}
	}

	void _CleanupGlobalAmbient()
	{
		_updatedGlobalAmbient = false;
	}

	void _UpdateAutoLuminous()
	{
		if( this.morphAutoLuminous != null && this.morphAutoLuminous.updated && _cloneMaterials != null ) { // Check for updated AutoLuminous.
			foreach( CloneMaterial cloneMaterial in _cloneMaterials ) {
				if( cloneMaterial.updateMaterialData != null && cloneMaterial.materialData != null && cloneMaterial.materials != null ) {
					for( int i = 0; i < cloneMaterial.updateMaterialData.Length; ++i ) {
						if( !cloneMaterial.updateMaterialData[i] ) {
							float shininess = cloneMaterial.materialData[i].shininess;
							if( shininess > 100.0f ) {
								cloneMaterial.updateMaterialData[i] = true;
							}
						}
					}
				}
			}
		}
	}
	
	void _CleanupAutoLuminous()
	{
		if( this.morphAutoLuminous != null ) { // Cleanup for updated AutoLuminous.
			this.morphAutoLuminous.updated = false;
		}
	}

	public int _skinnedMeshCount {
		get {
			if( _skinnedMeshRenderers != null ) {
				return _skinnedMeshRenderers.Length;
			}
			return 0;
		}
	}

	public int _cloneMeshCount {
		get {
			if( _cloneMeshes != null ) {
				return _cloneMeshes.Length;
			}
			return 0;
		}
	}

	public void _UploadMeshVertex( int meshID, Vector3[] vertices, Vector3[] normals )
	{
		if( _cloneMeshes != null && meshID >= 0 && meshID < _cloneMeshes.Length ) {
			CloneMesh cloneMesh = _cloneMeshes[meshID];
			if( cloneMesh != null ) {
				if( vertices != null ) {
					cloneMesh.mesh.vertices = vertices;
				}
				if( normals != null ) {
					cloneMesh.mesh.normals = normals;
				}
			}
		}
	}

	void _UploadMeshMaterial()
	{
		if( !Application.isPlaying ) {
			return; // Don't initialize cloneMesh for Editor Mode.
		}

		_UpdateGlobalAmbient();
		_UpdateAutoLuminous();
		
		if( _cloneMaterials != null ) {
			foreach( CloneMaterial cloneMaterial in _cloneMaterials ) {
				if( cloneMaterial.updateMaterialData != null && cloneMaterial.materialData != null && cloneMaterial.materials != null ) {
					for( int i = 0; i < cloneMaterial.updateMaterialData.Length; ++i ) {
						if( cloneMaterial.updateMaterialData[i] ) {
							cloneMaterial.updateMaterialData[i] = false;
							MMD4MecanimCommon.FeedbackMaterial( ref cloneMaterial.materialData[i], cloneMaterial.materials[i], this.morphAutoLuminous );
						}
					}
				}
			}
		}

		_CleanupGlobalAmbient();
		_CleanupAutoLuminous();
	}

	public int[] _PrepareMeshFlags()
	{
		// Memo: Require _InitializeMesh()
		if( _skinnedMeshRenderers != null ) {
			int length = _skinnedMeshRenderers.Length;
			if( length > 0 ) {
				return new int[length];
			}
		}
		
		return null;
	}

	public int[] _PrepareMeshFlags( out bool blendShapesAnything )
	{
		// Memo: Require _InitializeMesh()
		blendShapesAnything = false;
		if( _skinnedMeshRenderers != null ) {
			int length = _skinnedMeshRenderers.Length;
			if( length > 0 ) {
				int[] meshFlags = new int[length];
				#if UNITY_4_0 || UNITY_4_1 || UNITY_4_2
				// Not supported BlendShapes.
				#else
				if( this.vertexMorphEnabled && this.blendShapesEnabled ) {
					for( int i = 0; i != length; ++i ) {
						SkinnedMeshRenderer skinnedMeshRenderer = _skinnedMeshRenderers[i];
						if( skinnedMeshRenderer != null &&
							skinnedMeshRenderer.sharedMesh != null &&
							skinnedMeshRenderer.sharedMesh.blendShapeCount != 0 ) {
							unchecked {
								meshFlags[i] |= (int)MeshFlags.BlendShapes;
								blendShapesAnything = true;
							}
						}
					}
				}
				#endif
				return meshFlags;
			}
		}

		return null;
	}

	public void _InitializeCloneMesh( int[] meshFlags )
	{
		// Memo: Require _InitializeMesh()
		int cloneMaterialIndex = 0;
		int cloneMaterialLength = 0;
		if( _meshRenderers != null ) {
			cloneMaterialLength += _meshRenderers.Length;
		}
		if( _skinnedMeshRenderers != null ) {
			cloneMaterialLength += _skinnedMeshRenderers.Length;
		}
		if( cloneMaterialLength > 0 ) {
			_cloneMaterials = new CloneMaterial[cloneMaterialLength];
		}

		if( _meshRenderers != null ) { // Setup Materials for No Animation Mesh.
			for( int meshIndex = 0; meshIndex != _meshRenderers.Length; ++meshIndex ) {
				MeshRenderer meshRenderer = _meshRenderers[meshIndex];
				
				Material[] materials = null;
				if( Application.isPlaying ) {
					materials = meshRenderer.materials;
				}
				if( materials == null ) { // for Editor Mode.
					materials = meshRenderer.sharedMaterials;
				}
				
				_PostfixRenderQueue( materials, this.postfixRenderQueue );
				
				_cloneMaterials[cloneMaterialIndex] = new CloneMaterial();
				_SetupCloneMaterial( _cloneMaterials[cloneMaterialIndex], materials );
				++cloneMaterialIndex;
			}
		}

		bool initializeCloneMesh = false;
		if( meshFlags != null && _skinnedMeshRenderers != null && meshFlags.Length == _skinnedMeshRenderers.Length && Application.isPlaying ) { // Don't initialize cloneMesh in Editor Mode.
			for( int i = 0; i != meshFlags.Length && !initializeCloneMesh; ++i ) {
				unchecked {
					initializeCloneMesh = (meshFlags[i] & (int)(MeshFlags.VertexMorph | MeshFlags.XDEF)) != 0;
				}
			}
		}

		if( _skinnedMeshRenderers != null ) {
			if( initializeCloneMesh ) {
				_cloneMeshes = new CloneMesh[_skinnedMeshRenderers.Length];
			}
			
			for( int meshIndex = 0; meshIndex != _skinnedMeshRenderers.Length; ++meshIndex ) {
				SkinnedMeshRenderer skinnedMeshRenderer = _skinnedMeshRenderers[meshIndex];
				
				if( (meshFlags[meshIndex] & (int)(MeshFlags.VertexMorph | MeshFlags.XDEF)) != 0 ) {
					_cloneMeshes[meshIndex] = new CloneMesh();
					MMD4MecanimCommon.CloneMeshWork cloneMeshWork = MMD4MecanimCommon.CloneMesh( skinnedMeshRenderer.sharedMesh );
					if( cloneMeshWork != null && cloneMeshWork.mesh != null ) {
						_cloneMeshes[meshIndex].skinnedMeshRenderer = skinnedMeshRenderer;
						_cloneMeshes[meshIndex].mesh = cloneMeshWork.mesh;
						_cloneMeshes[meshIndex].vertices = cloneMeshWork.vertices;
						if( (meshFlags[meshIndex] & (int)(MeshFlags.XDEF)) != 0 ) {
							if( this.xdefNormalEnabled ) {
								_cloneMeshes[meshIndex].normals = cloneMeshWork.normals;
							}
							_cloneMeshes[meshIndex].bindposes = cloneMeshWork.bindposes;
							_cloneMeshes[meshIndex].boneWeights = cloneMeshWork.boneWeights;
						}
						skinnedMeshRenderer.sharedMesh = _cloneMeshes[meshIndex].mesh;
					} else {
						Debug.LogError("CloneMesh() Failed. : " + this.gameObject.name );
					}
				}
				
				Material[] materials = null;
				if( Application.isPlaying ) {
					materials = skinnedMeshRenderer.materials;
				}
				if( materials == null ) { // for Editor Mode.
					materials = skinnedMeshRenderer.sharedMaterials;
				}
				
				_PostfixRenderQueue( materials, this.postfixRenderQueue );
				
				_cloneMaterials[cloneMaterialIndex] = new CloneMaterial();
				_SetupCloneMaterial( _cloneMaterials[cloneMaterialIndex], materials );
				++cloneMaterialIndex;
			}
		}
		
		// Check for Deferred Rendering
		if( _cloneMaterials != null ) {
			#if MMD4MECANIM_DEBUG
			for( int i = 0; i != _cloneMaterials.Length; ++i ) {
				Material[] materials = _cloneMaterials[i].materials;
				if( materials != null ) {
					for( int m = 0; m != materials.Length; ++m ) {
						if( MMD4MecanimCommon.IsDebugShader( materials[m] ) ) {
							_supportDebug = true;
							break;
						}
					}
					if( _supportDebug ) {
						break;
					}
				}
			}
			#endif
			for( int i = 0; i != _cloneMaterials.Length; ++i ) {
				Material[] materials = _cloneMaterials[i].materials;
				if( materials != null ) {
					for( int m = 0; m != materials.Length; ++m ) {
						if( MMD4MecanimCommon.IsDeferredShader( materials[m] ) ) {
							_supportDeferred = true;
							break;
						}
					}
					if( _supportDeferred ) {
						break;
					}
				}
			}
		}
	}

	public CloneMesh _GetCloneMesh( int meshIndex )
	{
		if( _cloneMeshes != null && meshIndex >= 0 && meshIndex < _cloneMeshes.Length ) {
			return _cloneMeshes[meshIndex];
		}

		return null;
	}

	public void _CleanupCloneMesh()
	{
		if( _cloneMeshes != null ) {
			for( int i = 0; i != _cloneMeshes.Length; ++i ) {
				if( _cloneMeshes[i] != null ) {
					if( !this.xdefNormalEnabled ) {
						_cloneMeshes[i].normals = null;
					}
					_cloneMeshes[i].bindposes = null;
					_cloneMeshes[i].boneWeights = null;
				}
			}
		}
	}
}
