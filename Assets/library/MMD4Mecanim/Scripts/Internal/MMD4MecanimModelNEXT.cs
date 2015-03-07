using UnityEngine;
using System.Collections;

public partial class MMD4MecanimModel
{
	public enum NEXTEdgePass
	{
		Pass4,
		Pass8,
	}

	public const float NEXTEdgeScale = 0.05f;

	public Material			nextEdgeMaterial_Pass4;
	public Material			nextEdgeMaterial_Pass8;
	public NEXTEdgePass		nextEdgePass = NEXTEdgePass.Pass4;
	public float			nextEdgeSize = 1.0f;
	public Color			nextEdgeColor = new Color( 0.4f, 1.0f, 1.0f, 1.0f );

	MeshRenderer[]			_nextEdgeMeshRenderers;
	SkinnedMeshRenderer[]	_nextEdgeSkinnedMeshRenderers;
	bool					_nextEdgeVisibleCached;
	float					_nextEdgeSizeCached;
	Color					_nextEdgeColorCached;

	public bool				supportNEXTEdge;

	void _InitializeNEXTMaterial()
	{
		if( this.nextEdgeMaterial_Pass4 == null || this.nextEdgeMaterial_Pass4.shader == null ) {
			this.nextEdgeMaterial_Pass4 = new Material( Shader.Find("MMD4Mecanim/MMDLit-NEXTEdge-Pass4") );
		}
		if( this.nextEdgeMaterial_Pass8 == null || this.nextEdgeMaterial_Pass8.shader == null ) {
			this.nextEdgeMaterial_Pass8 = new Material( Shader.Find("MMD4Mecanim/MMDLit-NEXTEdge-Pass8") );
		}
	}
	
	void _InitializeNEXTEdgeMesh()
	{
		if( !supportNEXTEdge ) {
			return;
		}
		
		_InitializeNEXTMaterial();
		
		if( this.nextEdgeMaterial_Pass4 == null ) {
			Debug.LogWarning( "nextEdgeMaterial_Pass4 is null. Skipped _InitializenextEdgeMesh()." );
			return;
		}
		if( this.nextEdgeMaterial_Pass8 == null ) {
			Debug.LogWarning( "nextEdgeMaterial_Pass8 is null. Skipped _InitializenextEdgeMesh()." );
			return;
		}

		//this.nextEdgeMaterial_Pass4.renderQueue = MaterialEdgeRenderQueue;
		//this.nextEdgeMaterial_Pass8.renderQueue = MaterialEdgeRenderQueue;

		bool isVisible = ( this.nextEdgeSize > 0.0f );
		_nextEdgeVisibleCached = isVisible;

		if( _meshRenderers != null ) {
			_nextEdgeMeshRenderers = new MeshRenderer[_meshRenderers.Length];
			for( int i = 0; i < _meshRenderers.Length; ++i ) {
				MeshRenderer meshRenderer = _meshRenderers[i];
				if( meshRenderer != null ) {
					Material[] materials = meshRenderer.sharedMaterials;
					if( materials == null || materials.Length == 0 ) {
						materials = meshRenderer.materials;
					}
					if( materials != null ) {
						materials = _CloneNEXTEdgeMaterials( materials );
					}
					if( materials != null ) {
						GameObject go = _CreateNEXTEdgeGameObject( meshRenderer.gameObject );
						MeshRenderer r = go.AddComponent<MeshRenderer>();
						r.enabled = isVisible;
						r.castShadows = false;
						r.receiveShadows = false;
						r.materials = materials;
						MeshFilter meshFilter = _meshRenderers[i].gameObject.GetComponent<MeshFilter>();
						if( meshFilter != null ) {
							MeshFilter m = go.AddComponent<MeshFilter>();
							m.sharedMesh = meshFilter.sharedMesh;
						}
						_nextEdgeMeshRenderers[i] = r;
					}
				}
			}
		}
		
		if( _skinnedMeshRenderers != null ) {
			_nextEdgeSkinnedMeshRenderers = new SkinnedMeshRenderer[_skinnedMeshRenderers.Length];
			for( int i = 0; i < _skinnedMeshRenderers.Length; ++i ) {
				SkinnedMeshRenderer skinnedMeshRenderer = _skinnedMeshRenderers[i];
				if( skinnedMeshRenderer != null ) {
					Material[] materials = skinnedMeshRenderer.sharedMaterials;
					if( materials == null || materials.Length == 0 ) {
						materials = skinnedMeshRenderer.materials;
					}
					if( materials != null ) {
						materials = _CloneNEXTEdgeMaterials( materials );
					}
					if( materials != null ) {
						GameObject go = _CreateNEXTEdgeGameObject( skinnedMeshRenderer.gameObject );
						SkinnedMeshRenderer r = go.AddComponent<SkinnedMeshRenderer>();
						r.sharedMesh = skinnedMeshRenderer.sharedMesh;
						r.bones = skinnedMeshRenderer.bones;
						r.rootBone = skinnedMeshRenderer.rootBone;
						r.castShadows = false;
						r.receiveShadows = false;
						r.materials = materials;
						r.enabled = isVisible;
						_nextEdgeSkinnedMeshRenderers[i] = r;
					}
				}
			}
		}

		_UpdatedNEXTEdge();
	}
	
	static GameObject _CreateNEXTEdgeGameObject( GameObject parentGameObject )
	{
		if( parentGameObject != null ) {
			GameObject go = new GameObject(parentGameObject.name + "(NEXTEdge)");
			go.transform.parent = parentGameObject.transform;
			go.transform.localPosition = Vector3.zero;
			go.transform.localRotation = Quaternion.identity;
			go.transform.localScale = Vector3.one;
			return go;
		}

		return null;
	}
	
	Material[] _CloneNEXTEdgeMaterials( Material[] materials )
	{
		if( materials != null ) {
			Material[] m = new Material[materials.Length];
			for( int j = 0; j < materials.Length; ++j ) {
				if( materials[j] != null && materials[j].shader != null ) {
					if( this.nextEdgePass == NEXTEdgePass.Pass4 ) {
						m[j] = this.nextEdgeMaterial_Pass4;
					} else {
						m[j] = this.nextEdgeMaterial_Pass8;
					}
				}
			}
			return m;
		}

		return null;
	}

	void _UpdatedNEXTEdge()
	{
		if( !supportNEXTEdge ) {
			return;
		}

		bool isVisible = ( this.nextEdgeSize > 0.0f );
		if( _nextEdgeVisibleCached != isVisible ) {
			_nextEdgeVisibleCached = isVisible;
			if( _nextEdgeMeshRenderers != null ) {
				foreach( var r in _nextEdgeMeshRenderers ) {
					r.enabled = isVisible;
				}
			}
			if( _nextEdgeSkinnedMeshRenderers != null ) {
				foreach( var r in _nextEdgeSkinnedMeshRenderers ) {
					r.enabled = isVisible;
				}
			}
		}

		if( _nextEdgeSizeCached != this.nextEdgeSize ||
		    _nextEdgeColorCached != this.nextEdgeColor ) {
			_nextEdgeSizeCached = this.nextEdgeSize;
			_nextEdgeColorCached = this.nextEdgeColor;
			if( nextEdgeMaterial_Pass4 != null ) {
				nextEdgeMaterial_Pass4.SetFloat( "_EdgeSize", _nextEdgeSizeCached * NEXTEdgeScale );
				nextEdgeMaterial_Pass4.SetColor( "_EdgeColor", _nextEdgeColorCached );
			}
			if( nextEdgeMaterial_Pass8 != null ) {
				nextEdgeMaterial_Pass8.SetFloat( "_EdgeSize", _nextEdgeSizeCached * NEXTEdgeScale );
				nextEdgeMaterial_Pass8.SetColor( "_EdgeColor", _nextEdgeColorCached );
			}
		}
	}
}
