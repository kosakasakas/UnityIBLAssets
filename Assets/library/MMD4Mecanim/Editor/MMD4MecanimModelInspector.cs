//#define _MMD4MECANIM_DEBUG_DEFAULTINSPECTOR

using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using MMDModel = MMD4MecanimImporter.MMDModel;

[CustomEditor(typeof(MMD4MecanimModel))]
public class MMD4MecanimModelInspector : Editor
{
	private static readonly string[] toolbarTitles0 = new string[] {
		"Model", "Bone", "IK", "Morph", "Anim", "Physics",
	};

	private static readonly string[] toolbarTitles1 = new string[] {
	};

	private bool _initialized;
#if _MMD4MECANIM_DEBUG_DEFAULTINSPECTOR
	private bool _defaultInspector;
#endif

	public override void OnInspectorGUI()
	{
		MMD4MecanimModel model = this.target as MMD4MecanimModel;

		if( string.IsNullOrEmpty( AssetDatabase.GetAssetPath( model ) ) ) { // Bugfix: Broken prefab.
			model.InitializeOnEditor();
		}

		_Initialize();

#if _MMD4MECANIM_DEBUG_DEFAULTINSPECTOR
		_defaultInspector = GUILayout.Toggle( _defaultInspector, "DefaultInspector" );
		if( _defaultInspector ) {
			DrawDefaultInspector();
			return;
		}
#endif

		int editViewPage0 = -1, editViewPage0Old = -1;
		int editViewPage1 = -1, editViewPage1Old = -1;
		if( (int)model.editorViewPage < toolbarTitles0.Length ) {
			editViewPage0 = editViewPage0Old = (int)model.editorViewPage;
		} else {
			editViewPage1 = editViewPage1Old = (int)model.editorViewPage - toolbarTitles0.Length;
		}

		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		editViewPage0 = GUILayout.Toolbar( editViewPage0, toolbarTitles0 );
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		editViewPage1 = GUILayout.Toolbar( editViewPage1, toolbarTitles1 );
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();

		if( editViewPage0 != editViewPage0Old ) {
			model.editorViewPage = (MMD4MecanimModel.EditorViewPage)(editViewPage0);
		}
		if( editViewPage1 != editViewPage1Old ) {
			model.editorViewPage = (MMD4MecanimModel.EditorViewPage)(editViewPage1 + toolbarTitles0.Length);
		}

		EditorGUI.BeginChangeCheck();

		switch( model.editorViewPage ) {
		case MMD4MecanimModel.EditorViewPage.Model:
			_DrawModelGUI();
			break;
		case MMD4MecanimModel.EditorViewPage.Bone:
			_DrawBoneGUI();
			break;
		case MMD4MecanimModel.EditorViewPage.IK:
			_DrawIKGUI();
			break;
		case MMD4MecanimModel.EditorViewPage.Morph:
			_DrawMorphGUI();
			break;
		case MMD4MecanimModel.EditorViewPage.Anim:
			_DrawAnimGUI();
			break;
		case MMD4MecanimModel.EditorViewPage.Physics:
			_DrawPhysicsGUI();
			break;
		}

		if( EditorGUI.EndChangeCheck() ) {
			EditorUtility.SetDirty( this.target );
		}
	}

	private void _Initialize()
	{
		if( _initialized ) {
			return;
		}
		
		_initialized = true;
		
		MMD4MecanimModel model = this.target as MMD4MecanimModel;

		Mesh mesh = model.defaultMesh;
		if( mesh == null ) {
			Debug.LogWarning( "defaultMesh is null." );
			return;
		}
		
		string fbxAssetPath = AssetDatabase.GetAssetPath( mesh );
		
		if( model.modelFile == null ) {
			if( !string.IsNullOrEmpty( fbxAssetPath ) ) {
				string modelAssetPath = System.IO.Path.GetDirectoryName( fbxAssetPath ) + "/"
					+ System.IO.Path.GetFileNameWithoutExtension( fbxAssetPath )
					+ ".model.bytes";
				
				model.modelFile = AssetDatabase.LoadAssetAtPath( modelAssetPath, typeof(TextAsset) ) as TextAsset;
			}
		}
		if( model.skinningEnabled ) {
			if( model.indexFile == null ) {
				if( !string.IsNullOrEmpty( fbxAssetPath ) ) {
					string indexAssetPath = System.IO.Path.GetDirectoryName( fbxAssetPath ) + "/"
						+ System.IO.Path.GetFileNameWithoutExtension( fbxAssetPath )
						+ ".index.bytes";
					
					model.indexFile = AssetDatabase.LoadAssetAtPath( indexAssetPath, typeof(TextAsset) ) as TextAsset;
				}
			}
		}
	}

	private void _DrawModelGUI()
	{
		MMD4MecanimModel model = this.target as MMD4MecanimModel;
		
		model.initializeOnAwake = EditorGUILayout.Toggle( "Initialize On Awake", model.initializeOnAwake );
		model.postfixRenderQueue = EditorGUILayout.Toggle( "Postfix Render Queue", model.postfixRenderQueue );
		model.updateWhenOffscreen = EditorGUILayout.Toggle( "Update When Offscreen", model.updateWhenOffscreen );
		#if MMD4MECANIM_DEBUG
		EditorGUILayout.FloatField( "Import Scale", model.importScale );
		#endif

		{
			TextAsset modelFile = model.modelFile;
			modelFile = (TextAsset)EditorGUILayout.ObjectField( "Model File", (Object)modelFile, typeof(TextAsset), false );
			if( modelFile != null ) {
				if( !AssetDatabase.GetAssetPath( modelFile ).ToLower().EndsWith( ".model.bytes" ) ) {
					modelFile = null;
				} else {
					model.modelFile = modelFile;
				}
			} else {
				model.modelFile = modelFile;
			}
		}
		
		{
			TextAsset indexFile = model.indexFile;
			indexFile = (TextAsset)EditorGUILayout.ObjectField( "Index File", (Object)indexFile, typeof(TextAsset), false );
			if( indexFile != null ) {
				if( !AssetDatabase.GetAssetPath( indexFile ).ToLower().EndsWith( ".index.bytes" ) ) {
					indexFile = null;
				} else {
					model.indexFile = indexFile;
				}
			} else {
				model.indexFile = indexFile;
			}
		}

		{
			TextAsset vertexFile = model.vertexFile;
			vertexFile = (TextAsset)EditorGUILayout.ObjectField( "Vertex File", (Object)vertexFile, typeof(TextAsset), false );
			if( vertexFile != null ) {
				if( !AssetDatabase.GetAssetPath( vertexFile ).ToLower().EndsWith( ".vertex.bytes" ) ) {
					vertexFile = null;
				} else {
					model.vertexFile = vertexFile;
				}
			} else {
				model.vertexFile = vertexFile;
			}
		}

		model.audioSource = (AudioSource)EditorGUILayout.ObjectField( "Audio Source", (Object)model.audioSource, typeof(AudioSource), true );
		
		model.physicsEngine = (MMD4MecanimModel.PhysicsEngine)EditorGUILayout.EnumPopup( "Physics Engine", (System.Enum)model.physicsEngine );
	}

	private void _DrawBoneGUI()
	{
		MMD4MecanimModel model = this.target as MMD4MecanimModel;
		
		// DisplayFrame
		
		MMD4MecanimEditorCommon.LookLikeInspector();
		int boneListLength = 0;
		if( model.boneList != null ) {
			boneListLength = model.boneList.Length;
		}

		EditorGUILayout.Separator();

		model.xdefEnabled			= EditorGUILayout.Toggle( "XDEF Enabled", model.xdefEnabled );
		model.xdefMobileEnabled		= EditorGUILayout.Toggle( "XDEF Mobile Enabled", model.xdefMobileEnabled );

		EditorGUILayout.Separator();

		model.boneInherenceEnabled	= EditorGUILayout.Toggle( "BoneInherenceEnabled", model.boneInherenceEnabled );
		model.boneMorphEnabled		= EditorGUILayout.Toggle( "BoneMorphEnabled", model.boneMorphEnabled );

		EditorGUILayout.Separator();

		model.pphEnabled			= EditorGUILayout.Toggle( "PPHEnabled", model.pphEnabled );
		model.pphEnabledNoAnimation	= EditorGUILayout.Toggle( "PPHEnabledNoAnimation", model.pphEnabledNoAnimation );

		EditorGUILayout.Separator();

		model.pphShoulderEnabled	= EditorGUILayout.Toggle( "PPHShoulderEnabled", model.pphShoulderEnabled );
		model.pphShoulderFixRate	= EditorGUILayout.Slider( "PPHShoulderFixRate", model.pphShoulderFixRate, 0.0f, 1.0f ); 

		EditorGUILayout.Separator();

		EditorGUILayout.TextField( "Size", boneListLength.ToString() );
		for( int i = 0; i < boneListLength; ++i ) {
			string name = i.ToString();
			if( model.modelData != null && model.modelData.boneDataList != null && i < model.modelData.boneDataList.Length ) {
				name = name + "." + model.modelData.boneDataList[i].nameJp;
			}
			GameObject boneGameObject = (model.boneList[i] != null) ? model.boneList[i].gameObject : null;
			EditorGUILayout.ObjectField( name, (Object)boneGameObject, typeof(GameObject), true );
		}
	}

	private void _DrawIKGUI()
	{
		MMD4MecanimModel model = this.target as MMD4MecanimModel;
		
		// DisplayFrame
		
		MMD4MecanimEditorCommon.LookLikeInspector();
		int ikListLength = 0;
		if( model.ikList != null ) {
			ikListLength = model.ikList.Length;
		}
		
		EditorGUILayout.Separator();
		
		model.ikEnabled = EditorGUILayout.Toggle( "IKEnabled", model.ikEnabled );
		
		EditorGUILayout.Separator();
		
		EditorGUILayout.TextField( "Size", ikListLength.ToString() );
		for( int i = 0; i < ikListLength; ++i ) {
			MMD4MecanimModel.IK ik = model.ikList[i];
			if( ik != null ) {
				string name = i.ToString();
				if( ik.destBone != null && ik.destBone.boneData != null ) {
					if( ik.destBone.boneData.nameJp != null ) {
						name = name + "." + ik.destBone.boneData.nameJp;
					}
				}

				EditorGUILayout.BeginHorizontal();
				ik.ikEnabled = GUILayout.Toggle(ik.ikEnabled, name);
				GUILayout.FlexibleSpace();
				name = "";
				GameObject boneGameObject = (ik.destBone != null) ? ik.destBone.gameObject : null;
				EditorGUILayout.ObjectField( name, (Object)boneGameObject, typeof(GameObject), true );
				EditorGUILayout.EndHorizontal();
			}
		}
	}
	
	private void _DrawMorphGUI()
	{
		MMD4MecanimModel model = this.target as MMD4MecanimModel;

		model.vertexMorphEnabled	= EditorGUILayout.Toggle( "Vertex Morph Enabled", model.vertexMorphEnabled );
		model.blendShapesEnabled	= EditorGUILayout.Toggle( "Blend Shapes Enabled", model.blendShapesEnabled );
		model.supportNEXTEdge		= EditorGUILayout.Toggle( "Support NEXT Edge", model.supportNEXTEdge );
		model.nextEdgePass			= (MMD4MecanimModel.NEXTEdgePass)EditorGUILayout.EnumPopup( model.nextEdgePass );
		model.nextEdgeSize			= EditorGUILayout.FloatField( "NEXT Edge Size", model.nextEdgeSize );
		model.nextEdgeColor			= EditorGUILayout.ColorField( "NEXT Edge Color", model.nextEdgeColor );

		EditorGUILayout.Separator();

		bool updatedAnything = false;
		if( model.modelData != null && model.modelData.morphDataList != null ) {
			for( int catIndex = 1; catIndex < 5; ++catIndex ) {
				MMD4MecanimData.MorphCategory morphCategory = (MMD4MecanimData.MorphCategory)catIndex;
				bool isVisible = (model.editorViewMorphBits & (1 << (catIndex - 1))) != 0;

				isVisible = EditorGUILayout.ToggleLeft( morphCategory.ToString(), isVisible );
				if( isVisible ) {
					model.editorViewMorphBits |= unchecked((byte)(1 << (catIndex - 1)));
				} else {
					model.editorViewMorphBits &= unchecked((byte)~(1 << (catIndex - 1)));
				}

				if( isVisible ) {
					for( int morphIndex = 0; morphIndex < model.modelData.morphDataList.Length; ++morphIndex ) {
						if( model.modelData.morphDataList[morphIndex].morphCategory == morphCategory ) {
							string name = model.modelData.morphDataList[morphIndex].nameJp;
							if( model.morphList != null && (uint)morphIndex < model.morphList.Length ) {
								MMD4MecanimModel.Morph morph = model.morphList[morphIndex];
								float weight = morph.weight;
								morph.weight = EditorGUILayout.Slider( name, weight, 0.0f, 1.0f );
								updatedAnything |= (weight != morph.weight);
							}
						}
					}
				}
			}
		} else {
			if( model.morphList != null ) {
				foreach( MMD4MecanimModel.Morph morph in model.morphList ) {
					float weight = morph.weight;
					morph.weight = EditorGUILayout.Slider( morph.name, morph.weight, 0.0f, 1.0f );
					updatedAnything |= (weight != morph.weight);
				}
			}
		}

		if( updatedAnything ) {
			model.ForceUpdateMorph();
		}
	}

	private void _DrawAnimGUI()
	{
		MMD4MecanimModel model = this.target as MMD4MecanimModel;

		model.animEnabled = GUILayout.Toggle( model.animEnabled, "Enabled" );
		
		GUI.enabled = model.animEnabled;

		model.animSyncToAudio = GUILayout.Toggle( model.animSyncToAudio, "Sync To Audio" );
		
		if( model.animList == null ) {
			model.animList = new MMD4MecanimModel.Anim[0];
		}
		
		//EditorGUILayout.Separator();
		if( model.animList != null ) {
			if( model.animList.Length > 0 ) {
				GUILayout.Label( "Animations", EditorStyles.boldLabel );
			}
			for( int animIndex = 0; animIndex < model.animList.Length; ) {
				MMD4MecanimModel.Anim anim = model.animList[animIndex];
				TextAsset animFile = anim.animFile;
				EditorGUILayout.BeginHorizontal();
				bool isRemove = GUILayout.Button("-", EditorStyles.miniButton, GUILayout.ExpandWidth(false) );
				animFile = (TextAsset)EditorGUILayout.ObjectField( "Anim File", (Object)animFile, typeof(TextAsset), false );
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.BeginHorizontal();
				GUILayout.Space(26.0f);
				anim.animatorStateName = EditorGUILayout.TextField( "Animator State Name", anim.animatorStateName );
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.BeginHorizontal();
				GUILayout.Space(26.0f);
				anim.audioClip = (AudioClip)EditorGUILayout.ObjectField( "Audio Clip", (AudioClip)anim.audioClip, typeof(AudioClip), false );
				EditorGUILayout.EndHorizontal();
				if( animFile != null ) {
					if( !AssetDatabase.GetAssetPath( animFile ).ToLower().EndsWith( ".anim.bytes" ) ) {
						animFile = null;
					} else {
						if( anim.animFile != animFile ) {
							anim.animFile = animFile;
							anim.animatorStateName = "Base Layer." + System.IO.Path.GetFileNameWithoutExtension( anim.animFile.name ) + ".vmd";
						}
					}
				} else {
					isRemove = true;
					anim.animFile = null;
					anim.animatorStateName = "";
				}
				if( isRemove ) {
					for( int i = animIndex; i + 1 < model.animList.Length; ++i ) {
						model.animList[i] = model.animList[i + 1];
					}
					System.Array.Resize( ref model.animList, model.animList.Length - 1 );
				} else {
					++animIndex;
				}
			}
		}

		EditorGUILayout.Separator();
		
		{
			GUILayout.Label( "Add Animation", EditorStyles.boldLabel );
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(26.0f);
			TextAsset animFile = (TextAsset)EditorGUILayout.ObjectField( "Anim File", (Object)null, typeof(TextAsset), false );
			EditorGUILayout.EndHorizontal();
			if( animFile != null ) {
				if( !AssetDatabase.GetAssetPath( animFile ).ToLower().EndsWith( ".anim.bytes" ) ) {
					Debug.LogWarning( System.IO.Path.GetExtension( AssetDatabase.GetAssetPath( animFile ) ).ToLower() );
					animFile = null;
				} else {
					MMD4MecanimModel.Anim anim = new MMD4MecanimModel.Anim();
					anim.animFile = animFile;
					anim.animatorStateName = "Base Layer." + System.IO.Path.GetFileNameWithoutExtension( anim.animFile.name ) + ".vmd";
					if( model.animList == null ) {
						model.animList = new MMD4MecanimModel.Anim[1];
						model.animList[0] = anim;
					} else {
						int animIndex = model.animList.Length;
						System.Array.Resize( ref model.animList, animIndex + 1 );
						model.animList[animIndex] = anim;
					}
				}
			}
		}
	}

	public enum PopupDetail {
		RigidBodyType,
	}

	private void _DrawPhysicsGUI()
	{
		MMD4MecanimModel model = this.target as MMD4MecanimModel;
		
		GUILayout.Label( "Model", EditorStyles.boldLabel );
		model.physicsEngine = (MMD4MecanimModel.PhysicsEngine)EditorGUILayout.EnumPopup( "Physics Engine", (System.Enum)model.physicsEngine );
		EditorGUILayout.Separator();

		GUILayout.Label( "Colliders", EditorStyles.boldLabel );

		EditorGUILayout.BeginHorizontal();
		GUILayout.Space( 20.0f );
		model.generatedColliderMargin = EditorGUILayout.FloatField( "Margin", model.generatedColliderMargin );
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.BeginHorizontal();
		GUILayout.Space( 20.0f );
		EditorGUILayout.LabelField("Generate Colliders");
		if( GUILayout.Button("Process", EditorStyles.miniButton) ) {
			_GenerateColliders();
		}
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.BeginHorizontal();
		GUILayout.Space( 20.0f );
		EditorGUILayout.LabelField("Remove Colliders");
		if( GUILayout.Button("Process", EditorStyles.miniButton) ) {
			_RemoveColliders();
		}
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.BeginHorizontal();
		GUILayout.Space( 20.0f );
		GUI.enabled = false;
		GUILayout.Label("Note: This function will be changed later.", EditorStyles.miniLabel);
		GUI.enabled = true;
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.Separator();

		model.editorViewRigidBodies = EditorGUILayout.Foldout( model.editorViewRigidBodies, "RigidBodies" );
		{
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space( 20.0f );

			EditorGUILayout.BeginVertical();
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label( "Name" );
			EditorGUILayout.EndHorizontal();
			if( model.editorViewRigidBodies && model.rigidBodyList != null ) {
				for( int i = 0; i < model.rigidBodyList.Length; ++i ) {
					MMD4MecanimModel.RigidBody rigidBody = model.rigidBodyList[i];
					if( rigidBody != null && rigidBody.rigidBodyData != null ) {
						EditorGUILayout.BeginHorizontal();
						string nameJp = rigidBody.rigidBodyData.nameJp;
						nameJp = nameJp.Replace( "\n", "" );
						rigidBody.freezed = !GUILayout.Toggle( !rigidBody.freezed, nameJp );
						EditorGUILayout.EndHorizontal();
					}
				}
			}
			EditorGUILayout.EndVertical();

			float fieldWidth = 40.0f;
			EditorGUILayout.BeginVertical();
			EditorGUILayout.BeginHorizontal();
			bool guiEnabled = GUI.enabled;
			GUI.enabled = false;
			EditorGUILayout.EnumPopup( PopupDetail.RigidBodyType );
			EditorGUILayout.TextField( "Mass", GUILayout.MaxWidth( fieldWidth ) );
			EditorGUILayout.TextField( "LinD", GUILayout.MaxWidth( fieldWidth ) );
			EditorGUILayout.TextField( "AngD", GUILayout.MaxWidth( fieldWidth ) );
			EditorGUILayout.TextField( "Rest", GUILayout.MaxWidth( fieldWidth ) );
			EditorGUILayout.TextField( "Fric", GUILayout.MaxWidth( fieldWidth ) );
			GUI.enabled = guiEnabled;
			EditorGUILayout.EndHorizontal();
			if( model.editorViewRigidBodies && model.rigidBodyList != null ) {
				for( int i = 0; i < model.rigidBodyList.Length; ++i ) {
					MMD4MecanimModel.RigidBody rigidBody = model.rigidBodyList[i];
					if( rigidBody != null && rigidBody.rigidBodyData != null ) {
						EditorGUILayout.BeginHorizontal();
						EditorGUILayout.EnumPopup( rigidBody.rigidBodyData.rigidBodyType );
						EditorGUILayout.FloatField( "", rigidBody.rigidBodyData.mass,			GUILayout.MaxWidth( fieldWidth ) );
						EditorGUILayout.FloatField( "", rigidBody.rigidBodyData.linearDamping,	GUILayout.MaxWidth( fieldWidth ) );
						EditorGUILayout.FloatField( "", rigidBody.rigidBodyData.angularDamping,	GUILayout.MaxWidth( fieldWidth ) );
						EditorGUILayout.FloatField( "", rigidBody.rigidBodyData.restitution,	GUILayout.MaxWidth( fieldWidth ) );
						EditorGUILayout.FloatField( "", rigidBody.rigidBodyData.friction,		GUILayout.MaxWidth( fieldWidth ) );
						EditorGUILayout.EndHorizontal();
					}
				}
			}
			EditorGUILayout.EndVertical();

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Separator();
		}

		GUI.enabled = (model.physicsEngine == MMD4MecanimModel.PhysicsEngine.BulletPhysics);
		GUILayout.Label( "Bullet Physics", EditorStyles.boldLabel );
		if( model.bulletPhysics != null ) {
			model.bulletPhysics.joinLocalWorld = EditorGUILayout.Toggle( "Join Local World", model.bulletPhysics.joinLocalWorld );
			model.bulletPhysics.useOriginalScale = EditorGUILayout.Toggle( "Use Original Scale", model.bulletPhysics.useOriginalScale );
		}

		EditorGUILayout.BeginHorizontal();
		GUILayout.Space( 20.0f );
		GUILayout.Label( "Reset Time Property", EditorStyles.boldLabel );
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.BeginHorizontal();
		GUILayout.Space( 20.0f );
		model.bulletPhysics.useCustomResetTime = EditorGUILayout.Toggle( "Use Custom Reset Time", model.bulletPhysics.useCustomResetTime );
		EditorGUILayout.EndHorizontal();

		GUI.enabled = (model.physicsEngine == MMD4MecanimModel.PhysicsEngine.BulletPhysics) && model.bulletPhysics.useCustomResetTime;

		EditorGUILayout.BeginHorizontal();
		GUILayout.Space( 20.0f );
		model.bulletPhysics.resetMorphTime = EditorGUILayout.FloatField( "Reset Morph Time", model.bulletPhysics.resetMorphTime );
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.BeginHorizontal();
		GUILayout.Space( 20.0f );
		model.bulletPhysics.resetWaitTime = EditorGUILayout.FloatField( "Reset Wait Time", model.bulletPhysics.resetWaitTime );
		EditorGUILayout.EndHorizontal();

		GUI.enabled = (model.physicsEngine == MMD4MecanimModel.PhysicsEngine.BulletPhysics);

		if( model.bulletPhysics.worldProperty == null ) {
			model.bulletPhysics.worldProperty = new MMD4MecanimInternal.Bullet.WorldProperty();
		}

		if( model.bulletPhysics.worldProperty != null ) {
			var worldProperty = model.bulletPhysics.worldProperty;

			EditorGUILayout.BeginHorizontal();
			GUILayout.Space( 20.0f );
			EditorGUILayout.BeginVertical();

			GUILayout.Label( "World Property", EditorStyles.boldLabel );
			worldProperty.accurateStep = EditorGUILayout.Toggle( "Accurate Step", worldProperty.accurateStep );
			worldProperty.multiThreading = EditorGUILayout.Toggle( "Multi Threading", worldProperty.multiThreading );
			worldProperty.framePerSecond = EditorGUILayout.IntField( "Frame Per Second", worldProperty.framePerSecond );
			worldProperty.resetFrameRate = EditorGUILayout.IntField( "Reset Frame Rate", worldProperty.resetFrameRate );
			worldProperty.limitDeltaFrames = EditorGUILayout.IntField( "Limit Delta Frames", worldProperty.limitDeltaFrames );
			worldProperty.axisSweepDistance = EditorGUILayout.FloatField( "Axis Sweep Distance", worldProperty.axisSweepDistance );
			worldProperty.gravityScale = EditorGUILayout.FloatField( "Gravity Scale", worldProperty.gravityScale );
			worldProperty.gravityNoise = EditorGUILayout.FloatField( "Gravity Noise", worldProperty.gravityNoise );
			worldProperty.gravityDirection = EditorGUILayout.Vector3Field( "Gravity Direction", worldProperty.gravityDirection );
			worldProperty.vertexScale = EditorGUILayout.FloatField( "Vertex Scale", worldProperty.vertexScale );
			worldProperty.importScale = EditorGUILayout.FloatField( "Import Scale", worldProperty.importScale );
			worldProperty.worldSolverInfoNumIterations = EditorGUILayout.IntField( "Iterations", worldProperty.worldSolverInfoNumIterations );
			worldProperty.worldSolverInfoSplitImpulse = EditorGUILayout.Toggle( "Split Impulse", worldProperty.worldSolverInfoSplitImpulse );
			worldProperty.worldAddFloorPlane = EditorGUILayout.Toggle( "Add Floor Plane", worldProperty.worldAddFloorPlane );
			worldProperty.optimizeBulletXNA = EditorGUILayout.Toggle( "Optimize Bullet XNA", worldProperty.optimizeBulletXNA );

			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();
		}

		GUI.enabled = (model.physicsEngine == MMD4MecanimModel.PhysicsEngine.BulletPhysics);

		if( model.bulletPhysics.mmdModelProperty == null ) {
			model.bulletPhysics.mmdModelProperty = new MMD4MecanimInternal.Bullet.MMDModelProperty();
		}

		if( model.bulletPhysics.mmdModelProperty != null ) {
			var modelProperty = model.bulletPhysics.mmdModelProperty;

			EditorGUILayout.BeginHorizontal();
			GUILayout.Space( 20.0f );
			EditorGUILayout.BeginVertical();

			GUILayout.Label( "RigidBody Property (Construct)", EditorStyles.boldLabel );
			modelProperty.rigidBodyIsAdditionalDamping					= EditorGUILayout.Toggle( "AdditionalDamping", modelProperty.rigidBodyIsAdditionalDamping );
			modelProperty.rigidBodyIsEnableSleeping						= EditorGUILayout.Toggle( "EnableSleeping", modelProperty.rigidBodyIsEnableSleeping );
			modelProperty.rigidBodyIsUseCcd								= EditorGUILayout.Toggle( "UseCcd", modelProperty.rigidBodyIsUseCcd );
			modelProperty.rigidBodyCcdMotionThreshold					= EditorGUILayout.FloatField( "CcdMotionThreshold", modelProperty.rigidBodyCcdMotionThreshold );
			modelProperty.rigidBodyShapeScale							= EditorGUILayout.FloatField( "ShapeScale", modelProperty.rigidBodyShapeScale );
			modelProperty.rigidBodyMassRate								= EditorGUILayout.FloatField( "MassRate", modelProperty.rigidBodyMassRate );
			modelProperty.rigidBodyLinearDampingRate					= EditorGUILayout.FloatField( "LinearDampingRate", modelProperty.rigidBodyLinearDampingRate );
			modelProperty.rigidBodyAngularDampingRate					= EditorGUILayout.FloatField( "AngularDampingRate", modelProperty.rigidBodyAngularDampingRate );
			modelProperty.rigidBodyRestitutionRate						= EditorGUILayout.FloatField( "RestitutionRate", modelProperty.rigidBodyRestitutionRate );
			modelProperty.rigidBodyFrictionRate							= EditorGUILayout.FloatField( "FrictionRate", modelProperty.rigidBodyFrictionRate );

			GUILayout.Label( "RigidBody Property (Prefix 2.79 to 2.75)", EditorStyles.boldLabel );
			modelProperty.rigidBodyLinearDampingLossRate				= EditorGUILayout.FloatField( "LinearDampingLossRate", modelProperty.rigidBodyLinearDampingLossRate );
			modelProperty.rigidBodyLinearDampingLimit					= EditorGUILayout.FloatField( "LinearDampingLimit", modelProperty.rigidBodyLinearDampingLimit );
			modelProperty.rigidBodyAngularDampingLossRate				= EditorGUILayout.FloatField( "AngularDampingLossRate", modelProperty.rigidBodyAngularDampingLossRate );
			modelProperty.rigidBodyAngularDampingLimit					= EditorGUILayout.FloatField( "AngularDampingLimit", modelProperty.rigidBodyAngularDampingLimit );

			GUILayout.Label( "RigidBody Property (Velocity Limit)", EditorStyles.boldLabel );
			modelProperty.rigidBodyLinearVelocityLimit					= EditorGUILayout.FloatField( "LinearVelocityLimit", modelProperty.rigidBodyLinearVelocityLimit );
			modelProperty.rigidBodyAngularVelocityLimit					= EditorGUILayout.FloatField( "AngularVelocityLimit", modelProperty.rigidBodyAngularVelocityLimit );

			GUILayout.Label( "RigidBody Property (Correction)", EditorStyles.boldLabel );
			modelProperty.rigidBodyAntiJitterRate						= EditorGUILayout.Slider( "AntiJitterRate", modelProperty.rigidBodyAntiJitterRate, 0.0f, 1.0f );
			modelProperty.rigidBodyAntiJitterRateOnKinematic			= EditorGUILayout.Slider( "AntiJitterRateOnKinematic", modelProperty.rigidBodyAntiJitterRateOnKinematic, 0.0f, 1.0f );
			modelProperty.rigidBodyPreBoneAlignmentLimitLength			= EditorGUILayout.FloatField( "PreBoneAlignmentLimitLength", modelProperty.rigidBodyPreBoneAlignmentLimitLength );
			modelProperty.rigidBodyPreBoneAlignmentLossRate				= EditorGUILayout.FloatField( "PreBoneAlignmentLossRate", modelProperty.rigidBodyPreBoneAlignmentLossRate );
			modelProperty.rigidBodyPostBoneAlignmentLimitLength			= EditorGUILayout.FloatField( "PostBoneAlignmentLimitLength", modelProperty.rigidBodyPostBoneAlignmentLimitLength );
			modelProperty.rigidBodyPostBoneAlignmentLossRate			= EditorGUILayout.FloatField( "PostBoneAlignmentLossRate", modelProperty.rigidBodyPostBoneAlignmentLossRate );

			GUILayout.Label( "RigidBody Property (Force Angular Limit)", EditorStyles.boldLabel );
			modelProperty.rigidBodyIsUseForceAngularVelocityLimit		= EditorGUILayout.Toggle( "UseForceAngularVelocityLimit", modelProperty.rigidBodyIsUseForceAngularVelocityLimit );
			modelProperty.rigidBodyIsUseForceAngularAccelerationLimit	= EditorGUILayout.Toggle( "UseForceAngularAccelerationLimit", modelProperty.rigidBodyIsUseForceAngularAccelerationLimit );
			modelProperty.rigidBodyForceAngularVelocityLimit			= EditorGUILayout.FloatField( "ForceAngularVelocityLimit", modelProperty.rigidBodyForceAngularVelocityLimit );

			GUILayout.Label( "RigidBody Property (Additional Collider)", EditorStyles.boldLabel );
			modelProperty.rigidBodyIsAdditionalCollider					= EditorGUILayout.Toggle( "AdditionalCollider", modelProperty.rigidBodyIsAdditionalCollider );
			modelProperty.rigidBodyAdditionalColliderBias				= EditorGUILayout.FloatField( "AdditionalColliderBias", modelProperty.rigidBodyAdditionalColliderBias );

			GUILayout.Label( "RigidBody Property (Other)", EditorStyles.boldLabel );
			modelProperty.rigidBodyIsForceTranslate						= EditorGUILayout.Toggle( "ForceTranslate", modelProperty.rigidBodyIsForceTranslate );

			GUILayout.Label( "Joint Property", EditorStyles.boldLabel );
			modelProperty.jointRootAdditionalLimitAngle					= EditorGUILayout.FloatField( "RootAdditionalLimitAngle", modelProperty.jointRootAdditionalLimitAngle );

			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();
		}
	}

	static GameObject _WeakCreateChildCollider( GameObject parentGameObject, string name )
	{
		foreach( Transform child in parentGameObject.transform ) {
			if( child.name == name ) {
				return child.gameObject;
			}
		}

		GameObject go = new GameObject( name );
		go.transform.parent = parentGameObject.transform;
		return go;
	}

	static Type _WeakAddCollider<Type>( GameObject go )
		where Type : Collider
	{
		Type t = go.GetComponent<Type>();
		if( t == default(Type) ) {
			t = go.AddComponent<Type>();
			t.isTrigger = true;
		}

		return t;
	}

	private void _GenerateColliders()
	{
		MMD4MecanimModel model = this.target as MMD4MecanimModel;
		if( model.modelData == null ) {
			return;
		}

		MMD4MecanimData.RigidBodyData[] rigidBodyDataList = model.modelData.rigidBodyDataList;
		if( rigidBodyDataList == null ) {
			return;
		}

		float margin = Mathf.Max( model.generatedColliderMargin, 0.0f );

		for( int n = 0; n != rigidBodyDataList.Length; ++n ) {
			MMD4MecanimData.RigidBodyData rigidBodyData = rigidBodyDataList[n];
			if( rigidBodyData != null && rigidBodyData.boneID >= 0 ) {
				if( model.boneList != null && rigidBodyData.boneID < model.boneList.Length ) {
					MMD4MecanimBone bone = model.boneList[rigidBodyData.boneID];

					float vertexScale = model.modelData.vertexScale;
					float importScale = model.modelData.importScale;

					Vector3 shapeSize = rigidBodyData.shapeSize * vertexScale * importScale;
					Vector3 position = rigidBodyData.position * vertexScale * importScale;
					Vector3 rotation = rigidBodyData.rotation;
					
					GameObject parentGameObject = bone.gameObject;
					if( parentGameObject == null ) {
						continue;
					}

					GameObject rigidBodyGameObject = _WeakCreateChildCollider( parentGameObject, "Coll." + n + "." + parentGameObject.name );
					rigidBodyGameObject.transform.localPosition = Vector3.zero;
					rigidBodyGameObject.transform.localRotation = Quaternion.identity;
					rigidBodyGameObject.transform.localScale = Vector3.one;
					
					// LH to RH
					position.z = -position.z;
					rotation.x = -rotation.x;
					rotation.y = -rotation.y;
					
					BulletXNA.LinearMath.IndexedMatrix boneTransform;
					
					boneTransform._basis = MMD4MecanimInternal.Bullet.Math.BasisRotationYXZ( ref rotation );
					boneTransform._origin = position;
					
					Vector3 geomPosition = boneTransform._origin;
					var geomRotation = boneTransform.GetRotation();
					
					Quaternion quaternion = new Quaternion(
						geomRotation.X,
						-geomRotation.Y,
						-geomRotation.Z,
						geomRotation.W );
					
					rigidBodyGameObject.transform.localRotation = quaternion;
					rigidBodyGameObject.transform.localPosition = new Vector3( -geomPosition[0], geomPosition[1], geomPosition[2] );
					
					switch( rigidBodyData.shapeType ) {
					case MMD4MecanimData.ShapeType.Sphere:
						{
							SphereCollider sphereCollider = _WeakAddCollider<SphereCollider>( rigidBodyGameObject );
							sphereCollider.radius = shapeSize.x + margin;
						}
						break;
					case MMD4MecanimData.ShapeType.Box:
						{
							BoxCollider boxCollider = _WeakAddCollider<BoxCollider>( rigidBodyGameObject );
							boxCollider.size = shapeSize * 2.0f + new Vector3( margin, margin, margin ) * 2.0f;
						}
						break;
					case MMD4MecanimData.ShapeType.Capsule:
						{
							CapsuleCollider capsuleCollider = _WeakAddCollider<CapsuleCollider>( rigidBodyGameObject );
							capsuleCollider.radius = shapeSize.x + margin;
							capsuleCollider.height = shapeSize.y + shapeSize.x * 2.0f + margin * 2.0f;
						}
						break;
					}
				}
			}
		}
	}

	private void _RemoveColliders()
	{
		MMD4MecanimModel model = this.target as MMD4MecanimModel;
		if( model.modelData == null ) {
			return;
		}
		
		MMD4MecanimData.RigidBodyData[] rigidBodyDataList = model.modelData.rigidBodyDataList;
		if( rigidBodyDataList == null ) {
			return;
		}
		
		for( int n = 0; n != rigidBodyDataList.Length; ++n ) {
			MMD4MecanimData.RigidBodyData rigidBodyData = rigidBodyDataList[n];
			if( rigidBodyData != null && rigidBodyData.boneID >= 0 ) {
				if( model.boneList != null && rigidBodyData.boneID < model.boneList.Length ) {
					MMD4MecanimBone bone = model.boneList[rigidBodyData.boneID];

					GameObject parentGameObject = bone.gameObject;
					if( parentGameObject == null ) {
						continue;
					}

					string colliderName = "Coll." + n + "." + parentGameObject.name;
					foreach( Transform trn in parentGameObject.transform ) {
						if( trn.name == colliderName ) {
							GameObject.DestroyImmediate( trn.gameObject );
							break;
						}
					}
				}
			}
		}
	}

	private void _DrawDebugGUI()
	{
		//MMD4MecanimModel model = this.target as MMD4MecanimModel;
		
		//EditorGUILayout.Separator();
		
		//GUILayout.Label( "Debug", EditorStyles.boldLabel );
	}
}
