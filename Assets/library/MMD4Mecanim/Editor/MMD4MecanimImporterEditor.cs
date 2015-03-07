using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using MMD4Mecanim;

[InitializeOnLoad]
public class MMD4MecanimImporterEditor : Editor
{
	static bool _isStartupUnityEditor = true;
	static bool _isHierarchyWindowChanged = false;
	static int _isPlaymodeChangedDelay = 0;
	static int _importedMMDAssetDelay = 0;
	static int _importedFBXAssetDelay = 0;
	static List<string> _importedMMDAssetPaths = new List<string>();
	static List<string> _importedFBXAssetPaths = new List<string>();
	public static volatile bool _isProcessedPMX2FBX = false;
	static readonly string _compilingLockTempFile = "Temp/MMD4MecanimLockfile";

	static MMD4MecanimImporterEditor()
	{
		EditorApplication.playmodeStateChanged += () =>
		{
			#if MMD4MECANIM_DEBUG
			Debug.LogWarning( "EditorApplication.playmodeStateChanged() isPlaying:" + EditorApplication.isPlaying
			                 + " isPlayingOrWillChangePlaymode:" + EditorApplication.isPlayingOrWillChangePlaymode );
			#endif
			_isPlaymodeChangedDelay = 2;
		};
		
		EditorApplication.hierarchyWindowChanged += () =>
		{
			#if MMD4MECANIM_DEBUG
			Debug.LogWarning( "EditorApplication.hierarchyWindowChanged()" );
			#endif
			_isHierarchyWindowChanged = true;
		};
		
		EditorApplication.update += () =>
		{
			if( MMD4MecanimImporter._overrideEditorStyle ) {
				Object obj = UnityEditor.Selection.activeObject;
				if( obj == null || obj.GetType() != typeof(MMD4MecanimImporter) ) {
					MMD4MecanimImporter._UnlockEditorStyle();
				}
			}

			if( _isProcessedPMX2FBX ) { // for MAC(Invoke ImportAssets)
				_isProcessedPMX2FBX = false;
				AssetDatabase.Refresh();
				return;
			}

			if( EditorApplication.isCompiling ) {
				if( !File.Exists( _compilingLockTempFile ) ) {
					try {
						using( FileStream fs = File.Create( _compilingLockTempFile ) ) {
						}
					} catch( System.Exception ) {
					}
				}
				return;
			}
		
			if( _isStartupUnityEditor ) {
				if( File.Exists( _compilingLockTempFile ) ) {
					MMD4MecanimEditorCommon.ValidateScriptExecutionOrder();

					#if MMD4MECANIM_DEBUG
					Debug.LogWarning( "_compilingLockTempFile exists. Skip _StartupUnityEditor()" );
					#endif
					_isStartupUnityEditor = false; // Skip _OnStartupUnityEditor() on compiled.
					_isHierarchyWindowChanged = false; // Skip _ForceAllCheckModelInScene() on compiled.

					File.Delete( _compilingLockTempFile );
				}
			}

			if( _isPlaymodeChangedDelay > 0 ) {
				--_isPlaymodeChangedDelay;
				_isStartupUnityEditor = false; // Skip _OnStartupUnityEditor() on play mode changed.
				_isHierarchyWindowChanged = false; // Skip _ForceAllCheckModelInScene() on play mode changed.
				return;
			}

			if( !EditorApplication.isCompiling &&
			    !EditorApplication.isPlaying &&
				!EditorApplication.isPlayingOrWillChangePlaymode &&
				MMD4MecanimImporter._pmx2fbxProcessingCount == 0 ) {
				if( _isStartupUnityEditor ) {
					_isStartupUnityEditor = false;
					_OnStartupUnityEditor();
				}

				if( _importedMMDAssetDelay > 0 ) {
					--_importedMMDAssetDelay;
				} else {
					while( _importedMMDAssetPaths.Count > 0 ) {
						string mmdAssetPath = _importedMMDAssetPaths[0];
						_importedMMDAssetPaths.RemoveAt(0);
						_OnImportedMMDAsset( mmdAssetPath );
					}
				}

				if( _importedFBXAssetDelay > 0 ) {
					--_importedFBXAssetDelay;
				} else {
					while( _importedFBXAssetPaths.Count > 0 ) {
						string fbxAssetPath = _importedFBXAssetPaths[0];
						_importedFBXAssetPaths.RemoveAt(0);
						_OnImportedFBXAsset( fbxAssetPath );
					}
				}

				if( _isHierarchyWindowChanged ) {
					_isHierarchyWindowChanged = false;
					_ForceAllCheckModelInScene();
				}

				_cache_importers = null; // Purge _cache_importers.
			} else {
				_isStartupUnityEditor = false; // Skip _OnStartupUnityEditor() on processing.
				_isHierarchyWindowChanged = false; // Skip _ForceAllCheckModelInScene() on processing.
			}
		};
	}

	//------------------------------------------------------------------------------------
	
	static void _OnStartupUnityEditor()
	{
		#if MMD4MECANIM_DEBUG
		Debug.LogWarning( "_OnStartupUnityEditor()" );
		#endif

		MMD4MecanimEditorCommon.ValidateScriptExecutionOrder();

		string[] assetPaths = AssetDatabase.GetAllAssetPaths();
		if( assetPaths != null ) {
			_cache_GetImporters();

			// Check for imported .pmd/.pmx
			foreach( string assetPath in assetPaths ) {
				if( MMD4MecanimEditorCommon.IsExtensionPMDorPMX( assetPath ) ) {
					_OnImportedMMDAsset( assetPath ); // Redirect to _OnImportedMMDAsset()
				}
			}

			// Check for imported .fbx
			foreach( string assetPath in assetPaths ) {
				if( MMD4MecanimEditorCommon.IsExtensionFBX( assetPath ) ) {
					MMD4MecanimImporter importer = _GetImporterFromFBXAssetPath( assetPath );
					if( importer != null ) {
						importer._OnImportedFBXAsset( assetPath );
					}
				}
			}

			_cache_importers = null; // Purge _cache_importers.
		}
	}

	//------------------------------------------------------------------------------------

	static void _MoveDependedAssets( string[] dependedPaths, string[] dependedFromPaths )
	{
		if( dependedPaths != null && dependedFromPaths != null ) {
			for( int i = 0; i < dependedPaths.Length; ++i ) {
				if( File.Exists( dependedFromPaths[i] ) ) {
					AssetDatabase.MoveAsset( dependedFromPaths[i], dependedPaths[i] );
				}
			}
		}
	}

	static void _DeleteDependedAssets( string[] dependedPaths )
	{
		if( dependedPaths != null ) {
			foreach( string dependedPath in dependedPaths ) {
				if( File.Exists( dependedPath ) ) {
					AssetDatabase.DeleteAsset( dependedPath );
				}
			}
		}
	}

	//------------------------------------------------------------------------------------

	static void _ForceRefreshImportScale()
	{
		MMD4MecanimModel[] models = GameObject.FindObjectsOfType( typeof(MMD4MecanimModel) ) as MMD4MecanimModel[];
		if( models != null ) {
			foreach( MMD4MecanimModel model in models ) {
				string assetPath = MMD4MecanimImporter.GetAssetPath( model );
				if( assetPath != null ) {
					ModelImporter modelImporter = (ModelImporter)ModelImporter.GetAtPath( assetPath );
					float importScale = MMD4MecanimEditorCommon.GetModelImportScale( modelImporter );
					if( model.importScale != importScale ) {
						model.importScale = importScale;
						EditorUtility.SetDirty( model );
					}
				}
			}
		}
		
		// Pending: Refresh mmdModel(MMD4MecanimImporter) property.
	}

	static void _ForceAllCheckModelInScene()
	{
		Animator[] animators = GameObject.FindObjectsOfType( typeof(Animator) ) as Animator[];
		if( animators != null ) {
			foreach( Animator animator in animators ) {
				MMD4MecanimModel model = animator.gameObject.GetComponent< MMD4MecanimModel >();
				if( model == null ) {
					bool isAddedSkinningMesh = false;
					SkinnedMeshRenderer skinnedMeshRenderer = MMD4MecanimCommon.GetSkinnedMeshRenderer( animator.gameObject );
					if( skinnedMeshRenderer != null && skinnedMeshRenderer.sharedMesh != null ) {
						string assetPath = AssetDatabase.GetAssetPath( skinnedMeshRenderer.sharedMesh );
						if( MMD4MecanimEditorCommon.IsExtensionFBX( assetPath ) ) {
							_CheckModelInScene( animator, assetPath, true );
						}
					}
					if( !isAddedSkinningMesh ) {
						MeshRenderer meshRenderer = MMD4MecanimCommon.GetMeshRenderer( animator.gameObject );
						if( meshRenderer != null ) {
							MeshFilter meshFilter = meshRenderer.gameObject.GetComponent<MeshFilter>();
							if( meshFilter != null && meshFilter.sharedMesh != null ) {
								string assetPath = AssetDatabase.GetAssetPath( meshFilter.sharedMesh );
								if( MMD4MecanimEditorCommon.IsExtensionFBX( assetPath ) ) {
									_CheckModelInScene( animator, assetPath, false );
								}
							}
						}
					}
				}
			}
		}
	}

	static void _CheckModelInScene( Animator animator, string assetPath, bool isSkinningMesh )
	{
		string modelDataPath = MMD4MecanimImporter.GetModelDataPath( assetPath );
		if( File.Exists( modelDataPath ) ) {
			TextAsset modelData = (TextAsset)AssetDatabase.LoadAssetAtPath( modelDataPath, typeof(TextAsset) );
			if( isSkinningMesh ) {
				string indexDataPath = MMD4MecanimImporter.GetIndexDataPath( assetPath );
				string vertexDataPath = MMD4MecanimImporter.GetVertexDataPath( assetPath );
				if( File.Exists( indexDataPath ) ) {
					TextAsset indexData = (TextAsset)AssetDatabase.LoadAssetAtPath( indexDataPath, typeof(TextAsset) );
					TextAsset vertexData = (TextAsset)AssetDatabase.LoadAssetAtPath( vertexDataPath, typeof(TextAsset) );
					_MakeModel( animator.gameObject, assetPath, modelData, indexData, vertexData, isSkinningMesh );
				}
			} else {
				_MakeModel( animator.gameObject, assetPath, modelData, null, null, isSkinningMesh );
			}
		}
	}

	static void _MakeModel( GameObject modelGameObject, string fbxAssetPath, TextAsset modelData, TextAsset indexData, TextAsset vertexData, bool isSkinned )
	{
		if( modelData != null && (!isSkinned || indexData != null) ) {
			ModelImporter modelImporter = (ModelImporter)ModelImporter.GetAtPath( fbxAssetPath );
			if( modelImporter != null ) {
				MMD4MecanimModel model = modelGameObject.AddComponent< MMD4MecanimModel >();
				float importScale = MMD4MecanimEditorCommon.GetModelImportScale( modelImporter );
				model.importScale = importScale;
				model.modelFile = modelData;
				model.indexFile = indexData;
				model.vertexFile = vertexData; // Accept null.

				// Add Animations.(Optional)
				MMD4MecanimImporter importer = _GetImporterFromFBXAssetPath( fbxAssetPath );
				if( importer != null && importer.pmx2fbxProperty != null && importer.pmx2fbxProperty.vmdAssetList != null ) {
					foreach( Object vmdAsset in importer.pmx2fbxProperty.vmdAssetList ) {
						string vmdAssetPath = AssetDatabase.GetAssetPath( vmdAsset );
						if( !string.IsNullOrEmpty( vmdAssetPath ) ) {
							string animAssetPath = MMD4MecanimImporter.GetAnimDataPath( vmdAssetPath );
							TextAsset animAsset = (TextAsset)AssetDatabase.LoadAssetAtPath( animAssetPath, typeof(TextAsset) );
							if( animAsset != null ) {
								if( model.animList == null ) {
									model.animList = new MMD4MecanimModel.Anim[1];
								} else {
									System.Array.Resize( ref model.animList, model.animList.Length + 1 );
								}
								
								MMD4MecanimModel.Anim anim = new MMD4MecanimModel.Anim();
								anim.animFile = animAsset;
								anim.animatorStateName = "Base Layer." + Path.GetFileNameWithoutExtension( animAsset.name ) + ".vmd";
								model.animList[model.animList.Length - 1] = anim;
							}
						}
					}
				} else {
					#if MMD4MECANIM_DEBUG
					Debug.LogWarning( "_MakeModel: Not found vmdAssetList. " + fbxAssetPath );
					#endif
				}
			}
		}
	}

	//------------------------------------------------------------------------------------

	static string[] _GetMMDAssetDependedPaths( string mmdAssetPath )
	{
		return new string[] {
			MMD4MecanimImporter.GetImporterAssetPath( mmdAssetPath ),
			MMD4MecanimImporter.GetImporterPropertyAssetPath( mmdAssetPath ),
		};
	}

	public static void _OnRegistImportedMMDAsset( string mmdAssetPath )
	{
		_importedMMDAssetDelay = 1;
		_importedMMDAssetPaths.Add( mmdAssetPath );
	}

	public static void _OnImportedMMDAsset( string mmdAssetPath )
	{
		if( mmdAssetPath == null ) {
			return;
		}

		// Create with .MMD4Mecanim.asset
		string importerAssetPath = MMD4MecanimImporter.GetImporterAssetPath( mmdAssetPath );
		if( !File.Exists( importerAssetPath ) ) {
			MMD4MecanimImporter importer = ScriptableObject.CreateInstance< MMD4MecanimImporter >();
			AssetDatabase.CreateAsset( importer, importerAssetPath );
			AssetDatabase.Refresh(); // for MAC(NFD)
			if( _cache_importers != null ) { // Sync to cache.
				_cache_importers.Add( importer );
			}
		}
	}

	public static void _OnMovedMMDAsset( string mmdAssetPath, string mmdAssetFromPath )
	{
		if( mmdAssetPath == null || mmdAssetFromPath == null ) {
			return;
		}

		// Move with .MMD4Mecanim.asset & .MMD4Mecanim.xml
		_MoveDependedAssets( _GetMMDAssetDependedPaths( mmdAssetPath ),
		                    _GetMMDAssetDependedPaths( mmdAssetFromPath ) );
	}

	public static void _OnDeletedMMDAsset( string mmdAssetPath )
	{
		if( mmdAssetPath == null ) {
			return;
		}

		// Delete with .MMD4Mecanim.asset & .MMD4Mecanim.xml
		string[] dependedPaths = _GetMMDAssetDependedPaths( mmdAssetPath );
		string importerAssetPath = dependedPaths[0];
		if( _cache_importers != null ) { // Sync to cache.
			for( int i = 0; i < _cache_importers.Count; ++i ) {
				if( _cache_importers[i] != null && AssetDatabase.GetAssetPath( _cache_importers[i] ) == importerAssetPath ) {
					_cache_importers.RemoveAt( i );
					break;
				}
			}
		}
		_DeleteDependedAssets( dependedPaths );
	}

	//------------------------------------------------------------------------------------

	static string[] _GetFBXAssetDependedPaths( string fbxAssetPath )
	{
		return new string[] {
			MMD4MecanimImporter.GetMMDModelPath( fbxAssetPath ), // .xml
			MMD4MecanimImporter.GetModelDataPath( fbxAssetPath ), // .model.bytes
			MMD4MecanimImporter.GetExtraDataPath( fbxAssetPath ), // .extra.bytes
			MMD4MecanimImporter.GetIndexDataPath( fbxAssetPath ), // .index.bytes
			MMD4MecanimImporter.GetVertexDataPath( fbxAssetPath ), // .vertex.bytes
		};
	}

	static List<MMD4MecanimImporter> _cache_importers;
	static List<MMD4MecanimImporter> _cache_GetImporters( string[] assetPaths )
	{
		if( _cache_importers == null ) {
			_cache_importers = new List<MMD4MecanimImporter>();
			if( assetPaths == null ) {
				assetPaths = AssetDatabase.GetAllAssetPaths();
			}
			if( assetPaths != null ) {
				foreach( string assetPath in assetPaths ) {
					if( assetPath.EndsWith( ".MMD4Mecanim.asset" ) ) {
						//MMD4MecanimImporter importer = AssetDatabase
						MMD4MecanimImporter importer = (MMD4MecanimImporter)AssetDatabase.LoadAssetAtPath( assetPath, typeof(MMD4MecanimImporter) );
						if( importer != null ) {
							importer.Setup();
							_cache_importers.Add( importer );
						}
					}
				}
			}
		} else { // Validation cache.
			for( int i = 0; i < _cache_importers.Count; ) {
				if( _cache_importers[i] != null ) {
					++i;
				} else {
					_cache_importers.RemoveAt( i );
				}
			}
		}
		
		return _cache_importers;
	}

	static List<MMD4MecanimImporter> _cache_GetImporters()
	{
		return _cache_GetImporters( null );
	}

	static MMD4MecanimImporter _GetImporterFromFBXAssetPath( string fbxAssetPath )
	{
		if( fbxAssetPath == null ) {
			return null;
		}
		
		if( _cache_importers != null ) {
			return _cache_GetImporterFromFBXAssetPath( fbxAssetPath );
		}

		string importerAssetPath = MMD4MecanimImporter.GetImporterAssetPath( fbxAssetPath );
		if( File.Exists( importerAssetPath ) ) {
			#if MMD4MECANIM_DEBUG
			Debug.LogWarning("_GetImporterFromFBXAssetPath: Use Directly." + fbxAssetPath );
			#endif
			MMD4MecanimImporter importer = (MMD4MecanimImporter)AssetDatabase.LoadAssetAtPath( importerAssetPath, typeof(MMD4MecanimImporter) );
			if( importer != null ) {
				importer.Setup();
			}
			return importer;
		}

		#if MMD4MECANIM_DEBUG
		Debug.LogWarning("_GetImporterFromFBXAssetPath: Use Cache." + fbxAssetPath );
		#endif
		return _cache_GetImporterFromFBXAssetPath( fbxAssetPath );
	}

	static MMD4MecanimImporter _cache_GetImporterFromFBXAssetPath( string fbxAssetPath )
	{
		if( fbxAssetPath == null ) {
			return null;
		}

		List<MMD4MecanimImporter> importers = _cache_GetImporters();
		foreach( MMD4MecanimImporter importer in importers )  {
			if( fbxAssetPath == importer.fbxAssetPath ) {
				return importer;
			}
		}

		return null;
	}

	public static void _OnRegistImportedFBXAsset( string fbxAssetPath )
	{
		_importedFBXAssetDelay = 1;
		_importedFBXAssetPaths.Add( fbxAssetPath );
	}

	public static void _OnImportedFBXAsset( string fbxAssetPath )
	{
		#if MMD4MECANIM_DEBUG
		Debug.LogWarning( "_OnImportedFBXAsset:" + fbxAssetPath );
		#endif

		if( fbxAssetPath == null ) {
			return;
		}

		MMD4MecanimImporter importer = _GetImporterFromFBXAssetPath( fbxAssetPath );
		if( importer != null ) {
			importer._OnImportedFBXAsset( fbxAssetPath );

			_ForceRefreshImportScale();
		} else {
			#if MMD4MECANIM_DEBUG
			Debug.LogWarning( "_GetImporterFromFBXAssetPath: Failed." + fbxAssetPath );
			#endif
		}
	}

	public static void _OnMovedFBXAsset( string fbxAssetPath, string fbxAssetFromPath )
	{
		if( fbxAssetPath == null ) {
			return;
		}

		_MoveDependedAssets( _GetFBXAssetDependedPaths( fbxAssetPath ),
		                    _GetFBXAssetDependedPaths( fbxAssetFromPath ) );

		MMD4MecanimImporter importer = _GetImporterFromFBXAssetPath( fbxAssetFromPath );
		if( importer != null ) {
			importer.fbxAssetPath = fbxAssetPath;
			importer.SavePMX2FBXConfig();
		}
	}

	public static void _OnDeletedFBXAsset( string fbxAssetPath )
	{
		if( fbxAssetPath == null ) {
			return;
		}

		_DeleteDependedAssets( _GetFBXAssetDependedPaths( fbxAssetPath ) );

		MMD4MecanimImporter importer = _GetImporterFromFBXAssetPath( fbxAssetPath );
		if( importer != null ) {
			importer.fbxAsset = null;
			importer.fbxAssetPath = null;
			importer.SavePMX2FBXConfig();
		}
	}
}
