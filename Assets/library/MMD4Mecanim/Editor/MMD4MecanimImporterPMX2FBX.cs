//#define _HIDDEN_FUNCTIONS

using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public partial class MMD4MecanimImporter : ScriptableObject
{
	// .pmx2fbx.xml
	public class PMX2FBXConfig
	{
		public const int ShaderTypeUnknown = -1;

		public enum ShaderType
		{
			MMD4Mecanim,
			Otherwise,
		}

		// Helper function
		public static int GetShaderType( Material material )
		{
			if( material != null && material.shader != null && material.shader.name != null ) {
				string shaderName = material.shader.name;
				if( shaderName.StartsWith( "MMD4Mecanim" ) ) {
					return (int)ShaderType.MMD4Mecanim;
				} else if( material.mainTexture != null ) {
					return (int)ShaderType.Otherwise;
				}
			}

			return ShaderTypeUnknown;
		}

		public enum Transparency
		{
			Disable,
			Enable,
		}

		public enum AutoLuminous
		{
			Disable,
			Emissive,
		}

		#if false
		public enum RotationBasedUpon
		{
			Original,
			RootNodeRotation,
		}
		
		public enum PositionBasedUpon
		{
			Original,
			RootNodePosition,
		}
		#endif

		public enum Wine
		{
			NXWine,
			WineBottler,
			Wine,
			Manual,
		}
		
		public class GlobalSettings
		{
			public bool					editorAdvancedMode				= false;

			public float				vertexScale						= 8.0f;
			public float				vertexScaleByHeight				= 0.0f;
			public float				importScale						= 0.01f;
			public int					keepIKTargetBoneFlag			= 0;
			public int					enableIKInnerLockFlag			= 1;
			public int					enableIKInnerLockKneeFlag		= -1;
			public float				innerLockKneeClamp				= -1.0f;
			public float				innerLockKneeRatioU				= -1.0f;
			public float				innerLockKneeRatioL				= -1.0f;
			public float				innerLockKneeScale				= -1.0f;
			public int					enableIKMuscleFlag				= 1;
			public int					enableIKMuscleHipFlag			= 1;
			public int					enableIKMuscleFootFlag			= 1;
			public float				muscleHipUpperXAngle			= -1.0f;
			public float				muscleHipLowerXAngle			= -1.0f;
			public float				muscleHipInnerYAngle			= -1.0f;
			public float				muscleHipOuterYAngle			= -1.0f;
			public float				muscleHipInnerZAngle			= -1.0f;
			public float				muscleHipOuterZAngle			= -1.0f;
			public float				muscleFootUpperXAngle			= -1.0f;
			public float				muscleFootLowerXAngle			= -1.0f;
			public float				muscleFootInnerYAngle			= -1.0f;
			public float				muscleFootOuterYAngle			= -1.0f;
			public float				muscleFootInnerZAngle			= -1.0f;
			public float				muscleFootOuterZAngle			= -1.0f;
#if MMD4MECANIM_DEBUG
			public int					parseHierarchyFlag				= 0;
			public int					debugAnimBeginFrame				= 0;
			public int					debugAnimEndFrame				= 0;
#endif
			public int					createSkeletonFlag				= 1;
			public int					renameFlag						= 1;
			public int					prefixBoneNoNameFlag			= 1;
			public int					prefixNullBoneNameFlag			= 1;
			public int					prefixRenameFlag				= 1;
			public int					addMotionNameExtFlag			= 1;
			public int					nullBoneFlag					= 1;
			public int					nullBoneAnimationFlag			= 0;
			public int					dummyCharBoneFlag				= 0;
			public int					concatBoneFlag					= 0;

			public int					bone4MecanimFlag				= 1;
			public int					bodyBone4Mecanim				= 1;
			public int					armBone4Mecanim					= 1;
			public int					handBone4Mecanim				= 1;
			public int					handBone4MecanimModThumb0		= 1;
			public int					legBone4Mecanim					= 1;
			public int					headBone4Mecanim				= 1;

			public int					blendShapesFlag					= 1;

			public int					splitMeshFlag					= 1;
			public int					splitMeshVertexMorphFlag		= 1;
			public int					splitMeshVertexMorphExtraFlag	= 1;
			public int					splitMeshVertexMorphCombineFlag	= 1;
			public int					splitMeshXDEFFlag				= 0;
			public int					splitMeshXDEFExtraFlag			= 0;
			public int					splitMeshXDEFExtraAlphaFlag		= 0;

			public int					supportVMDIKDisabled			= 1;
			public int					supportVMDPhysicsDisabled		= 1;

			public int					animNullAnimationFlag			= 1;
			public int					animRootTransformFlag			= 1;
			public float				animKeyRotationEpsilon1			= 0.02f;
			public float				animKeyRotationEpsilon2			= 0.03f;
			public float				animKeyTranslationEpsilon1		= 0.002f;
			public float				animKeyTranslationEpsilon2		= 0.003f;
			public int					animAwakeWaitingTime			= 3;

			public int					morphOppaiFlag					= 0;
			public int					morphOppaiTanimaMethod			= 0;
			public double				morphOppaiPower					= 0.5;
			public double				morphOppaiCenter				= 0.6;
			public double				morphOppaiRaidus				= 1.0;
			public string				morphOppaiBoneName1				= "\u4E0A\u534A\u8EAB";
			public string				morphOppaiBoneName2				= "\u9996";
		}
		
		public class BulletPhysics
		{
			public int					enabled											= 1;
			public int					framePerSecond									= 0;
			public float				gravityScale									= 10.0f;
			public int					worldSolverInfoNumIterations					= 0;
			public int					worldSolverInfoSplitImpulse						= 1;
			public int					worldAddFloorPlane								= 1;
			public int					rigidBodyIsAdditionalDamping					= 1;
			public int					rigidBodyIsForceBoneAlignment					= 1;
			public int					rigidBodyIsEnableSleeping						= 0;
			public int					rigidBodyIsUseCcd								= 1;
			public float				rigidBodyCcdMotionThreshold						= 0.01f;
			public float				rigidBodyShapeScale								= 1.0f;
			public float				rigidBodyMassRate								= 1.0f;
			public float				rigidBodyLinearDampingRate						= 1.0f;
			public float				rigidBodyAngularDampingRate						= 1.0f;
			public float				rigidBodyRestitutionRate						= 1.0f;
			public float				rigidBodyFrictionRate							= 1.0f;
			public float				rigidBodyAntiJitterRate							= 1.0f;
			public float				rigidBodyAntiJitterRateOnKinematic				= 1.0f;
			public float				rigidBodyPreBoneAlignmentLimitLength			= 0.01f;
			public float				rigidBodyPreBoneAlignmentLossRate				= 0.01f;
			public float				rigidBodyPostBoneAlignmentLimitLength			= 0.01f;
			public float				rigidBodyPostBoneAlignmentLossRate				= 0.01f;
			public float				rigidBodyLinearDampingLossRate					= -1.0f;
			public float				rigidBodyLinearDampingLimit						= -1.0f;
			public float				rigidBodyAngularDampingLossRate					= -1.0f;
			public float				rigidBodyAngularDampingLimit					= -1.0f;
			public float				rigidBodyLinearVelocityLimit					= -1.0f;
			public float				rigidBodyAngularVelocityLimit					= -1.0f;
			public int					rigidBodyIsUseForceAngularVelocityLimit			= 1;
			public int					rigidBodyIsUseForceAngularAccelerationLimit		= 1;
			public float				rigidBodyForceAngularVelocityLimit				= -1.0f;
			public int					rigidBodyIsAdditionalCollider					= 1;
			public float				rigidBodyAdditionalColliderBias					= 0.99f;
			public int					rigidBodyIsForceTranslate						= 1;
			public float				jointRootAdditionalLimitAngle					= -1.0f;
		}

		public class Rename
		{
			public string				from;
			public string				to;
		}
		
		public class FreezeRigidBody
		{
			public string				boneName;
			public int					recursively;
		}

		public class FreezeMotion
		{
			public string				boneName;
			public int					recursively;
		}

		public class MaterialProperty
		{
			public string				materialName;
			public bool					isProcessing = true;
		}
		
		public class MMD4MecanimProperty
		{
			// for PMX2FBX
			public bool					vertexScaleByHeightFlag;
			public float				vertexScaleByHeight = 158.0f;
			public bool					waitProcessExitting;
			public string				pmxAssetPath;
			public List<string>			vmdAssetPathList;
			public string				fbxOutputPath;
			public string				fbxAssetPath;
			// for Mac / Linux
			public bool					useWineFlag;
			public Wine					wine;
			public string				winePath;
			// for Material
#if MMD4MECANIM_DEBUG
			public bool					isDebugShader = false;
#endif
			public bool					isDeferred = false;
			public Color				baseDiffuse = new Color( 1.0f, 1.0f, 1.0f, 1.0f );
			public Transparency			transparency = Transparency.Enable;
			public bool					isDrawEdge = true;
			public float				edgeScale = 1.0f;
			public float				shadowLum = 1.5f;
			public bool					isSelfShadow = true;
			public float				addLightToonCen = -0.1f;
			public float				addLightToonMin = 0.5f;
			public AutoLuminous			autoLuminous = AutoLuminous.Disable;
			public float				autoLuminousPower = 5.0f;

			public List<MaterialProperty> materialProperyList;
#if false
			// for Animation
			public bool					rootTransformRotationBakeIntoPose = true;
			public RotationBasedUpon	rootTransformRotationBasedUpon = RotationBasedUpon.Original;
			public bool					rootTransformPositionYBakeIntoPose = true;
			public PositionBasedUpon	rootTransformPositionYBasedUpon = PositionBasedUpon.Original;
			public bool					rootTransformPositionXZBakeIntoPose = true;
			public PositionBasedUpon	rootTransformPositionXZBasedUpon = PositionBasedUpon.Original;
			public bool					keepAdditionalBones = true;
			public bool					autoProcessAnimationsOnImported = true;
#endif
			// Helper functions.
			public MaterialProperty FindMaterialProperty( Material material )
			{
				if( material != null && material.name != null ) {
					if( this.materialProperyList != null ) {
						foreach( MaterialProperty materialProperty in this.materialProperyList ) {
							if( materialProperty != null && materialProperty.materialName != null && materialProperty.materialName == material.name ) {
								return materialProperty;
							}
						}
					}
				}
				
				return null;
			}

			public void PrepareMaterialPropertyList( Material[] materials )
			{
				if( this.materialProperyList == null ) {
					this.materialProperyList = new List<MaterialProperty>();
				}

				if( materials != null ) {
					foreach( Material material in materials ) {
						if( material != null && material.name != null ) {
							if( FindMaterialProperty( material ) == null ) {
								MaterialProperty materialProperty = new MaterialProperty();
								materialProperty.materialName = material.name;
								this.materialProperyList.Add( materialProperty );
							}
						}
					}
				}
			}
		}

		public GlobalSettings			globalSettings;
		public BulletPhysics			bulletPhysics;
		public List<Rename>				renameList;
		public List<FreezeRigidBody>	freezeRigidBodyList;
		public List<FreezeMotion>		freezeMotionList;
		public MMD4MecanimProperty		mmd4MecanimProperty;

		// Helper functions.
		public MaterialProperty FindMaterialProperty( Material material )
		{
			if( this.mmd4MecanimProperty != null ) {
				return this.mmd4MecanimProperty.FindMaterialProperty( material );
			}

			return null;
		}

		public void PrepareMaterialPropertyList( Material[] materials )
		{
			if( this.mmd4MecanimProperty != null ) {
				this.mmd4MecanimProperty.PrepareMaterialPropertyList( materials );
			}
		}
	}
	
	static readonly string[] WinePaths = new string[] {
		"/Applications/NXWine.app/Contents/Resources/bin/wine",
		"/Applications/Wine.app/Contents/Resources/bin/wine",
		"/opt/local/bin/wine",
		"/opt/local/bin/wine",
	};
	
	// .model.xml
	public class MMDModel
	{
		public enum FileType
		{
			None,
			PMD,
			PMX,
		}
		
		public enum BoneType
		{
			Rotate,
			RotateAndMove,
			IKDestination,
			Unknown,
			UnderIK,
			UnderRotate,
			IKTarget,
			NoDisp,
			Twist,
			FollowRotate,
		}
		
		public enum SphereMode
		{
			None,
			Multiply,
			Adding,
			SubTexture,
		}
		
		// for PMD
		public enum ExpType
		{
			Base,
			EyeBrow,
			Eye,
			Lip,
			Other,
		}
		
		public enum MorphCategory
		{
			Base,
			EyeBrow,
			Eye,
			Lip,
			Other,
		}

		public enum MorphType
		{
			Group,
			Vertex,
			Bone,
			UV,
			UVA1,
			UVA2,
			UVA3,
			UVA4,
			Material,
		}
		
		public enum DisplayFrameItemType
		{
			Bone,
			Morph,
		}
		
		public enum ShapeType
		{
			Sphere,
			Box,
			Capsule,
		}
		
		public enum RigidBodyType
		{
			Static,
			Dynamic,
			StaticDynamic,
		}
		
		public enum JointType
		{
			Spring6DOF,
		}

		public class GlobalSettings
		{
			public FileType				fileType;
			public float				fileVersion;
			public string				modelNameJp;
			public string				modelNameEn;
			public string				commentJp;
			public string				commentEn;
			public uint					numVertex;
			public float				vertexScale;
			public float				importScale;
		}
		
		public class Texture
		{
			public string				fileName;
		}
		
		public class Material
		{
			public string				nameJp;
			public string				nameEn;
			public string				materialName;

			public Color				diffuse;
			public Color				specular;		// not use A
			public float				shiness;
			public Color				ambient;		// not use A
			public SphereMode			sphereMode;
			public int					toonID;
			
			public Color				edgeColor;
			public float				edgeSize;
			public int					textureID;
			public int					additionalTextureID;
			public int					toonTextureID;

			public uint					flags;
			public bool					isDrawBothFaces;
			public bool					isDrawGroundShadow;
			public bool					isDrawSelfShadowMap;
			public bool					isDrawSelfShadow;
			public bool					isDrawEdge;
			public uint					numIndex;
		}
		
		public class Bone
		{
			public string				nameJp;
			public string				nameEn;
			public string				skeletonName;
			public int					parentBoneID;
			public int					sortedBoneID;
			//public int				orderedBoneID;
			public Vector3				origin;
			
			public uint					additionalFlags;
			public bool					isLimitAngleX;
			public bool					isRigidBody;
			public bool					isKinematic;
			
			public BoneType				boneType;
			public int					targetBoneID;
			public int					childBoneID;
			public float				followCoef;
			
			public int					transformLayerID;
			public uint					flags;
			public int					destination;
			public bool					isRotate;
			public bool					isTranslate;
			public bool					isVisible;
			public bool					isControllable;
			public bool					isIK;
			public int					inherenceLocal;
			public bool					isInherenceRotate;
			public bool					isInherenceTranslate;
			public bool					isFixedAxis;
			public bool					isLocalAxis;
			public bool					isTransformAfterPhysics;
			public bool					isTransformExternalParent;
			public string				humanType;
		}
		
		public class IKLink
		{
			public bool					hasAngularLimit;
			public Vector3				lowerLimit;
			public Vector3				upperLimit;
		}
		
		public class IK
		{
			public int					destBoneID;
			public int					targetBoneID;
			public int					iteration;
			public float				constraintAngle;
			public IKLink[]				ikLinkList;
		}
		
		public class Morph
		{
			public string				nameJp;
			public string				nameEn;
			public uint					additionalFlags;
			public bool					isMorphBaseVertex;	// for PMD
			
			// for PMD
			public ExpType				expType;

			// for PMX
			public MorphCategory		morphCategory;
			public MorphType			morphType;
			
			public int[]				indexList;
		}
		
		public class DisplayFrame
		{
			public class Item
			{
				public DisplayFrameItemType	type;
				public int					index;
			}
			
			public string				nameJp;
			public string				nameEn;
			public uint					additionalFlags;
			public bool					isSpecial;
			
			public Item[]				itemList;
		}
		
		public class RigidBody
		{
			public string				nameJp;
			public string				nameEn;
			public int					boneID;
			public uint					collisionGroupID;
			public uint					collisionMask;
			public ShapeType			shapeType;
			public float				shapeWidth;
			public float				shapeHeight;
			public float				shapeDepth;
			public Vector3				position;
			public Quaternion			rotation;
			public float				mass;
			public float				linearDamping;
			public float				angularDamping;
			public float				restitution;
			public float				friction;
			public float				rigidBodyType;
		}

		public class Joint
		{
			public string				nameJp;
			public string				nameEn;
			public JointType			jointType;
			public int					targetRigidBodyIDA;
			public int					targetRigidBodyIDB;
			public Vector3				position;
			public Quaternion			rotation;
			public Vector3				limitPosFrom;
			public Vector3				limitPosTo;
			public Vector3				limitRotFrom;
			public Vector3				limitRotTo;
		}

		public GlobalSettings			globalSettings;
		public Texture[]				textureList;
		public Material[]				materialList;
		public Bone[]					boneList;
		public IK[]						ikList;
		public Morph[]					morphList;
		public DisplayFrame[]			displayFrameList;
		public RigidBody[]				rigidBodyList;
		public Joint[]					jointList;
	}
	
	public static PMX2FBXConfig GetPMX2FBXConfig( string xmlAssetPath )
	{
		if( string.IsNullOrEmpty( xmlAssetPath ) ) {
			return null;
		}
		
		if( !System.IO.File.Exists( xmlAssetPath ) ) {
			return null;
		}
		
		System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer( typeof(PMX2FBXConfig) );
		try {
			using( System.IO.FileStream fs = new System.IO.FileStream( xmlAssetPath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read ) ) {
				return (PMX2FBXConfig)serializer.Deserialize(fs);
			}
		} catch( System.Exception ) {
			return null;
		}
	}
	
	public static bool WritePMX2FBXConfig( string xmlAssetPath, PMX2FBXConfig pmx2fbxConfig )
	{
		if( string.IsNullOrEmpty( xmlAssetPath ) ) {
			Debug.LogWarning( "xmlAssetPath is null." );
			return false;
		}
		
		System.Xml.XmlWriterSettings xmlWriterSettings = new System.Xml.XmlWriterSettings 
		{ 
		    Indent = true, 
		    OmitXmlDeclaration = false, 
		    Encoding = System.Text.Encoding.UTF8 
		};		

		System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer( typeof(PMX2FBXConfig) );
		try {
			using( System.IO.FileStream fs = new System.IO.FileStream( xmlAssetPath, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None ) ) {
				using (System.Xml.XmlWriter xmlWriter = System.Xml.XmlWriter.Create(fs, xmlWriterSettings)) {  		
					serializer.Serialize( xmlWriter, pmx2fbxConfig );
				}
			}
			return true;
		} catch( System.Exception ) {
			Debug.LogError( "" );
			return false;
		}
	}

	//----------------------------------------------------------------------------------------------------------------------------------------------------------------------------

	public static string GetImporterAssetPath( string mmdAssetPath )
	{
		if( mmdAssetPath == null ) {
			return null;
		}

		return ( MMD4MecanimEditorCommon.GetPathWithoutExtension( mmdAssetPath ) + ".MMD4Mecanim.asset" ).Normalize(System.Text.NormalizationForm.FormC);
	}

	public static string GetImporterPropertyAssetPath( string mmdAssetPath )
	{
		if( mmdAssetPath == null ) {
			return null;
		}

		return ( MMD4MecanimEditorCommon.GetPathWithoutExtension( mmdAssetPath ) + ".MMD4Mecanim.xml" ).Normalize(System.Text.NormalizationForm.FormC);
	}

	public static string GetMMDModelPath( GameObject fbxAsset )
	{
		if( fbxAsset == null ) {
			return null;
		}

		string fbxAssetPath = AssetDatabase.GetAssetPath( fbxAsset );
		return ( MMD4MecanimEditorCommon.GetPathWithoutExtension( fbxAssetPath ) + ".xml" ).Normalize(System.Text.NormalizationForm.FormC);
	}
	
	public static string GetMMDModelPath( string fbxAssetPath )
	{
		if( fbxAssetPath == null ) {
			return null;
		}
		
		return ( MMD4MecanimEditorCommon.GetPathWithoutExtension( fbxAssetPath ) + ".xml" ).Normalize(System.Text.NormalizationForm.FormC);
	}

	public static string GetIndexDataPath( string fbxAssetPath )
	{
		if( fbxAssetPath == null ) {
			return null;
		}
		
		return ( MMD4MecanimEditorCommon.GetPathWithoutExtension( fbxAssetPath ) + ".index.bytes" ).Normalize(System.Text.NormalizationForm.FormC);
	}

	public static string GetVertexDataPath( string fbxAssetPath )
	{
		if( fbxAssetPath == null ) {
			return null;
		}
		
		return ( MMD4MecanimEditorCommon.GetPathWithoutExtension( fbxAssetPath ) + ".vertex.bytes" ).Normalize(System.Text.NormalizationForm.FormC);
	}

	public static string GetModelDataPath( string fbxAssetPath )
	{
		if( fbxAssetPath == null ) {
			return null;
		}
		
		return ( MMD4MecanimEditorCommon.GetPathWithoutExtension( fbxAssetPath ) + ".model.bytes" ).Normalize(System.Text.NormalizationForm.FormC);
	}

	public static string GetExtraDataPath( string fbxAssetPath )
	{
		if( fbxAssetPath == null ) {
			return null;
		}
		
		return ( MMD4MecanimEditorCommon.GetPathWithoutExtension( fbxAssetPath ) + ".extra.bytes" ).Normalize(System.Text.NormalizationForm.FormC);
	}

	public static string GetAnimDataPath( string vmdAssetPath )
	{
		if( vmdAssetPath == null ) {
			return null;
		}

		return ( MMD4MecanimEditorCommon.GetPathWithoutExtension( vmdAssetPath ) + ".anim.bytes" ).Normalize(System.Text.NormalizationForm.FormC);
	}

	//----------------------------------------------------------------------------------------------------------------------------------------------------------------------------

	public static MMDModel GetMMDModel( string xmlAssetPath )
	{
		if( string.IsNullOrEmpty( xmlAssetPath ) || !File.Exists( xmlAssetPath ) ) {
			return null;
		}
		
		System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer( typeof(MMDModel) );
		try {
			using( System.IO.FileStream fs = new System.IO.FileStream( xmlAssetPath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read ) ) {
				return (MMDModel)serializer.Deserialize(fs);
			}
		} catch( System.Exception e ) {
			Debug.LogError( "GetMMDModel:" + e.ToString() );
			return null;
		}
	}
	
	private void _OnInspectorGUI_PMX2FBX()
	{
		GUI.enabled = !this.isProcessing;
		
		EditorGUIUtility.LookLikeControls();
		
		if( this.pmx2fbxProperty == null || this.pmx2fbxConfig == null ) {
			return;
		}
		
		var mmd4MecanimProperty = pmx2fbxConfig.mmd4MecanimProperty;
		var globalSettings = this.pmx2fbxConfig.globalSettings;
		var bulletPhysics = this.pmx2fbxConfig.bulletPhysics;

		bool editorAdvancedMode = this._editorAdvancedMode;

		if( editorAdvancedMode ) {
			GUILayout.Label( "Global Settings", EditorStyles.boldLabel );
			if( globalSettings != null && mmd4MecanimProperty != null ) {
				EditorGUILayout.BeginHorizontal();
				GUILayout.Space( 20.0f );
				EditorGUILayout.BeginVertical();
				if( mmd4MecanimProperty.vertexScaleByHeightFlag ) {
					mmd4MecanimProperty.vertexScaleByHeight = EditorGUILayout.FloatField( "Height", mmd4MecanimProperty.vertexScaleByHeight );
					globalSettings.vertexScaleByHeight = mmd4MecanimProperty.vertexScaleByHeight;
				} else {
					globalSettings.vertexScale = EditorGUILayout.FloatField( "Vertex Scale", globalSettings.vertexScale );
					globalSettings.vertexScaleByHeight = 0.0f;
				}

				mmd4MecanimProperty.vertexScaleByHeightFlag = GUILayout.Toggle( mmd4MecanimProperty.vertexScaleByHeightFlag, "Vertex Scale by Height" );
				globalSettings.importScale = EditorGUILayout.FloatField( "Import Scale", globalSettings.importScale );
				globalSettings.blendShapesFlag = EditorGUILayout.Toggle( "BlendShapes", globalSettings.blendShapesFlag != 0 ) ? 1 : 0;
				EditorGUILayout.EndVertical();
				EditorGUILayout.EndHorizontal();

				pmx2fbxProperty.viewAdvancedGlobalSettings = EditorGUILayout.Foldout( pmx2fbxProperty.viewAdvancedGlobalSettings, "Advanced" );
				if( pmx2fbxProperty.viewAdvancedGlobalSettings ) {
					MMD4MecanimEditorCommon.LookLikeInspector();

					EditorGUILayout.BeginHorizontal();
					GUILayout.Space( 20.0f );
					EditorGUILayout.BeginVertical();

					pmx2fbxProperty.viewAdvancedGlobalSettingsIK = EditorGUILayout.Foldout( pmx2fbxProperty.viewAdvancedGlobalSettingsIK, "IK" );
					if( pmx2fbxProperty.viewAdvancedGlobalSettingsIK ) {
						globalSettings.keepIKTargetBoneFlag = EditorGUILayout.Toggle( "KeepIKTargetBone", globalSettings.keepIKTargetBoneFlag != 0 ) ? 1 : 0;

						globalSettings.enableIKInnerLockFlag = EditorGUILayout.BeginToggleGroup( "IKInnerLock", globalSettings.enableIKInnerLockFlag != 0 ) ? 1 : 0;
						globalSettings.enableIKInnerLockKneeFlag = EditorGUILayout.IntField( "EnableIKInnerLockKnee", globalSettings.enableIKInnerLockKneeFlag );
						globalSettings.innerLockKneeClamp = EditorGUILayout.FloatField( "InnerLockKneeClamp", globalSettings.innerLockKneeClamp );
						globalSettings.innerLockKneeRatioU = EditorGUILayout.FloatField( "InnerLockKneeRatioU", globalSettings.innerLockKneeRatioU );
						globalSettings.innerLockKneeRatioL = EditorGUILayout.FloatField( "InnerLockKneeRatioL", globalSettings.innerLockKneeRatioL );
						globalSettings.innerLockKneeScale = EditorGUILayout.FloatField( "InnerLockKneeScale", globalSettings.innerLockKneeScale );
						EditorGUILayout.EndToggleGroup();

						globalSettings.enableIKMuscleFlag = EditorGUILayout.BeginToggleGroup( "IKMuscle", globalSettings.enableIKMuscleFlag != 0 ) ? 1 : 0;
						bool guiEnabled = GUI.enabled;
						globalSettings.enableIKMuscleHipFlag = EditorGUILayout.Toggle( "EnableIKMuscleHip", globalSettings.enableIKMuscleHipFlag != 0 ) ? 1 : 0;
						GUI.enabled = GUI.enabled && (globalSettings.enableIKMuscleHipFlag != 0);
						globalSettings.muscleHipUpperXAngle = EditorGUILayout.FloatField( "MuscleHipUpperXAngle", globalSettings.muscleHipUpperXAngle );
						globalSettings.muscleHipLowerXAngle = EditorGUILayout.FloatField( "MuscleHipLowerXAngle", globalSettings.muscleHipLowerXAngle );
						globalSettings.muscleHipInnerYAngle = EditorGUILayout.FloatField( "MuscleHipInnerYAngle", globalSettings.muscleHipInnerYAngle );
						globalSettings.muscleHipOuterYAngle = EditorGUILayout.FloatField( "MuscleHipOuterYAngle", globalSettings.muscleHipOuterYAngle );
						globalSettings.muscleHipInnerZAngle = EditorGUILayout.FloatField( "MuscleHipInnerZAngle", globalSettings.muscleHipInnerZAngle );
						globalSettings.muscleHipOuterZAngle = EditorGUILayout.FloatField( "MuscleHipOuterZAngle", globalSettings.muscleHipOuterZAngle );
						GUI.enabled = guiEnabled;
						globalSettings.enableIKMuscleFootFlag = EditorGUILayout.Toggle( "EnableIKMuscleFoot", globalSettings.enableIKMuscleFootFlag != 0 ) ? 1 : 0;
						GUI.enabled = GUI.enabled && (globalSettings.enableIKMuscleFootFlag != 0);
						globalSettings.muscleFootUpperXAngle = EditorGUILayout.FloatField( "MuscleFootUpperXAngle", globalSettings.muscleFootUpperXAngle );
						globalSettings.muscleFootLowerXAngle = EditorGUILayout.FloatField( "MuscleFootLowerXAngle", globalSettings.muscleFootLowerXAngle );
						globalSettings.muscleFootInnerYAngle = EditorGUILayout.FloatField( "MuscleFootInnerYAngle", globalSettings.muscleFootInnerYAngle );
						globalSettings.muscleFootOuterYAngle = EditorGUILayout.FloatField( "MuscleFootOuterYAngle", globalSettings.muscleFootOuterYAngle );
						globalSettings.muscleFootInnerZAngle = EditorGUILayout.FloatField( "MuscleFootInnerZAngle", globalSettings.muscleFootInnerZAngle );
						globalSettings.muscleFootOuterZAngle = EditorGUILayout.FloatField( "MuscleFootOuterZAngle", globalSettings.muscleFootOuterZAngle );
						GUI.enabled = guiEnabled;
						EditorGUILayout.EndToggleGroup();
					}

					EditorGUILayout.Separator();

					#if MMD4MECANIM_DEBUG
					EditorGUILayout.LabelField( "Debug", EditorStyles.boldLabel );
					globalSettings.parseHierarchyFlag = EditorGUILayout.Toggle( "ParseHierarchy", globalSettings.parseHierarchyFlag != 0 ) ? 1 : 0;
					globalSettings.debugAnimBeginFrame = EditorGUILayout.IntField( "DebugAnimBeginFrame", globalSettings.debugAnimBeginFrame );
					globalSettings.debugAnimEndFrame = EditorGUILayout.IntField( "DebugAnimEndFrame", globalSettings.debugAnimEndFrame );
					EditorGUILayout.Separator();
					#endif

					{
						globalSettings.bone4MecanimFlag = EditorGUILayout.BeginToggleGroup( "Bone4Mecanim", globalSettings.bone4MecanimFlag != 0 ) ? 1 : 0;
						globalSettings.bodyBone4Mecanim = EditorGUILayout.Toggle( "BodyBone4Mecanim", globalSettings.bodyBone4Mecanim != 0 ) ? 1 : 0;
						globalSettings.armBone4Mecanim = EditorGUILayout.Toggle( "ArmBone4Mecanim", globalSettings.armBone4Mecanim != 0 ) ? 1 : 0;
						globalSettings.handBone4Mecanim = EditorGUILayout.Toggle( "HandBone4Mecanim", globalSettings.handBone4Mecanim != 0 ) ? 1 : 0;
						globalSettings.handBone4MecanimModThumb0 = EditorGUILayout.Toggle( "HandBone4MecanimModThumb0", globalSettings.handBone4MecanimModThumb0 != 0 ) ? 1 : 0;
						globalSettings.legBone4Mecanim = EditorGUILayout.Toggle( "LegBone4Mecanim", globalSettings.legBone4Mecanim != 0 ) ? 1 : 0;
						globalSettings.headBone4Mecanim = EditorGUILayout.Toggle( "HeadBone4Mecanim", globalSettings.headBone4Mecanim != 0 ) ? 1 : 0;
						EditorGUILayout.EndToggleGroup();
					}

					EditorGUILayout.Separator();

					EditorGUILayout.LabelField( "Etcetera", EditorStyles.boldLabel );

					globalSettings.createSkeletonFlag = EditorGUILayout.Toggle( "CreateSkeleton", globalSettings.createSkeletonFlag != 0 ) ? 1 : 0;
					globalSettings.addMotionNameExtFlag = EditorGUILayout.Toggle( "AddMotionNameExt", globalSettings.addMotionNameExtFlag != 0 ) ? 1 : 0;
					globalSettings.nullBoneFlag = EditorGUILayout.Toggle( "NullBone", globalSettings.nullBoneFlag != 0 ) ? 1 : 0;
					globalSettings.nullBoneAnimationFlag = EditorGUILayout.Toggle( "NullBoneAnimation", globalSettings.nullBoneAnimationFlag != 0 ) ? 1 : 0;
					{
						bool guiEnabled = GUI.enabled;
						globalSettings.splitMeshFlag = EditorGUILayout.Toggle( "SplitMesh", globalSettings.splitMeshFlag != 0 ) ? 1 : 0;
						GUI.enabled = guiEnabled && (globalSettings.splitMeshFlag != 0);
						globalSettings.splitMeshVertexMorphFlag = EditorGUILayout.Toggle( "SplitMesh VertexMorph", globalSettings.splitMeshVertexMorphFlag != 0 ) ? 1 : 0;
						globalSettings.splitMeshVertexMorphExtraFlag = EditorGUILayout.Toggle( "SplitMesh VertexMorphExtra", globalSettings.splitMeshVertexMorphExtraFlag != 0 ) ? 1 : 0;
						globalSettings.splitMeshVertexMorphCombineFlag = EditorGUILayout.Toggle( "SplitMesh VertexMorphCombine", globalSettings.splitMeshVertexMorphCombineFlag != 0 ) ? 1 : 0;
						globalSettings.splitMeshXDEFFlag = EditorGUILayout.Toggle( "SplitMesh XDEF", globalSettings.splitMeshXDEFFlag != 0 ) ? 1 : 0;
						globalSettings.splitMeshXDEFExtraFlag = EditorGUILayout.Toggle( "SplitMesh XDEF Extra", globalSettings.splitMeshXDEFExtraFlag != 0 ) ? 1 : 0;
						globalSettings.splitMeshXDEFExtraAlphaFlag = EditorGUILayout.Toggle( "SplitMesh XDEF ExtraAlpha", globalSettings.splitMeshXDEFExtraAlphaFlag != 0 ) ? 1 : 0;
						GUI.enabled = guiEnabled;
					}

					EditorGUILayout.Separator();
					globalSettings.supportVMDIKDisabled = EditorGUILayout.Toggle( "SupportVMDIKDisabled", globalSettings.supportVMDIKDisabled != 0 ) ? 1 : 0;
					globalSettings.supportVMDPhysicsDisabled = EditorGUILayout.Toggle( "SupportVMDPhysicsDisabled", globalSettings.supportVMDPhysicsDisabled != 0 ) ? 1 : 0;
					EditorGUILayout.Separator();

					globalSettings.animNullAnimationFlag = EditorGUILayout.Toggle( "AnimNullAnimation", globalSettings.animNullAnimationFlag != 0 ) ? 1 : 0;
					globalSettings.animRootTransformFlag = EditorGUILayout.Toggle( "AnimRootTransform", globalSettings.animRootTransformFlag != 0 ) ? 1 : 0;
					globalSettings.animKeyRotationEpsilon1 = EditorGUILayout.FloatField( "AnimKeyRotationEpsilon1", globalSettings.animKeyRotationEpsilon1 );
					globalSettings.animKeyRotationEpsilon2 = EditorGUILayout.FloatField( "AnimKeyRotationEpsilon2", globalSettings.animKeyRotationEpsilon2 );
					globalSettings.animKeyTranslationEpsilon1 = EditorGUILayout.FloatField( "AnimKeyTranslationEpsilon1", globalSettings.animKeyTranslationEpsilon1 );
					globalSettings.animKeyTranslationEpsilon2 = EditorGUILayout.FloatField( "AnimKeyTranslationEpsilon2", globalSettings.animKeyTranslationEpsilon2 );
					globalSettings.animAwakeWaitingTime = EditorGUILayout.IntField( "AnimAwakeWaitingTime", globalSettings.animAwakeWaitingTime );

					EditorGUILayout.Separator();

					globalSettings.morphOppaiFlag = EditorGUILayout.BeginToggleGroup( "MorphOppai", globalSettings.morphOppaiFlag != 0 ) ? 1 : 0;
					globalSettings.morphOppaiTanimaMethod = EditorGUILayout.IntField( "MorphOppaiTanimaMethod", globalSettings.morphOppaiTanimaMethod );
					globalSettings.morphOppaiPower = (double)EditorGUILayout.FloatField( "MorphOppaiPower", (float)globalSettings.morphOppaiPower );
					globalSettings.morphOppaiCenter = (double)EditorGUILayout.FloatField( "MorphOppaiCenter", (float)globalSettings.morphOppaiCenter );
					globalSettings.morphOppaiRaidus = (double)EditorGUILayout.FloatField( "MorphOppaiRaidus", (float)globalSettings.morphOppaiRaidus );
					globalSettings.morphOppaiBoneName1 = EditorGUILayout.TextField( "MorphOppaiBoneName1", globalSettings.morphOppaiBoneName1 );
					globalSettings.morphOppaiBoneName2 = EditorGUILayout.TextField( "MorphOppaiBoneName2", globalSettings.morphOppaiBoneName2 );
					EditorGUILayout.EndToggleGroup();

					EditorGUILayout.EndVertical();
					EditorGUILayout.EndHorizontal();
				}
			}
		
			EditorGUIUtility.LookLikeControls();

			EditorGUILayout.Separator();

			GUILayout.Label( "Bullet Physics", EditorStyles.boldLabel );

			if( bulletPhysics != null && pmx2fbxProperty != null ) {
				EditorGUILayout.BeginHorizontal();
				GUILayout.Space( 20.0f );
				EditorGUILayout.BeginVertical();
				bulletPhysics.enabled = EditorGUILayout.Toggle( "Enabled", bulletPhysics.enabled != 0 ) ? 1 : 0;

				if( !this.isProcessing ) {
					GUI.enabled = (bulletPhysics.enabled != 0);
				}

				bulletPhysics.framePerSecond = EditorGUILayout.IntField( "Frame Per Second", bulletPhysics.framePerSecond );
				bulletPhysics.gravityScale = (float)EditorGUILayout.FloatField( "Gravity Scale", (float)bulletPhysics.gravityScale );
				EditorGUILayout.EndVertical();
				EditorGUILayout.EndHorizontal();

				pmx2fbxProperty.viewAdvancedBulletPhysics = EditorGUILayout.Foldout( pmx2fbxProperty.viewAdvancedBulletPhysics, "Advanced" );
				if( pmx2fbxProperty.viewAdvancedBulletPhysics ) {
					MMD4MecanimEditorCommon.LookLikeInspector();

					EditorGUILayout.BeginHorizontal();
					GUILayout.Space( 20.0f );
					EditorGUILayout.BeginVertical();
					bulletPhysics.worldSolverInfoNumIterations = EditorGUILayout.IntField( "WorldSolverInfoNumIterations", bulletPhysics.worldSolverInfoNumIterations );
					bulletPhysics.worldSolverInfoSplitImpulse = EditorGUILayout.Toggle( "WorldSolverInfoSplitImpulse", bulletPhysics.worldSolverInfoSplitImpulse != 0 ) ? 1 : 0;
					bulletPhysics.worldAddFloorPlane = EditorGUILayout.Toggle( "WorldAddFloorPlane", bulletPhysics.worldAddFloorPlane != 0 ) ? 1 : 0;
					bulletPhysics.rigidBodyIsAdditionalDamping = EditorGUILayout.Toggle( "RigidBodyIsAdditionalDamping", bulletPhysics.rigidBodyIsAdditionalDamping != 0 ) ? 1 : 0;
					bulletPhysics.rigidBodyIsForceBoneAlignment = EditorGUILayout.Toggle( "RigidBodyIsForceBoneAlignment", bulletPhysics.rigidBodyIsForceBoneAlignment != 0 ) ? 1 : 0;
					bulletPhysics.rigidBodyIsEnableSleeping = EditorGUILayout.Toggle( "RigidBodyIsEnableSleeping", bulletPhysics.rigidBodyIsEnableSleeping != 0 ) ? 1 : 0;
					bulletPhysics.rigidBodyIsUseCcd = EditorGUILayout.Toggle( "RigidBodyIsUseCcd", bulletPhysics.rigidBodyIsUseCcd != 0 ) ? 1 : 0;
					bulletPhysics.rigidBodyCcdMotionThreshold = EditorGUILayout.FloatField( "RigidBodyCcdMotionThreshold", bulletPhysics.rigidBodyCcdMotionThreshold );
					bulletPhysics.rigidBodyShapeScale = EditorGUILayout.FloatField( "RigidBodyShapeScale", bulletPhysics.rigidBodyShapeScale );
					bulletPhysics.rigidBodyMassRate = EditorGUILayout.FloatField( "RigidBodyMassRate", bulletPhysics.rigidBodyMassRate );
					bulletPhysics.rigidBodyLinearDampingRate = EditorGUILayout.FloatField( "RigidBodyLinearDampingRate", bulletPhysics.rigidBodyLinearDampingRate );
					bulletPhysics.rigidBodyAngularDampingRate = EditorGUILayout.FloatField( "RigidBodyAngularDampingRate", bulletPhysics.rigidBodyAngularDampingRate );
					bulletPhysics.rigidBodyRestitutionRate = EditorGUILayout.FloatField( "RigidBodyRestitutionRate", bulletPhysics.rigidBodyRestitutionRate );
					bulletPhysics.rigidBodyFrictionRate = EditorGUILayout.FloatField( "RigidBodyFrictionRate", bulletPhysics.rigidBodyFrictionRate );
					bulletPhysics.rigidBodyAntiJitterRate = EditorGUILayout.Slider( "RigidBodyAntiJitterRate", bulletPhysics.rigidBodyAntiJitterRate, 0.0f, 1.0f );
					bulletPhysics.rigidBodyAntiJitterRateOnKinematic = EditorGUILayout.Slider( "RigidBodyAntiJitterRateOnKinematic", bulletPhysics.rigidBodyAntiJitterRateOnKinematic, 0.0f, 1.0f );
					bulletPhysics.rigidBodyPreBoneAlignmentLimitLength = EditorGUILayout.FloatField( "RigidBodyPreBoneAlignmentLimitLength", bulletPhysics.rigidBodyPreBoneAlignmentLimitLength );
					bulletPhysics.rigidBodyPreBoneAlignmentLossRate = EditorGUILayout.FloatField( "RigidBodyPreBoneAlignmentLossRate", bulletPhysics.rigidBodyPreBoneAlignmentLossRate );
					bulletPhysics.rigidBodyPostBoneAlignmentLimitLength = EditorGUILayout.FloatField( "RigidBodyPostBoneAlignmentLimitLength", bulletPhysics.rigidBodyPostBoneAlignmentLimitLength );
					bulletPhysics.rigidBodyPostBoneAlignmentLossRate = EditorGUILayout.FloatField( "RigidBodyPostBoneAlignmentLossRate", bulletPhysics.rigidBodyPostBoneAlignmentLossRate );
					bulletPhysics.rigidBodyLinearDampingLossRate = EditorGUILayout.FloatField( "RigidBodyLinearDampingLossRate", bulletPhysics.rigidBodyLinearDampingLossRate );
					bulletPhysics.rigidBodyLinearDampingLimit = EditorGUILayout.FloatField( "RigidBodyLinearDampingLimit", bulletPhysics.rigidBodyLinearDampingLimit );
					bulletPhysics.rigidBodyAngularDampingLossRate = EditorGUILayout.FloatField( "RigidBodyAngularDampingLossRate", bulletPhysics.rigidBodyAngularDampingLossRate );
					bulletPhysics.rigidBodyAngularDampingLimit = EditorGUILayout.FloatField( "RigidBodyAngularDampingLimit", bulletPhysics.rigidBodyAngularDampingLimit );
					bulletPhysics.rigidBodyLinearVelocityLimit = EditorGUILayout.FloatField( "RigidBodyLinearVelocityLimit", bulletPhysics.rigidBodyLinearVelocityLimit );
					bulletPhysics.rigidBodyAngularVelocityLimit = EditorGUILayout.FloatField( "RigidBodyAngularVelocityLimit", bulletPhysics.rigidBodyAngularVelocityLimit );
					bulletPhysics.rigidBodyIsUseForceAngularVelocityLimit = EditorGUILayout.Toggle( "RigidBodyIsUseForceAngularVelocityLimit", bulletPhysics.rigidBodyIsUseForceAngularVelocityLimit != 0 ) ? 1 : 0;
					bulletPhysics.rigidBodyIsUseForceAngularAccelerationLimit = EditorGUILayout.Toggle( "RigidBodyIsUseForceAngularAccelerationLimit", bulletPhysics.rigidBodyIsUseForceAngularAccelerationLimit != 0 ) ? 1 : 0;
					bulletPhysics.rigidBodyForceAngularVelocityLimit = EditorGUILayout.FloatField( "RigidBodyForceAngularVelocityLimit", bulletPhysics.rigidBodyForceAngularVelocityLimit );
					bulletPhysics.rigidBodyIsAdditionalCollider = EditorGUILayout.Toggle( "RigidBodyIsAdditionalCollider", bulletPhysics.rigidBodyIsAdditionalCollider != 0 ) ? 1 : 0;
					bulletPhysics.rigidBodyAdditionalColliderBias = EditorGUILayout.FloatField( "RigidBodyAdditionalColliderBias", bulletPhysics.rigidBodyAdditionalColliderBias );
					bulletPhysics.rigidBodyIsForceTranslate = EditorGUILayout.Toggle( "RigidBodyIsForceTranslate", bulletPhysics.rigidBodyIsForceTranslate != 0 ) ? 1 : 0;
					bulletPhysics.jointRootAdditionalLimitAngle = EditorGUILayout.FloatField( "JointRootAdditionalLimitAngle", bulletPhysics.jointRootAdditionalLimitAngle );
					EditorGUILayout.EndVertical();
					EditorGUILayout.EndHorizontal();
				}

				if( !this.isProcessing ) {
					GUI.enabled = true;
				}
			}
		
			EditorGUILayout.Separator();
			EditorGUIUtility.LookLikeControls();
			GUILayout.Label( "Rename List", EditorStyles.boldLabel );

			if( pmx2fbxConfig.renameList != null && pmx2fbxConfig.renameList.Count > 0 ) {
				GUILayout.BeginHorizontal();
				GUILayout.Space( 40.0f );
				GUILayout.Label( "From", GUILayout.ExpandWidth(true) );
				GUILayout.Label( "To", GUILayout.ExpandWidth(true) );
				GUILayout.EndHorizontal();
				for( int i = 0; i < pmx2fbxConfig.renameList.Count; ) {
					GUILayout.BeginHorizontal();
					GUILayout.Space( 20.0f );
					bool isRemove = GUILayout.Button("-", EditorStyles.miniButton, GUILayout.ExpandWidth(false) );
					if( pmx2fbxConfig.renameList[i].from == null ) {
						pmx2fbxConfig.renameList[i].from = "";
					}
					if( pmx2fbxConfig.renameList[i].to == null ) {
						pmx2fbxConfig.renameList[i].to = "";
					}
					pmx2fbxConfig.renameList[i].from = EditorGUILayout.TextField( pmx2fbxConfig.renameList[i].from );
					pmx2fbxConfig.renameList[i].to = EditorGUILayout.TextField( pmx2fbxConfig.renameList[i].to );
					GUILayout.EndHorizontal();
					if( isRemove ) {
						pmx2fbxConfig.renameList.RemoveAt( i );
					} else {
						++i;
					}
				}
			}
			{
				GUILayout.BeginHorizontal();
				GUILayout.Space( 20.0f );
				bool isAdd = GUILayout.Button("+", EditorStyles.miniButton, GUILayout.ExpandWidth(false) );
				GUILayout.Label( "Add" );
				GUILayout.EndHorizontal();
				
				if( isAdd ) {
					PMX2FBXConfig.Rename rename = new PMX2FBXConfig.Rename();
					rename.from = "";
					rename.to = "";
					if( pmx2fbxConfig.renameList == null ) {
						pmx2fbxConfig.renameList = new List<PMX2FBXConfig.Rename>();
					}
					pmx2fbxConfig.renameList.Add( rename );
				}
			}

			EditorGUILayout.Separator();
			EditorGUIUtility.LookLikeControls();
			GUILayout.Label( "Freeze Rigid Body List", EditorStyles.boldLabel );
			
			if( pmx2fbxConfig.freezeRigidBodyList != null ) {
				for( int i = 0; i < pmx2fbxConfig.freezeRigidBodyList.Count; ) {
					GUILayout.BeginHorizontal();
					GUILayout.Space( 20.0f );
					bool isRemove = GUILayout.Button("-", EditorStyles.miniButton, GUILayout.ExpandWidth(false) );
					if( pmx2fbxConfig.freezeRigidBodyList[i].boneName == null ) {
						pmx2fbxConfig.freezeRigidBodyList[i].boneName = "";
					}
					GUILayout.Label( "BoneName", GUILayout.ExpandWidth(false) );
					GUILayout.Space( 4.0f );
					pmx2fbxConfig.freezeRigidBodyList[i].boneName = EditorGUILayout.TextField( pmx2fbxConfig.freezeRigidBodyList[i].boneName  );
					pmx2fbxConfig.freezeRigidBodyList[i].recursively = GUILayout.Toggle( pmx2fbxConfig.freezeRigidBodyList[i].recursively != 0, "Recursively" ) ? 1 : 0;
					GUILayout.EndHorizontal();
					if( isRemove ) {
						pmx2fbxConfig.freezeRigidBodyList.RemoveAt( i );
					} else {
						++i;
					}
				}
			}
			
			{
				GUILayout.BeginHorizontal();
				GUILayout.Space( 20.0f );
				bool isAdd = GUILayout.Button("+", EditorStyles.miniButton, GUILayout.ExpandWidth(false) );
				GUILayout.Label( "Add" );
				GUILayout.EndHorizontal();
				
				if( isAdd ) {
					PMX2FBXConfig.FreezeRigidBody disableRigidBody = new PMX2FBXConfig.FreezeRigidBody();
					disableRigidBody.boneName = "";
					disableRigidBody.recursively = 0;
					if( pmx2fbxConfig.freezeRigidBodyList == null ) {
						pmx2fbxConfig.freezeRigidBodyList = new List<PMX2FBXConfig.FreezeRigidBody>();
					}
					pmx2fbxConfig.freezeRigidBodyList.Add( disableRigidBody );
				}
			}
			
			EditorGUILayout.Separator();
			EditorGUIUtility.LookLikeControls();
			GUILayout.Label( "Freeze Motion List", EditorStyles.boldLabel );
			
			if( pmx2fbxConfig.freezeMotionList != null ) {
				for( int i = 0; i < pmx2fbxConfig.freezeMotionList.Count; ) {
					GUILayout.BeginHorizontal();
					GUILayout.Space( 20.0f );
					bool isRemove = GUILayout.Button("-", EditorStyles.miniButton, GUILayout.ExpandWidth(false) );
					if( pmx2fbxConfig.freezeMotionList[i].boneName == null ) {
						pmx2fbxConfig.freezeMotionList[i].boneName = "";
					}
					GUILayout.Label( "BoneName", GUILayout.ExpandWidth(false) );
					GUILayout.Space( 4.0f );
					pmx2fbxConfig.freezeMotionList[i].boneName = EditorGUILayout.TextField( pmx2fbxConfig.freezeMotionList[i].boneName );
					pmx2fbxConfig.freezeMotionList[i].recursively = GUILayout.Toggle( pmx2fbxConfig.freezeMotionList[i].recursively != 0, "Recursively" ) ? 1 : 0;
					GUILayout.EndHorizontal();
					if( isRemove ) {
						pmx2fbxConfig.freezeMotionList.RemoveAt( i );
					} else {
						++i;
					}
				}
			}

			{
				GUILayout.BeginHorizontal();
				GUILayout.Space( 20.0f );
				bool isAdd = GUILayout.Button("+", EditorStyles.miniButton, GUILayout.ExpandWidth(false) );
				GUILayout.Label( "Add" );
				GUILayout.EndHorizontal();
				
				if( isAdd ) {
					PMX2FBXConfig.FreezeMotion freezeMotion = new PMX2FBXConfig.FreezeMotion();
					freezeMotion.boneName = "";
					freezeMotion.recursively = 0;
					if( pmx2fbxConfig.freezeMotionList == null ) {
						pmx2fbxConfig.freezeMotionList = new List<PMX2FBXConfig.FreezeMotion>();
					}
					pmx2fbxConfig.freezeMotionList.Add( freezeMotion );
				}
			}

			EditorGUILayout.Separator();
		}

		GUILayout.Label( "PMX/PMD", EditorStyles.boldLabel );
		GUILayout.BeginHorizontal();
		GUILayout.Space( 26.0f );
		
		Object pmxAsset = (this.pmx2fbxProperty != null) ? this.pmx2fbxProperty.pmxAsset : null;
		pmxAsset = EditorGUILayout.ObjectField( (Object)pmxAsset, typeof(Object), false );
		if( pmxAsset != null ) {
			string pmxExtension = Path.GetExtension( AssetDatabase.GetAssetPath( pmxAsset ) ).ToLower();
			if( pmxExtension == ".pmx" || pmxExtension == ".pmd" ) {
				this.pmx2fbxProperty.pmxAsset = pmxAsset;
			}
		}
		
		GUILayout.EndHorizontal();

		EditorGUILayout.Separator();
		
		GUILayout.Label( "VMD", EditorStyles.boldLabel );

		if( pmx2fbxProperty != null && pmx2fbxProperty.vmdAssetList != null ) {
			for( int i = 0; i < pmx2fbxProperty.vmdAssetList.Count; ) {
				GUILayout.BeginHorizontal();
				bool isRemoved = GUILayout.Button("-", EditorStyles.miniButton, GUILayout.ExpandWidth(false) );
				Object vmdAsset = EditorGUILayout.ObjectField( pmx2fbxProperty.vmdAssetList[i], typeof(Object), false );
				GUILayout.EndHorizontal();
				if( vmdAsset != null ) {
					string vmdAssetExt = Path.GetExtension( AssetDatabase.GetAssetPath( vmdAsset ) ).ToLower();
					if( vmdAssetExt == ".vmd" ) {
						pmx2fbxProperty.vmdAssetList[i] = vmdAsset;
					}
				} else {
					isRemoved = true;
				}
				if( isRemoved ) {
					pmx2fbxProperty.vmdAssetList.RemoveAt( i );
				} else {
					++i;
				}
			}
		}
		
		{
			GUILayout.BeginHorizontal();
			GUILayout.Space( 26.0f );
			Object vmdAsset = EditorGUILayout.ObjectField( (Object)null, typeof(Object), false );
			if( vmdAsset != null ) {
				string vmdAssetExt = Path.GetExtension( AssetDatabase.GetAssetPath( vmdAsset ) ).ToLower();
				if( vmdAssetExt == ".vmd" ) {
					if( pmx2fbxProperty.vmdAssetList == null ) {
						pmx2fbxProperty.vmdAssetList = new List<Object>();
					}
					pmx2fbxProperty.vmdAssetList.Add( vmdAsset );
				}
			}
			GUILayout.EndHorizontal();
		}

		EditorGUILayout.Separator();
		
		GUILayout.Label( "FBX Path", EditorStyles.boldLabel );
		GUILayout.BeginHorizontal();
		GUILayout.Space( 26.0f );
		mmd4MecanimProperty.fbxOutputPath = EditorGUILayout.TextField( mmd4MecanimProperty.fbxOutputPath );
		GUILayout.EndHorizontal();
		
		if( Application.platform == RuntimePlatform.WindowsEditor ) {
			// Nothing.
		} else {
			GUILayout.Label( "Wine", EditorStyles.boldLabel );

			GUILayout.BeginHorizontal();
			GUILayout.Space( 26.0f );
			mmd4MecanimProperty.useWineFlag = EditorGUILayout.Toggle( "Enabled", mmd4MecanimProperty.useWineFlag );
			GUILayout.EndHorizontal();
			
			if( !this.isProcessing ) {
				if( !mmd4MecanimProperty.useWineFlag ) {
					GUI.enabled = false;
				}
			}
			GUILayout.BeginHorizontal();
			GUILayout.Space( 26.0f );
			mmd4MecanimProperty.wine = (PMX2FBXConfig.Wine)EditorGUILayout.EnumPopup( "Type", (System.Enum)mmd4MecanimProperty.wine );
			GUILayout.EndHorizontal();
			string winePath = WinePaths[(int)mmd4MecanimProperty.wine];
			if( mmd4MecanimProperty.wine == PMX2FBXConfig.Wine.Manual ) {
				GUILayout.BeginHorizontal();
				GUILayout.Space( 26.0f );
				mmd4MecanimProperty.winePath = EditorGUILayout.TextField( mmd4MecanimProperty.winePath );
				winePath = mmd4MecanimProperty.winePath;
				GUILayout.EndHorizontal();
			}
			if( !File.Exists( winePath ) ) {
				GUILayout.BeginHorizontal();
				GUILayout.Space( 26.0f );
				EditorGUILayout.LabelField( "! Not found Wine path." );
				GUILayout.EndHorizontal();
			}
			if( !this.isProcessing ) {
				if( !mmd4MecanimProperty.useWineFlag ) {
					GUI.enabled = true;
				}
			}
		}
		
		EditorGUILayout.Separator();
		
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		{
			bool isRevert = false;
			bool isApply = false;
			bool isProcess = false;

			if( this._editorAdvancedMode ) {
				isRevert = GUILayout.Button("Revert", GUILayout.ExpandWidth(false));
				isApply = GUILayout.Button("Apply", GUILayout.ExpandWidth(false));
			}

			isProcess = GUILayout.Button("Process", GUILayout.ExpandWidth(false));
			if( isRevert ) {
				this.pmx2fbxConfig = null;
				this.SetupWithReload();
			} else if( isApply ) {
				this.SavePMX2FBXConfig();
			} else if( isProcess ) {
				this.fbxAsset = null;
				this.fbxAssetPath = null;
				this.materialList = null;
				this.mmdModel = null;
				this.mmdModelLastWriteTime = new System.DateTime();
				this.pmx2fbxConfig.mmd4MecanimProperty.fbxAssetPath = null;
				_initializeMaterialAtLeastOnce = false; // Added.
				_initializeMaterialAfterPMX2FBX = true;
				this.SavePMX2FBXConfig();
				this.ProcessPMX2FBX();
			}
		}
		GUILayout.EndHorizontal();
	}
}
