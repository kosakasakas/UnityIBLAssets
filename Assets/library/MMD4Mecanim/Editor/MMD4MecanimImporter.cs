using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using IndexData = MMD4MecanimInternal.AuxData.IndexData;
using VertexData = MMD4MecanimInternal.AuxData.VertexData;
using MMD4Mecanim;

public partial class MMD4MecanimImporter : ScriptableObject
{
	private static readonly string[] toolbarTitlesBasic = new string[] {
		"PMX2FBX", "Material",
	};

	private static readonly string[] toolbarTitlesAdvanced = new string[] {
		"PMX2FBX", "Material", "Rig", "Animations",
	};

	public enum EditorViewPage
	{
		PMX2FBX,
		Material,
		Rig,
		Animations,
	}

	public class PMX2FBXProperty
	{
		public bool			viewAdvancedGlobalSettings;
		public bool			viewAdvancedGlobalSettingsIK;
		public bool			viewAdvancedBulletPhysics;
		
		public Object		pmxAsset;
		public List<Object>	vmdAssetList = new List<Object>();
	}

	public void _OnImportedFBXAsset( string fbxAssetPath )
	{
		Setup();

		if( this.fbxAsset == null ) {
			this.fbxAssetPath = null;
			this.fbxAsset = (GameObject)AssetDatabase.LoadAssetAtPath( fbxAssetPath, typeof(GameObject) );
			if( this.fbxAsset == null ) {
				Debug.LogError( "Nothing fbxAsset. " + fbxAssetPath );
				return;
			}
			this.fbxAssetPath = fbxAssetPath;
		}

		_PrepareDependency();
	}

	public GameObject		fbxAsset;
	public string			fbxAssetPath;

	[System.NonSerialized]
	public List<Material>	materialList;

	[System.NonSerialized]
	public MMDModel			mmdModel;
	[System.NonSerialized]
	public System.DateTime	mmdModelLastWriteTime;

	[System.NonSerialized]
	public TextAsset		indexAsset;
	[System.NonSerialized]
	public IndexData		indexData;
	[System.NonSerialized]
	public TextAsset		vertexAsset;
	[System.NonSerialized]
	public VertexData		vertexData;

	public EditorViewPage	editorViewPage;

	[System.NonSerialized]
	public PMX2FBXProperty	pmx2fbxProperty;
	[System.NonSerialized]
	public PMX2FBXConfig	pmx2fbxConfig;

	private bool			_prepareDependencyAtLeastOnce;

	private LicenseAgree	_licenseAgree;
	private string[]		_readmeFiles;
	private string[]		_readmeTexts;
	private bool			_isPiapro;
	private string[]		_comments;
	private Vector2			_readmeScrollView;
	private int				_readmeSelected;
	private bool			_isAgreedWeak;
	private bool			_isCheckedLicense;
	private bool			_isCheckedWarning;
	private bool			_isCheckedPiapro;

	public static bool		_overrideEditorStyle;

	void OnEnable()
	{
	}

	void OnDisable()
	{
		// Recompiled
		_UnlockEditorStyle();
	}

	public static void _UnlockEditorStyle()
	{
		if( _overrideEditorStyle ) {
			_overrideEditorStyle = false;
			try {
				EditorStyles.textField.fontSize = 0;
				EditorStyles.textField.wordWrap = false;
				EditorStyles.label.wordWrap = false;
			} catch( System.Exception ) {
			}
		}
	}

	bool _editorAdvancedMode {
		get {
			if( this.pmx2fbxConfig != null && this.pmx2fbxConfig.globalSettings != null ) {
				return this.pmx2fbxConfig.globalSettings.editorAdvancedMode;
			} else {
				return false;
			}
		}
		set {
			if( this.pmx2fbxConfig == null ) {
				this.pmx2fbxConfig = new PMX2FBXConfig();
			}
			if( this.pmx2fbxConfig.globalSettings == null ) {
				this.pmx2fbxConfig.globalSettings = new PMX2FBXConfig.GlobalSettings();
			}
			this.pmx2fbxConfig.globalSettings.editorAdvancedMode = value;
		}
	}

	public void OnInspectorGUI()
	{
		if( !Application.isPlaying ) {
			if( !_prepareDependencyAtLeastOnce ) {
				_prepareDependencyAtLeastOnce = true;
				_PrepareDependency();
			}
		}

		if( !Setup() ) {
			return;
		}

		if( this.pmx2fbxConfig != null ) {
			if( _licenseAgree == LicenseAgree.NotProcess ) {
				_licenseAgree = CheckLicenseAgree( ref _readmeFiles, ref _readmeTexts, ref _isPiapro );
				_comments = DecodeModelComments();
			}

			if( !_isAgreedWeak ) {
				_overrideEditorStyle = true;
				EditorStyles.textField.fontSize = 14;
				EditorStyles.textField.wordWrap = true;
				EditorStyles.label.wordWrap = true;

				string title = LicenseAgreeStrings[(int)_licenseAgree];
				EditorGUILayout.TextArea( title );

				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.BeginVertical();
				if( _readmeFiles != null ) {
					for( int i = 0; i < _readmeFiles.Length; ++i ) {
						string readmeFileName = System.IO.Path.GetFileName( _readmeFiles[i] );
						int selected = GUILayout.Toolbar((i == _readmeSelected) ? 0 : -1, new string[] { readmeFileName });
						if( _readmeSelected != i && selected == 0 ) {
							_readmeSelected = i;
						}
					}
				}
				EditorGUILayout.EndVertical();
				GUILayout.FlexibleSpace();
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.Separator();

				string readmeText = null;
				if( _readmeTexts != null && _readmeSelected < _readmeTexts.Length ) {
					readmeText = _readmeTexts[_readmeSelected];
				}

				_readmeScrollView = EditorGUILayout.BeginScrollView( _readmeScrollView );

				if( _comments != null ) {
					if( (int)Comment.ModelNameJp < _comments.Length ) {
						string modelNameJp = _comments[(int)Comment.ModelNameJp];
						if( modelNameJp != null ) {
							EditorGUILayout.TextArea( modelNameJp );
						}
					}
					if( (int)Comment.CommentJp < _comments.Length ) {
						string commentJp = _comments[(int)Comment.CommentJp];
						if( commentJp != null ) {
							EditorGUILayout.TextArea( commentJp );
						}
					}

					EditorGUILayout.Separator();
				}

				if( !string.IsNullOrEmpty( readmeText ) ) {
					EditorGUILayout.TextArea( readmeText );
				}

				{
					string str_LicenceWarningNotice = LicenceWarningNotice;
					string str_LicenceAgreedWarning = LicenceAgreedWarning;
					string str_LicenseWarningPiapro = LicenseWarningPiapro;
					string str_LicenceDeniedWarning = LicenceDeniedWarning;
					string str_LicenceDeniedNotice = LicenceDeniedNotice;
					if( Application.systemLanguage != SystemLanguage.Japanese ) {
						str_LicenceWarningNotice = LicenceWarningNotice_En;
						str_LicenceAgreedWarning = LicenceAgreedWarning_En;
						str_LicenseWarningPiapro = LicenseWarningPiapro_En;
						str_LicenceDeniedWarning = LicenceDeniedWarning_En;
						str_LicenceDeniedNotice = LicenceDeniedNotice_En;
					}

					EditorGUILayout.Separator();
					if( _licenseAgree != LicenseAgree.Denied ) {
						EditorGUILayout.SelectableLabel( str_LicenceWarningNotice );
						EditorGUILayout.Separator();
					}
					_isCheckedLicense = GUILayout.Toggle(_isCheckedLicense, str_LicenceAgreedWarning);
					bool licenseAgreeded = _isCheckedLicense;
					EditorGUILayout.Separator();
					if( _isPiapro ) {
						_isCheckedPiapro = GUILayout.Toggle(_isCheckedPiapro, str_LicenseWarningPiapro);
						EditorGUILayout.Separator();
						licenseAgreeded = licenseAgreeded && _isCheckedPiapro;
					}
					if( _licenseAgree == LicenseAgree.Denied ) {
						_isCheckedWarning = GUILayout.Toggle(_isCheckedWarning, str_LicenceDeniedWarning);
						EditorGUILayout.TextArea( str_LicenceDeniedNotice );
						EditorGUILayout.Separator();
						licenseAgreeded = licenseAgreeded && _isCheckedWarning;
					}
					GUI.enabled = licenseAgreeded;
					EditorGUILayout.BeginHorizontal();
					GUILayout.FlexibleSpace();
					bool isAgree = GUILayout.Button("\u540c\u610f\u3059\u308b", GUILayout.ExpandWidth(false));
					GUI.enabled = true;
					EditorGUILayout.EndHorizontal();
					if( isAgree ) {
						_isAgreedWeak = true;
					}
				}
				EditorGUILayout.EndScrollView();
				return;
			} else {
				_UnlockEditorStyle();
			}
		}

		bool editorAdvancedMode = this._editorAdvancedMode;
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		this._editorAdvancedMode = GUILayout.Toggle( this._editorAdvancedMode, "Advanced Mode" );
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if( editorAdvancedMode ) {
			this.editorViewPage = (EditorViewPage)GUILayout.Toolbar( (int)this.editorViewPage, toolbarTitlesAdvanced );
		} else {
			if( (int)this.editorViewPage >= toolbarTitlesBasic.Length ) {
				this.editorViewPage = (EditorViewPage)0;
			}
			this.editorViewPage = (EditorViewPage)GUILayout.Toolbar( (int)this.editorViewPage, toolbarTitlesBasic );
		}
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();

		EditorGUILayout.Separator();

		switch( this.editorViewPage ) {
		case EditorViewPage.PMX2FBX:
			_OnInspectorGUI_PMX2FBX();
			break;
		case EditorViewPage.Material:
			_OnInspectorGUI_Material();
			break;
		case EditorViewPage.Rig:
			_OnInspectorGUI_Rig();
			break;
		case EditorViewPage.Animations:
			_OnInspectorGUI_Animations();
			break;
		}
	}

	void _OnInspectorGUI_ShowFBXField()
	{
		GameObject fbxAsset = this.fbxAsset;
		fbxAsset = EditorGUILayout.ObjectField( (Object)fbxAsset, typeof(GameObject), false ) as GameObject;
		if( fbxAsset != null && fbxAsset != this.fbxAsset ) {
			string fbxAssetPath = AssetDatabase.GetAssetPath( fbxAsset );
			if( !MMD4MecanimEditorCommon.IsExtensionFBX( fbxAssetPath ) ) {
				fbxAsset = null;
			} else {
				this.fbxAsset = fbxAsset;
				this.fbxAssetPath = fbxAssetPath;
				this.materialList = null;

				this.mmdModel = null;
				this.mmdModelLastWriteTime = new System.DateTime();
				this.indexAsset = null;
				this.indexData = null;
				this.vertexAsset = null;
				this.vertexData = null;
				PrepareDependency();
			}
		}
	}

	//--------------------------------------------------------------------------------------------------------------------------------------------

	public static readonly string ScriptExtension = ".MMD4Mecanim.asset";

	//--------------------------------------------------------------------------------------------------------------------------------------------

	public static string GetAssetPath( Animator animator )
	{
		if( animator != null ) {
			return AssetDatabase.GetAssetPath( animator.avatar );
		}
		return null;
	}

	public static string GetAssetPath( MMD4MecanimModel model )
	{
		if( model != null ) {
			return GetAssetPath( model.gameObject.GetComponent< Animator >() );
		}
		return null;
	}

	//--------------------------------------------------------------------------------------------------------------------------------------------

	string _GetScriptAssetPath()
	{
		return AssetDatabase.GetAssetPath( this );
	}
	
	string _GetScriptAssetPathWithoutExtension()
	{
		return MMD4MecanimEditorCommon.GetPathWithoutExtension( _GetScriptAssetPath(), ScriptExtension.Length );
	}
	
	public static string GetPMX2FBXRootConfigPath()
	{
		Shader mmdlitShader = Shader.Find( "MMD4Mecanim/MMDLit" );
		if( mmdlitShader != null ) {
			string shaderAssetPath = AssetDatabase.GetAssetPath( mmdlitShader );
			string pmx2fbxConfigPath = Path.GetDirectoryName( Path.GetDirectoryName( shaderAssetPath ) )
										+ "/Editor/PMX2FBX/pmx2fbx.xml";
			
			if( File.Exists( pmx2fbxConfigPath ) ) {
				return pmx2fbxConfigPath;
			}
		}
		
		return null;
	}

	public static string GetPMX2FBXPath( bool useWineFlag )
	{
		Shader mmdlitShader = Shader.Find( "MMD4Mecanim/MMDLit" );
		if( mmdlitShader != null ) {
			string pmx2fbxExecutePath = "/Editor/PMX2FBX/pmx2fbx";
			if( Application.platform == RuntimePlatform.WindowsEditor || useWineFlag ) {
				pmx2fbxExecutePath = pmx2fbxExecutePath + ".exe";
			}
			
			string shaderAssetPath = AssetDatabase.GetAssetPath( mmdlitShader );
			string pmx2fbxPath = Path.GetDirectoryName( Path.GetDirectoryName( shaderAssetPath ) ) + pmx2fbxExecutePath;
			if( File.Exists( pmx2fbxPath ) ) {
				return pmx2fbxPath;
			}
		}
		
		return null;
	}

	public bool PrepareDependency()
	{
		if( this.isProcessing ) {
			return false;
		}

		_PrepareDependency();
		return true;
	}

	void _PrepareDependency()
	{
		if( this.isProcessing ) {
			return;
		}
		
		if( !Setup() ) {
			return;
		}

		// Check FBX.
		if( this.fbxAsset == null && this.pmx2fbxConfig.mmd4MecanimProperty != null ) {
			string fbxAssetPath = this.pmx2fbxConfig.mmd4MecanimProperty.fbxOutputPath;
			if( File.Exists( fbxAssetPath ) ) {
				#if MMD4MECANIM_DEBUG
				Debug.Log( "Load fbxAssetPath:" + fbxAssetPath );
				#endif
				this.fbxAsset = AssetDatabase.LoadAssetAtPath( fbxAssetPath, typeof(GameObject) ) as GameObject;
				this.fbxAssetPath = fbxAssetPath;
			}
		}
		
		// Check MMDModel.
		if( this.fbxAsset != null ) {
			string mmdModelPath = GetMMDModelPath( this.fbxAssetPath );
			if( File.Exists( mmdModelPath ) ) {
				if( this.mmdModel == null || this.mmdModelLastWriteTime != MMD4MecanimEditorCommon.GetLastWriteTime( mmdModelPath ) ) {
					#if MMD4MECANIM_DEBUG
					Debug.Log( "Load mmdModelPath:" + mmdModelPath );
					#endif
					this.mmdModel = GetMMDModel( mmdModelPath );
					this.mmdModelLastWriteTime = MMD4MecanimEditorCommon.GetLastWriteTime( mmdModelPath );
				} else {
					// No changed.
				}
			} else {
				this.mmdModel = null;
				this.mmdModelLastWriteTime = new System.DateTime();
			}
		} else {
			this.mmdModel = null;
			this.mmdModelLastWriteTime = new System.DateTime();
		}
		
		// Check Index / vertex Data.
		if( this.fbxAsset != null ) {
			if( this.indexAsset == null || this.indexData == null ) {
				string indexAssetPath = GetIndexDataPath( this.fbxAssetPath );
				if( File.Exists( indexAssetPath ) ) {
					#if MMD4MECANIM_DEBUG
					//Debug.Log( "Load indexAssetPath:" + indexAssetPath );
					#endif
					TextAsset indexAsset = AssetDatabase.LoadAssetAtPath( indexAssetPath, typeof(TextAsset) ) as TextAsset;
					if( indexAsset != null ) {
						this.indexData = MMD4MecanimInternal.AuxData.BuildIndexData( indexAsset );
						if( this.indexData != null ) {
							this.indexAsset = indexAsset;
						}
					}
				}
			}
			if( this.vertexAsset == null || this.vertexData == null ) {
				string vertexAssetPath = GetVertexDataPath( this.fbxAssetPath );
				if( File.Exists( vertexAssetPath ) ) {
					#if MMD4MECANIM_DEBUG
					//Debug.Log( "Load vertexAssetPath:" + vertexAssetPath );
					#endif
					TextAsset vertexAsset = AssetDatabase.LoadAssetAtPath( vertexAssetPath, typeof(TextAsset) ) as TextAsset;
					if( vertexAsset != null ) {
						this.vertexData = MMD4MecanimInternal.AuxData.BuildVertexData( vertexAsset );
						if( this.vertexData != null ) {
							this.vertexAsset = vertexAsset;
						}
					}
				}
			}
		} else {
			this.indexData = null;
			this.vertexData = null;
		}

		_CheckFBXMaterial();
	}

	public bool CheckFBXMaterialVertex()
	{
		if( this.isProcessing ) {
			return false;
		}
		
		_CheckFBXMaterialVertex();
		return true;
	}

	public void _CheckModelInScene( List<Animator> assetAnimators, List<string> assetPaths, List<bool> assetIsSkinned )
	{
		if( this.isProcessing || !Setup() ) {
			return;
		}

		if( string.IsNullOrEmpty( this.fbxAssetPath ) ) {
			return;
		}
		
		for( int i = 0; i < assetPaths.Count; ++i ) {
			string assetPath = assetPaths[i];
			if( !string.IsNullOrEmpty( assetPath ) && assetPath == this.fbxAssetPath ) {
				if( assetIsSkinned[i] ) {
					string modelDataPath = GetModelDataPath( assetPath );
					string indexDataPath = GetIndexDataPath( assetPath );
					string vertexDataPath = GetVertexDataPath( assetPath );
					if( File.Exists( modelDataPath ) && File.Exists( indexDataPath ) ) {
						TextAsset modelData = AssetDatabase.LoadAssetAtPath( modelDataPath, typeof(TextAsset) ) as TextAsset;
						TextAsset indexData = AssetDatabase.LoadAssetAtPath( indexDataPath, typeof(TextAsset) ) as TextAsset;
						TextAsset vertexData = AssetDatabase.LoadAssetAtPath( vertexDataPath, typeof(TextAsset) ) as TextAsset;
						_MakeModel( assetAnimators[i].gameObject, modelData, indexData, vertexData, assetIsSkinned[i] );
					}
				} else {
					string modelDataPath = GetModelDataPath( assetPath );
					if( File.Exists( modelDataPath ) ) {
						TextAsset modelData = AssetDatabase.LoadAssetAtPath( modelDataPath, typeof(TextAsset) ) as TextAsset;
						_MakeModel( assetAnimators[i].gameObject, modelData, null, null, assetIsSkinned[i] );
					}
				}
			}
		}
	}
	
	private void _MakeModel( GameObject modelGameObject, TextAsset modelData, TextAsset indexData, TextAsset vertexData, bool isSkinned )
	{
		if( modelData != null && (!isSkinned || indexData != null) ) {
			MMD4MecanimModel model = modelGameObject.AddComponent< MMD4MecanimModel >();
			model.importScale = this.fbxAssertImportScale;
			model.modelFile = modelData;
			model.indexFile = indexData;
			model.vertexFile = vertexData; // Accept null.

			// Add Animations.(Optional)
			if( this.pmx2fbxProperty.vmdAssetList != null ) {
				foreach( Object vmdAsset in this.pmx2fbxProperty.vmdAssetList ) {
					string vmdAssetPath = AssetDatabase.GetAssetPath( vmdAsset );
					if( !string.IsNullOrEmpty( vmdAssetPath ) ) {
						string animAssetPath = GetAnimDataPath( vmdAssetPath );
						TextAsset animAsset = AssetDatabase.LoadAssetAtPath( animAssetPath, typeof(TextAsset) ) as TextAsset;
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
			}
		}
	}

	public bool Setup()
	{
		if( this.pmx2fbxProperty == null || this.pmx2fbxConfig == null ) {
			return _Setup();
		}

		return true;
	}

	public bool SetupWithReload()
	{
		return _Setup();
	}

	bool _Setup()
	{
		string scriptAssetPathWithoutExtension = _GetScriptAssetPathWithoutExtension();
		if( string.IsNullOrEmpty(scriptAssetPathWithoutExtension) ) {
			return false;
		}
		
		if( this.pmx2fbxProperty == null ) {
			this.pmx2fbxProperty = new PMX2FBXProperty();
		}
		
		/* Load config */
		if( this.pmx2fbxConfig == null ) {
			this.pmx2fbxConfig = GetPMX2FBXConfig( ( scriptAssetPathWithoutExtension + ".MMD4Mecanim.xml" ).Normalize(NormalizationForm.FormC) );
			if( this.pmx2fbxConfig == null ) { 
				this.pmx2fbxConfig = GetPMX2FBXConfig( GetPMX2FBXRootConfigPath() );
				if( this.pmx2fbxConfig == null ) {
					this.pmx2fbxConfig = new PMX2FBXConfig();
				} else {
					this.pmx2fbxConfig.renameList = null;
				}
			}

			/* Binding assets.( On loading only. ) */
			if( this.pmx2fbxConfig != null && this.pmx2fbxConfig.mmd4MecanimProperty != null ) {
				var mmd4MecanimProperty = this.pmx2fbxConfig.mmd4MecanimProperty;
				// Load PMX/PMD Asset ( Path )
				if( pmx2fbxProperty.pmxAsset == null ) {
					string pmxAssetPath = mmd4MecanimProperty.pmxAssetPath;
					if( System.IO.File.Exists( pmxAssetPath ) ) {
						pmx2fbxProperty.pmxAsset = AssetDatabase.LoadAssetAtPath( pmxAssetPath, typeof(Object) );
					}
				}
				// Load VMD
				this.pmx2fbxProperty.vmdAssetList = new List<Object>();
				if( mmd4MecanimProperty.vmdAssetPathList != null ) {
					for( int i = 0; i < mmd4MecanimProperty.vmdAssetPathList.Count; ++i ) {
						{
							string vmdAssetPath = mmd4MecanimProperty.vmdAssetPathList[i];
							Object vmdAsset = AssetDatabase.LoadAssetAtPath( vmdAssetPath, typeof(Object) );
							if( vmdAsset != null ) {
								pmx2fbxProperty.vmdAssetList.Add( vmdAsset );
							}
						}
					}
				}
				// Load FBX
				if( this.fbxAsset == null ) {
					{
						string fbxAssetPath = mmd4MecanimProperty.fbxAssetPath;
						if( !string.IsNullOrEmpty( fbxAssetPath ) && File.Exists( fbxAssetPath ) ) {
							this.fbxAsset = AssetDatabase.LoadAssetAtPath( fbxAssetPath, typeof(GameObject) ) as GameObject;
							this.fbxAssetPath = fbxAssetPath;
						}
					}
					if( this.fbxAsset == null ) {
						string fbxAssetPath = mmd4MecanimProperty.fbxOutputPath;
						if( !string.IsNullOrEmpty( fbxAssetPath ) && File.Exists( fbxAssetPath ) ) {
							this.fbxAsset = AssetDatabase.LoadAssetAtPath( fbxAssetPath, typeof(GameObject) ) as GameObject;
							this.fbxAssetPath = fbxAssetPath;
						}
					}
				}
			}
		}
		
		if( this.pmx2fbxConfig == null ) {
			this.pmx2fbxConfig = new PMX2FBXConfig();
		}
		if( this.pmx2fbxConfig.globalSettings == null ) {
			this.pmx2fbxConfig.globalSettings = new PMX2FBXConfig.GlobalSettings();
		}
		if( this.pmx2fbxConfig.bulletPhysics == null ) {
			this.pmx2fbxConfig.bulletPhysics = new PMX2FBXConfig.BulletPhysics();
		}
		if( this.pmx2fbxConfig.renameList == null ) {
			this.pmx2fbxConfig.renameList = new List<PMX2FBXConfig.Rename>();
		}
		if( this.pmx2fbxConfig.freezeRigidBodyList == null ) {
			this.pmx2fbxConfig.freezeRigidBodyList = new List<PMX2FBXConfig.FreezeRigidBody>();
		}
		if( this.pmx2fbxConfig.freezeMotionList == null ) {
			this.pmx2fbxConfig.freezeMotionList = new List<PMX2FBXConfig.FreezeMotion>();
		}
		if( this.pmx2fbxConfig.mmd4MecanimProperty == null ) {
			this.pmx2fbxConfig.mmd4MecanimProperty = new PMX2FBXConfig.MMD4MecanimProperty();
		}
		if( string.IsNullOrEmpty( this.pmx2fbxConfig.mmd4MecanimProperty.fbxOutputPath ) ) {
			this.pmx2fbxConfig.mmd4MecanimProperty.fbxOutputPath = (scriptAssetPathWithoutExtension + ".fbx").Normalize(NormalizationForm.FormC);
		}

		if( this.pmx2fbxProperty.pmxAsset == null ) {
			string pmxAssetPath = scriptAssetPathWithoutExtension + ".pmx";
			if( File.Exists( pmxAssetPath ) ) {
				this.pmx2fbxProperty.pmxAsset = AssetDatabase.LoadAssetAtPath( pmxAssetPath, typeof(Object) );
			}
		}
		if( this.pmx2fbxProperty.pmxAsset == null ) {
			string pmxAssetPath = scriptAssetPathWithoutExtension + ".pmd";
			if( File.Exists( pmxAssetPath ) ) {
				this.pmx2fbxProperty.pmxAsset = AssetDatabase.LoadAssetAtPath( pmxAssetPath, typeof(Object) );
			}
		}
		if( this.fbxAsset == null ) {
			string fbxAssetPath = scriptAssetPathWithoutExtension + ".fbx";
			if( !string.IsNullOrEmpty( fbxAssetPath ) && File.Exists( fbxAssetPath ) ) {
				this.fbxAsset = AssetDatabase.LoadAssetAtPath( fbxAssetPath, typeof(GameObject) ) as GameObject;
				this.fbxAssetPath = fbxAssetPath;
			}
		}
		if( this.mmdModel == null ) {
			if( this.fbxAsset != null ) {
				string mmdModelPath = GetMMDModelPath( this.fbxAssetPath );
				if( !string.IsNullOrEmpty( mmdModelPath ) && File.Exists( mmdModelPath ) ) {
					this.mmdModel = GetMMDModel( mmdModelPath );
					this.mmdModelLastWriteTime = MMD4MecanimEditorCommon.GetLastWriteTime( mmdModelPath );
				}
			}
		}
		
		// Wine
		if( string.IsNullOrEmpty( this.pmx2fbxConfig.mmd4MecanimProperty.winePath ) ) {
			this.pmx2fbxConfig.mmd4MecanimProperty.wine = PMX2FBXConfig.Wine.Manual;
			this.pmx2fbxConfig.mmd4MecanimProperty.winePath = WinePaths[(int)PMX2FBXConfig.Wine.Manual];
			for( int i = 0; i < WinePaths.Length; ++i ) {
				if( File.Exists( WinePaths[i] ) ) {
					this.pmx2fbxConfig.mmd4MecanimProperty.wine = (PMX2FBXConfig.Wine)i;
					this.pmx2fbxConfig.mmd4MecanimProperty.winePath = WinePaths[i];
					break;
				}
			}
		}

		return true;
	}
	
	public void SavePMX2FBXConfig()
	{
		if( this.pmx2fbxConfig == null || this.pmx2fbxProperty == null ) {
			Debug.LogError("");
			return;
		}
		
		string scriptAssetPathWithoutExtension = _GetScriptAssetPathWithoutExtension();
		if( string.IsNullOrEmpty(scriptAssetPathWithoutExtension) ) {
			Debug.LogWarning( "Not found script." );
			return;
		}
		
		string pmx2fbxConfigPath = (scriptAssetPathWithoutExtension + ".MMD4Mecanim.xml").Normalize(NormalizationForm.FormC);
		
		if( this.pmx2fbxConfig.mmd4MecanimProperty != null ) {
			if( this.pmx2fbxProperty.pmxAsset != null ) {
				this.pmx2fbxConfig.mmd4MecanimProperty.pmxAssetPath = AssetDatabase.GetAssetPath( this.pmx2fbxProperty.pmxAsset );
			} else {
				this.pmx2fbxConfig.mmd4MecanimProperty.pmxAssetPath = null;
			}
			
			if( this.pmx2fbxProperty.vmdAssetList != null ) {
				this.pmx2fbxConfig.mmd4MecanimProperty.vmdAssetPathList = new List<string>();
				foreach( Object vmdAsset in this.pmx2fbxProperty.vmdAssetList ) {
					this.pmx2fbxConfig.mmd4MecanimProperty.vmdAssetPathList.Add( AssetDatabase.GetAssetPath( vmdAsset ) );
				}
			} else {
				this.pmx2fbxConfig.mmd4MecanimProperty.vmdAssetPathList = null;
			}
			
			if( this.fbxAsset != null ) {
				this.pmx2fbxConfig.mmd4MecanimProperty.fbxAssetPath = AssetDatabase.GetAssetPath( this.fbxAsset );
			} else {
				this.pmx2fbxConfig.mmd4MecanimProperty.fbxAssetPath = null;
			}
		}
		
		WritePMX2FBXConfig( pmx2fbxConfigPath, this.pmx2fbxConfig );
		AssetDatabase.ImportAsset( pmx2fbxConfigPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport );
		AssetDatabase.Refresh(); // for MAC(NFD)
	}
	
	public volatile static int _pmx2fbxProcessingCount;
	private volatile System.Diagnostics.Process _pmx2fbxProcess;

	public bool isProcessing
	{
		get { return _pmx2fbxProcess != null || _pmx2fbxProcessingCount > 0; }
	}

	public void ProcessPMX2FBX()
	{
		if( this.pmx2fbxConfig == null ||
			this.pmx2fbxConfig.mmd4MecanimProperty == null ||
			this.pmx2fbxProperty == null ) {
			Debug.LogError("");
			return;
		}
		
		bool useWineFlag =  this.pmx2fbxConfig.mmd4MecanimProperty.useWineFlag;
		string pmx2fbxPath = GetPMX2FBXPath(useWineFlag );
		if( pmx2fbxPath == null ) {
			Debug.LogError("");
			return;
		}
		
		if( _pmx2fbxProcess != null || _pmx2fbxProcessingCount > 0 ) {
			Debug.LogWarning( "Already processing pmx2fbx. Please wait." );
			return;
		}
		
		string scriptAssetPathWithoutExtension = _GetScriptAssetPathWithoutExtension();
		if( string.IsNullOrEmpty(scriptAssetPathWithoutExtension) ) {
			Debug.LogWarning( "Not found script." );
			return;
		}
		
		string pmx2fbxConfigPath = (scriptAssetPathWithoutExtension + ".MMD4Mecanim.xml").Normalize(NormalizationForm.FormC);
		
		string pmxAssetPath = this.pmx2fbxConfig.mmd4MecanimProperty.pmxAssetPath;
		if( string.IsNullOrEmpty( pmxAssetPath ) ) {
			Debug.LogError("PMX/PMD Path is null.");
			return;
		}
		
		string basePath = Application.dataPath;
		if( basePath.EndsWith( "Assets" ) ) {
			basePath = Path.GetDirectoryName( basePath );
		}

		System.Text.StringBuilder arguments = new System.Text.StringBuilder();
		
		if( Application.platform == RuntimePlatform.WindowsEditor || !useWineFlag ) {
			// Nothing.
		} else {
			arguments.Append( "\"" );
			arguments.Append( basePath + "/" + pmx2fbxPath );
			arguments.Append( "\" " );
		}
		
		arguments.Append( "-o \"" );
		arguments.Append( basePath + "/" + this.pmx2fbxConfig.mmd4MecanimProperty.fbxOutputPath );
		arguments.Append( "\" -conf \"" );
		arguments.Append( basePath + "/" + pmx2fbxConfigPath );
		arguments.Append( "\" \"" );
		arguments.Append( basePath + "/" + this.pmx2fbxConfig.mmd4MecanimProperty.pmxAssetPath );
		arguments.Append( "\"" );
		
		if( this.pmx2fbxConfig.mmd4MecanimProperty.vmdAssetPathList != null ) {
			foreach( string vmdAssetPath in this.pmx2fbxConfig.mmd4MecanimProperty.vmdAssetPathList ) {
				arguments.Append( " \"" );
				arguments.Append( basePath + "/" + vmdAssetPath );
				arguments.Append( "\"" );
			}
		}
		
		_pmx2fbxProcess = new System.Diagnostics.Process();
		++_pmx2fbxProcessingCount;
		
		if( Application.platform == RuntimePlatform.WindowsEditor || !useWineFlag ) {
			_pmx2fbxProcess.StartInfo.FileName = basePath + "/" + pmx2fbxPath;
		} else {
			string winePath = WinePaths[(int)this.pmx2fbxConfig.mmd4MecanimProperty.wine];
			if( this.pmx2fbxConfig.mmd4MecanimProperty.wine == PMX2FBXConfig.Wine.Manual ) {
				winePath = this.pmx2fbxConfig.mmd4MecanimProperty.winePath;
			}
			_pmx2fbxProcess.StartInfo.FileName = winePath;
		}
		_pmx2fbxProcess.StartInfo.Arguments = arguments.ToString();
        _pmx2fbxProcess.EnableRaisingEvents = true;
        _pmx2fbxProcess.Exited += _pmx2fbx_OnExited;
		
		if( !_pmx2fbxProcess.Start() ) {
			_pmx2fbxProcess.Dispose();
			_pmx2fbxProcess = null;
		}
	}
	
	void _pmx2fbx_OnExited(object sender, System.EventArgs e)
	{
		Debug.Log( "Processed pmx2fbx." );
		_pmx2fbxProcess.Dispose();
		_pmx2fbxProcess = null;
		if( --_pmx2fbxProcessingCount < 0 ) {
			_pmx2fbxProcessingCount = 0;
		}

		MMD4MecanimImporterEditor._isProcessedPMX2FBX = true; // for MAC
	}

	//----------------------------------------------------------------------------------------------------------------

	public float fbxAssertImportScale
	{
		get {
			if( !string.IsNullOrEmpty( this.fbxAssetPath ) ) {
				ModelImporter modelImporter = (ModelImporter)ModelImporter.GetAtPath( this.fbxAssetPath );
				if( modelImporter != null ) {
					return MMD4MecanimEditorCommon.GetModelImportScale( modelImporter );
				} else {
					Debug.LogWarning( "ModelImporter is not found. " + this.fbxAssetPath );
				}
			}

			return 0.0f;
		}
	}

	//----------------------------------------------------------------------------------------------------------------
	
	public static readonly string[] ReadmeExtNames = new string[] {
		".txt",
		".htm",
		".html",
	};
	public static readonly string[] ReadmeFileNames_StartsWith = new string[] {
		"readme",
		"read",
	};
	public static readonly string[] ReadmeFileNames_Contains_Japanese = new string[] {
		"\u30e2\u30c7\u30eb\u5229\u7528\u306e\u30ac\u30a4\u30c9\u30e9\u30a4\u30f3",	// mo-de-ru-RI-YO-no-ga-i-do-ra-i-n
		"\u304a\u8aad\u307f\u304f\u3060\u3055\u3044",	// o-YO-mi-ku-da-sa-i
		"\u3088\u3093\u3067\u306d",						// yo-n-de-ne
		"\u3088\u3093\u3067",							// yo-n-de
		"\u8aac\u660e",									// SETSU-MEI
		"\u8aad",										// YOMU
		"\u308a\u30fc\u3069\u307f\u30fc",				// ri-i-do-mi-i
		"\u30ea\u30fc\u30c9\u30df\u30fc",				// ri-i-do-mi-i(Kana)
		"\u308c\u3069\u3081",							// re-do-me
		"\u308a\u3069\u307f",							// ri-do-mi
		"\u308c\u3042\u3069\u3081",						// re-a-do-me
		"\u5229\u7528\u898f\u7d04",						// RI-YOU-KI-YAKU
		"\u5229\u7528",									// RI-YOU
		"\u4f7f\u7528",									// SHI-YOU
	};

	public static string[] FindReadmeFiles( string assetPath )
	{
		if( string.IsNullOrEmpty(assetPath) ) {
			Debug.LogError("");
			return null; // Error.
		}

		string directoryName = System.IO.Path.GetDirectoryName( assetPath );
		if( string.IsNullOrEmpty(directoryName) ) {
			Debug.LogError("");
			return null; // Error.
		}
		
		string[] files = System.IO.Directory.GetFiles( directoryName );
		if( files == null ) {
			Debug.LogError("");
			return null; // Error.
		}
		
		List<string> readmeFileNames = new List<string>();
		List<string> readmeFileNames_Low = new List<string>();
		List<string> readmeFileNames_VeryLow = new List<string>();
		try {
			foreach( string file in files ) {
				bool foundAnythingExt = false;
				string ext = System.IO.Path.GetExtension( file );
				if( string.IsNullOrEmpty(ext) ) {
					continue;
				}
				ext = ext.ToLower();
				for( int i = 0; i < ReadmeExtNames.Length; ++i ) {
					if( ext == ReadmeExtNames[i] ) {
						foundAnythingExt = true;
						break;
					}
				}
				if( !foundAnythingExt ) {
					continue; // Skip.
				}
				
				string fileName = System.IO.Path.GetFileNameWithoutExtension( file );
				
				bool foundAnything = false;
				for( int i = 0; i < ReadmeFileNames_Contains_Japanese.Length; ++i ) {
					if( fileName.Contains( ReadmeFileNames_Contains_Japanese[i] ) ) {
						readmeFileNames.Add( file );
						foundAnything = true;
						break;
					}
				}
				if( foundAnything ) {
					continue;
				}
				
				string fileNameEn = fileName.ToLower();
				for( int i = 0; i < ReadmeFileNames_StartsWith.Length; ++i ) {
					if( fileNameEn.StartsWith( ReadmeFileNames_StartsWith[i] ) ) {
						if( fileNameEn.Contains( "english" ) )  {
							readmeFileNames_Low.Add( file );
						} else {
							readmeFileNames_Low.Insert( 0, file );
						}
						foundAnything = true;
						break;
					}
				}
				if( foundAnything ) {
					continue;
				}

				// Memo: Force adding .txt/.html file.(Selectable)
				readmeFileNames_VeryLow.Add( file );
			}
			
			for( int i = 0; i < readmeFileNames_Low.Count; ++i ) {
				readmeFileNames.Add( readmeFileNames_Low[i] );
			}
			for( int i = 0; i < readmeFileNames_VeryLow.Count; ++i ) {
				readmeFileNames.Add( readmeFileNames_VeryLow[i] );
			}
		} catch( System.Exception ) {
			readmeFileNames.Clear();
			readmeFileNames_Low.Clear();
		}
		
		return readmeFileNames.ToArray();
	}

	public string[] FindReadmeFiles()
	{
		if( this.pmx2fbxProperty == null || this.pmx2fbxProperty.pmxAsset == null ) {
			Debug.LogError("");
			return null; // Error.
		}

		string assetPath = AssetDatabase.GetAssetPath( this.pmx2fbxProperty.pmxAsset );
		if( string.IsNullOrEmpty(assetPath) ) {
			Debug.LogError("");
			return null; // Error.
		}

		string[] files = FindReadmeFiles( assetPath );
		if( files != null ) {
			for( int i = 0; i < files.Length; ++i ) {
				try {
					string text = _ReadReadmeText( files[i] );
					if( !string.IsNullOrEmpty( text ) ) {
						if( text.Contains( LicenceImportantString ) ) {
							return files;
						}
					}
				} catch( System.Exception ) {
				}
			}
		}

		string[] parentFiles = FindReadmeFiles( Path.GetDirectoryName( assetPath ) );
		if( parentFiles == null || parentFiles.Length == 0 ) {
			return files;
		}

		if( files != null ) {
			string[] containFiles = new string[parentFiles.Length + files.Length];
			System.Array.Copy( parentFiles, 0, containFiles, 0, parentFiles.Length );
			System.Array.Copy( files, 0, containFiles, parentFiles.Length, files.Length );
			return containFiles;
		} else {
			return parentFiles;
		}
	}

	public static readonly string LicenceImportantString = "\u5229\u7528\u898f\u7d04"; // RI-YOU-KI-YAKU

	public enum LicenseAgree {
		NotProcess,
		Error,
		Unknown,
		Denied,
		Warning,
	}

	public static readonly string[] LicenseAgreeStrings = new string[] {
		"",
		"\u30e9\u30a4\u30bb\u30f3\u30b9\u72b6\u6cc1\u3092\u78ba\u8a8d\u3067\u304d\u307e\u305b\u3093\u3067\u3057\u305f\u3002",
		"\u5229\u7528\u898f\u7d04\u304c\u898b\u3064\u304b\u308a\u307e\u305b\u3093\u3067\u3057\u305f\u3002\u5229\u7528\u898f\u7d04\u3092\u3054\u78ba\u8a8d\u304f\u3060\u3055\u3044\u3002",
		"\u5229\u7528\u898f\u7d04\u3092\u3054\u78ba\u8a8d\u304f\u3060\u3055\u3044\u3002",
		"\u5229\u7528\u898f\u7d04\u3092\u3054\u78ba\u8a8d\u304f\u3060\u3055\u3044\u3002",
	};

	public static readonly string[] LicenseDeniedStrings = new string[] {
		"\uff2d\uff2d\uff24\u53ca\u3073\uff2d\uff2d\uff2d\u4ee5\u5916\u306e\u30a2\u30d7\u30ea\u30b1\u30fc\u30b7\u30e7\u30f3\u3067\u51fa\u529b\u3057\u305f\u753b\u50cf\u3001\u6620\u50cf\u306e\u516c\u958b\u306f\u539f\u5247\u3068\u3057\u3066\u4e0d\u53ef\u3067\u3059",
		"\uff2d\uff2d\uff24\u53ca\u3073\uff2d\uff2d\uff2d\u4ee5\u5916\u306e\u30a2\u30d7\u30ea\u30b1\u30fc\u30b7\u30e7\u30f3\u306b\u3088\u3063\u3066\u51fa\u529b\u3057\u305f\u753b\u50cf\u3001\u6620\u50cf\u306e\u516c\u958b\u306f\u7981\u6b62\u3067\u3059",
		"(\u81ea\u4f5c\u30b2\u30fc\u30e0\u3084\u30bd\u30d5\u30c8\u30a6\u30a7\u30a2\u7b49)\u3067\u4f7f\u7528\u3059\u308b\u3053\u3068\u306f\u51fa\u6765\u307e\u305b\u3093",
	};

	public static readonly string[] LicensePiaproStrings = new string[] {
		"\u30d4\u30a2\u30d7\u30ed",
		"piapro",
	};

	public static readonly string LicenceAgreedWarning = "\u5229\u7528\u898f\u7d04\u306b\u540c\u610f\u306e\u4e0a\u3001\u3054\u4f7f\u7528\u304f\u3060\u3055\u3044\u3002";
	public static readonly string LicenceWarningNotice = "\u516c\u958b\u30fb\u914d\u5e03\u3092\u76ee\u7684\u3068\u3059\u308b\u30b2\u30fc\u30e0\u53ca\u3073\u30b3\u30f3\u30c6\u30f3\u30c4\u3067\u306e\u5229\u7528\u306f\u3001\u660e\u793a\u7684\u306b\u8a31\u53ef\u3055\u308c\u3066\u3044\u308b\u5834\u5408\u3092\u9664\u3044\u3066\u3001\u5236\u4f5c\u3055\u308c\u3066\u3044\u308b\u65b9\u306b\u554f\u3044\u5408\u308f\u305b\u307e\u3057\u3087\u3046\u3002";
	public static readonly string LicenceDeniedWarning = "\u79c1\u7684\u5229\u7528\u306e\u7bc4\u56f2\u5185\u3067\u3001\u3054\u4f7f\u7528\u304f\u3060\u3055\u3044\u3002";
	public static readonly string LicenceDeniedNotice = "\u753b\u50cf\u3001\u6620\u50cf\u306e\u516c\u958b\u306f\u539f\u5247\u3068\u3057\u3066\u4e0d\u53ef\u3067\u3059\u3002";
	public static readonly string LicenseWarningPiapro = "PCL\u306e\u9075\u5b88\u3092\u304a\u9858\u3044\u3057\u307e\u3059\u3002\n\u516c\u5e8f\u826f\u4fd7\u306b\u53cd\u3059\u308b\u8868\u73fe\u306f\u7981\u6b62\u3055\u308c\u3066\u3044\u307e\u3059\u3002\nhttp://piapro.jp/license/pcl";

	public static readonly string LicenceAgreedWarning_En = "You must agree to these Terms of Use before using the model/motion.";
	public static readonly string LicenceWarningNotice_En = "If it is intended to distribute and games, need a permit creators.";
	public static readonly string LicenceDeniedWarning_En = "Within the scope of private use, please use.";
	public static readonly string LicenceDeniedNotice_En = "Public image, the image is impossible in principle.";
	public static readonly string LicenseWarningPiapro_En = "These existing artworks include copyrights of the creators who made them.\nSee more CC BY-NC.\nhttp://piapro.net/en_for_creators.html";

	// Reference: http://dobon.net/vb/dotnet/string/detectcode.html
	public static System.Text.Encoding GetCode(byte[] bytes)
	{
		const byte bEscape = 0x1B;
		const byte bAt = 0x40;
		const byte bDollar = 0x24;
		const byte bAnd = 0x26;
		const byte bOpen = 0x28; //'('
		const byte bB = 0x42;
		const byte bD = 0x44;
		const byte bJ = 0x4A;
		const byte bI = 0x49;
		
		int len = bytes.Length;
		byte b1, b2, b3, b4;
		
		bool isBinary = false;
		for (int i = 0; i < len; i++)
		{
			b1 = bytes[i];
			if (b1 <= 0x06 || b1 == 0x7F || b1 == 0xFF)
			{
				//'binary'
				isBinary = true;
				if (b1 == 0x00 && i < len - 1 && bytes[i + 1] <= 0x7F)
				{
					//smells like raw unicode
					return System.Text.Encoding.Unicode;
				}
			}
		}
		if (isBinary)
		{
			return null;
		}
		
		//not Japanese
		bool notJapanese = true;
		for (int i = 0; i < len; i++)
		{
			b1 = bytes[i];
			if (b1 == bEscape || 0x80 <= b1)
			{
				notJapanese = false;
				break;
			}
		}
		if (notJapanese)
		{
			return System.Text.Encoding.ASCII;
		}
		
		for (int i = 0; i < len - 2; i++)
		{
			b1 = bytes[i];
			b2 = bytes[i + 1];
			b3 = bytes[i + 2];
			
			if (b1 == bEscape)
			{
				if (b2 == bDollar && b3 == bAt)
				{
					//JIS_0208 1978
					//JIS
					return System.Text.Encoding.GetEncoding(50220);
				}
				else if (b2 == bDollar && b3 == bB)
				{
					//JIS_0208 1983
					//JIS
					return System.Text.Encoding.GetEncoding(50220);
				}
				else if (b2 == bOpen && (b3 == bB || b3 == bJ))
				{
					//JIS_ASC
					//JIS
					return System.Text.Encoding.GetEncoding(50220);
				}
				else if (b2 == bOpen && b3 == bI)
				{
					//JIS_KANA
					//JIS
					return System.Text.Encoding.GetEncoding(50220);
				}
				if (i < len - 3)
				{
					b4 = bytes[i + 3];
					if (b2 == bDollar && b3 == bOpen && b4 == bD)
					{
						//JIS_0212
						//JIS
						return System.Text.Encoding.GetEncoding(50220);
					}
					if (i < len - 5 &&
					    b2 == bAnd && b3 == bAt && b4 == bEscape &&
					    bytes[i + 4] == bDollar && bytes[i + 5] == bB)
					{
						//JIS_0208 1990
						//JIS
						return System.Text.Encoding.GetEncoding(50220);
					}
				}
			}
		}
		
		int sjis = 0;
		int euc = 0;
		int utf8 = 0;
		for (int i = 0; i < len - 1; i++)
		{
			b1 = bytes[i];
			b2 = bytes[i + 1];
			if (((0x81 <= b1 && b1 <= 0x9F) || (0xE0 <= b1 && b1 <= 0xFC)) &&
			    ((0x40 <= b2 && b2 <= 0x7E) || (0x80 <= b2 && b2 <= 0xFC)))
			{
				//SJIS_C
				sjis += 2;
				i++;
			}
		}
		for (int i = 0; i < len - 1; i++)
		{
			b1 = bytes[i];
			b2 = bytes[i + 1];
			if (((0xA1 <= b1 && b1 <= 0xFE) && (0xA1 <= b2 && b2 <= 0xFE)) ||
			    (b1 == 0x8E && (0xA1 <= b2 && b2 <= 0xDF)))
			{
				//EUC_C
				//EUC_KANA
				euc += 2;
				i++;
			}
			else if (i < len - 2)
			{
				b3 = bytes[i + 2];
				if (b1 == 0x8F && (0xA1 <= b2 && b2 <= 0xFE) &&
				    (0xA1 <= b3 && b3 <= 0xFE))
				{
					//EUC_0212
					euc += 3;
					i += 2;
				}
			}
		}
		for (int i = 0; i < len - 1; i++)
		{
			b1 = bytes[i];
			b2 = bytes[i + 1];
			if ((0xC0 <= b1 && b1 <= 0xDF) && (0x80 <= b2 && b2 <= 0xBF))
			{
				//UTF8
				utf8 += 2;
				i++;
			}
			else if (i < len - 2)
			{
				b3 = bytes[i + 2];
				if ((0xE0 <= b1 && b1 <= 0xEF) && (0x80 <= b2 && b2 <= 0xBF) &&
				    (0x80 <= b3 && b3 <= 0xBF))
				{
					//UTF8
					utf8 += 3;
					i += 2;
				}
			}
		}

		if (euc > sjis && euc > utf8) { //EUC
			return System.Text.Encoding.GetEncoding(51932);
		} else if (sjis > euc && sjis > utf8) { //SJIS
			return System.Text.Encoding.GetEncoding(932);
		} else if (utf8 > euc && utf8 > sjis) { //UTF8
			return System.Text.Encoding.UTF8;
		}
		
		return null;
	}

	public LicenseAgree CheckLicenseAgree( ref string[] readmeFiles, ref string[] readmeTexts, ref bool isPiapro )
	{
		readmeFiles = FindReadmeFiles();
		readmeTexts = null;
		return CheckLicenseAgree( readmeFiles, ref readmeTexts, ref isPiapro );
	}

	private static string _ReadReadmeText( string readmeFile )
	{
		string readmeText = null;

		if( string.IsNullOrEmpty( readmeText ) ) {
			try {
				byte[] readmeBytes = System.IO.File.ReadAllBytes( readmeFile );
				if( readmeBytes != null ) {
					System.Text.Encoding enc = GetCode( readmeBytes );
					if( enc != null ) {
						readmeText = enc.GetString( readmeBytes );
					}
				}
			} catch( System.Exception ) {
				readmeText = null;
			}
		}

		// Safety, but unstable.
		if( string.IsNullOrEmpty( readmeText ) ) {
			try {
				readmeText = System.IO.File.ReadAllText( readmeFile );
			} catch( System.Exception ) {
				readmeText = null;
			}
		}

		return readmeText;
	}

	static void _Swap( string[] a, int lhs, int rhs )
	{
		string t = a[lhs];
		a[lhs] = a[rhs];
		a[rhs] = t;
	}

	public static LicenseAgree CheckLicenseAgree( string[] readmeFiles, ref string[] readmeTexts, ref bool isPiapro )
	{
		isPiapro = false;

		readmeTexts = null;
		if( readmeFiles == null ) {
			return LicenseAgree.Error;
		}
		readmeTexts = new string[readmeFiles.Length];
		if( readmeFiles.Length == 0 ) {
			return LicenseAgree.Unknown;
		}

		bool isDenied = false;
		for( int n = 0; n < readmeFiles.Length; ++n ) {
			readmeTexts[n] = _ReadReadmeText( readmeFiles[n] );
			try {
				for( int i = 0; i < LicensePiaproStrings.Length; ++i ) {
					if( readmeTexts[n].Contains( LicensePiaproStrings[i] ) ) {
						isPiapro = true;
						break;
					}
				}
				for( int i = 0; i < LicenseDeniedStrings.Length; ++i ) {
					if( readmeTexts[n].Contains( LicenseDeniedStrings[i] ) ) {
						isDenied = true;
						if( n > 0 ) { // High priority is denied.
							_Swap( readmeFiles, 0, n );
							_Swap( readmeTexts, 0, n );
						}
						break;
					}
				}
			} catch( System.Exception ) {
			}
		}

		if( isDenied ) {
			return LicenseAgree.Denied;
		} else {
			return LicenseAgree.Warning;
		}
	}

	public string[] DecodeModelComments()
	{
		if( this.pmx2fbxProperty == null || this.pmx2fbxProperty.pmxAsset == null ) {
			Debug.LogError("");
			return null; // Error.
		}
		
		string assetPath = AssetDatabase.GetAssetPath( this.pmx2fbxProperty.pmxAsset );
		if( string.IsNullOrEmpty( assetPath ) ) {
			Debug.LogError("");
			return null; // Error.
		}

		byte[] bytes = null;
		try {
			bytes = File.ReadAllBytes( assetPath );
			if( bytes == null ) {
				Debug.LogError("");
				return null; // Error.
			}
		} catch( System.Exception ) {
			return null;
		}

		return DecodeModelComments( bytes, Path.GetExtension( assetPath ) );
	}

	public enum PMXEncoding {
		UTF16,
		UTF8,
	}

	public enum Comment {
		ModelNameJp,
		ModelNameEn,
		CommentJp,
		CommentEn,
	}

	public static string ReadPMDString( BinaryReader reader, int maxLen )
	{
		if( reader == null ) {
			Debug.LogError("");
			return null;
		}

		if( maxLen > 0 ) {
			byte[] bytes = reader.ReadBytes( maxLen );
			if( bytes != null ) {
				int len = 0;
				for( int i = 0; i < maxLen; ++i, ++len ) {
					if( bytes[i] == 0 ) {
						break;
					}
				}

				Encoding enc = System.Text.Encoding.GetEncoding(932);
				if( enc != null ) {
					return enc.GetString( bytes, 0, len );
				}
			}
		}

		return "";
	}

	public static string ReadPMXString( BinaryReader reader, PMXEncoding enc )
	{
		if( reader == null ) {
			Debug.LogError("");
			return null;
		}

		uint len = reader.ReadUInt32();
		if( len > 0 ) {
			byte[] bytes = reader.ReadBytes( (int)len );
			if( bytes != null ) {
				switch( enc ) { 
				case PMXEncoding.UTF16:
					return System.Text.Encoding.Unicode.GetString( bytes );
				case PMXEncoding.UTF8:
					return System.Text.Encoding.UTF8.GetString( bytes );
				}
			}
		}

		return "";
	}

	public static string[] DecodeModelComments( byte[] fileBytes, string extension )
	{
		if( string.IsNullOrEmpty( extension ) ) {
			Debug.LogError("");
			return null; // Error.
		}

		using( MemoryStream memoryStream = new MemoryStream( fileBytes ) ) {
			using( BinaryReader binaryReader = new BinaryReader( memoryStream ) ) {
				try {
					extension = extension.ToLower();
					if( extension == ".pmx" ) {
						byte[] bytes = binaryReader.ReadBytes( 4 );
						if( bytes == null || bytes.Length != 4 ||
						    bytes[0] != (byte)'P' ||
						    bytes[1] != (byte)'M' ||
						    bytes[2] != (byte)'X' ||
						    bytes[3] != (byte)' ' ) {
							Debug.LogError("");
							return null; // Error.
						}

						binaryReader.ReadSingle(); // fileVersion

						byte fileInfoLength = binaryReader.ReadByte();
						if( fileInfoLength == 0 ) {
							Debug.LogError("");
							return null; // Error.
						}

						byte[] fileInfo = binaryReader.ReadBytes( (int)fileInfoLength );
						if( fileInfo == null || fileInfo.Length != (int)fileInfoLength ) {
							Debug.LogError("");
							return null; // Error.
						}

						PMXEncoding enc = (PMXEncoding)fileInfo[0];
						if( enc != PMXEncoding.UTF16 &&
						    enc != PMXEncoding.UTF8 ) {
							Debug.LogError("");
							return null; // Error.
						}

						string[] comments = new string[4];
						comments[0] = ReadPMXString( binaryReader, enc ); // modelNameJp
						comments[1] = ReadPMXString( binaryReader, enc ); // modelNameEn
						comments[2] = ReadPMXString( binaryReader, enc ); // commentJp
						comments[3] = ReadPMXString( binaryReader, enc ); // commentEn
						return comments;
					} else if( extension == ".pmd" ) {
						byte[] bytes = binaryReader.ReadBytes( 3 );
						if( bytes == null || bytes.Length != 3 ||
						    bytes[0] != (byte)'P' ||
						    bytes[1] != (byte)'m' ||
						    bytes[2] != (byte)'d' ) {
							Debug.LogError("");
							return null; // Error.
						}

						binaryReader.ReadSingle(); // fileVersion

						string[] comments = new string[4];
						comments[0] = ReadPMDString( binaryReader, 20 );
						comments[2] = ReadPMDString( binaryReader, 256 );
						return comments;
					} else {
						Debug.LogError("");
						return null;
					}
				} catch ( System.Exception ){
					Debug.LogError("");
					return null; // Error.
				}
			}
		}
	}
}
