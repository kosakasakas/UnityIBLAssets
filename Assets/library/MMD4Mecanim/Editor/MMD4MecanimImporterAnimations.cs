using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MMD4MecanimProperty = MMD4MecanimImporter.PMX2FBXConfig.MMD4MecanimProperty;

public partial class MMD4MecanimImporter : ScriptableObject
{
	private void _OnInspectorGUI_Animations()
	{
		if( this.pmx2fbxConfig == null || this.pmx2fbxConfig.mmd4MecanimProperty == null ) {
			return;
		}

		GUILayout.Label( "FBX", EditorStyles.boldLabel );
		GUILayout.BeginHorizontal();
		GUILayout.Space( 26.0f );
		_OnInspectorGUI_ShowFBXField();
		GUILayout.EndHorizontal();
		
		EditorGUILayout.Separator();

		EditorGUILayout.LabelField( "Split Animations", EditorStyles.boldLabel );

		AnimationClip[] animationClips = GetAnimationClips();

		if( animationClips != null ) {
			foreach( AnimationClip animationClip in animationClips ) {
				EditorGUILayout.TextField( animationClip.name );
			}
		} else {
			EditorGUILayout.TextField( "No Animation." );
		}

		GUI.enabled = animationClips != null;
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if( GUILayout.Button("Process") ) {
			SplitAnimations( animationClips );
		}
		GUILayout.EndHorizontal();
		GUI.enabled = true;

#if false
		var mmd4MecanimProperty = this.pmx2fbxConfig.mmd4MecanimProperty;
		GUILayout.Label( "Root Transform Rotation", EditorStyles.boldLabel );
		mmd4MecanimProperty.rootTransformRotationBakeIntoPose = EditorGUILayout.Toggle( "Bake Into Pose", mmd4MecanimProperty.rootTransformRotationBakeIntoPose );
		mmd4MecanimProperty.rootTransformRotationBasedUpon = (PMX2FBXConfig.RotationBasedUpon)EditorGUILayout.EnumPopup( "Based Upon", (System.Enum)mmd4MecanimProperty.rootTransformRotationBasedUpon );

		GUILayout.Label( "Root Transform Position Y", EditorStyles.boldLabel );
		mmd4MecanimProperty.rootTransformPositionYBakeIntoPose = EditorGUILayout.Toggle( "Bake Into Pose", mmd4MecanimProperty.rootTransformPositionYBakeIntoPose );
		mmd4MecanimProperty.rootTransformPositionYBasedUpon = (PMX2FBXConfig.PositionBasedUpon)EditorGUILayout.EnumPopup( "Based Upon", (System.Enum)mmd4MecanimProperty.rootTransformPositionYBasedUpon );

		GUILayout.Label( "Root Transform Position XZ", EditorStyles.boldLabel );
		mmd4MecanimProperty.rootTransformPositionXZBakeIntoPose = EditorGUILayout.Toggle( "Bake Into Pose", mmd4MecanimProperty.rootTransformPositionXZBakeIntoPose );
		mmd4MecanimProperty.rootTransformPositionXZBasedUpon = (PMX2FBXConfig.PositionBasedUpon)EditorGUILayout.EnumPopup( "Based Upon", (System.Enum)mmd4MecanimProperty.rootTransformPositionXZBasedUpon );

		EditorGUILayout.Separator();

		mmd4MecanimProperty.keepAdditionalBones = EditorGUILayout.Toggle( "Keep Additional Bones", mmd4MecanimProperty.keepAdditionalBones );
		mmd4MecanimProperty.autoProcessAnimationsOnImported = EditorGUILayout.Toggle( "Auto Process on Imported", mmd4MecanimProperty.autoProcessAnimationsOnImported );

		EditorGUILayout.Separator();
		
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if( GUILayout.Button("Process") ) {
			ProcessAnimations();
		}
		GUILayout.EndHorizontal();
#endif
	}

	public AnimationClip[] GetAnimationClips()
	{
		if( !Setup() ) {
			Debug.LogError("");
			return null;
		}

		if( this.fbxAsset == null || string.IsNullOrEmpty( this.fbxAssetPath ) ) {
			//Debug.LogError("");
			return null;
		}

		Object[] assets = AssetDatabase.LoadAllAssetsAtPath( this.fbxAssetPath );
		if( assets == null || assets.Length == 0 ) {
			return null;
		}

		int animationClipLength = 0;
		foreach( Object asset in assets ) {
			AnimationClip animationClip = asset as AnimationClip;
			if( animationClip != null ) {
				if( animationClip.name != "Null" && !animationClip.name.StartsWith("__preview__") ) {
					++animationClipLength;
				}
			}
		}
		if( animationClipLength == 0 ) {
			return null;
		}

		AnimationClip[] animationClips = new AnimationClip[animationClipLength];
		int i = 0;
		foreach( Object asset in assets ) {
			AnimationClip animationClip = asset as AnimationClip;
			if( animationClip != null ) {
				if( animationClip.name != "Null" && !animationClip.name.StartsWith("__preview__") ) {
					animationClips[i] = animationClip;
					++i;
				}
			}
		}

		return animationClips;
	}

	public void SplitAnimations( AnimationClip[] animationClips )
	{
		if( !Setup() ) {
			Debug.LogError("");
			return;
		}

		if( this.fbxAsset == null || string.IsNullOrEmpty( this.fbxAssetPath ) ) {
			Debug.LogError("");
			return;
		}

		string outputDirectoryName = Path.GetDirectoryName( this.fbxAssetPath );
		string outputAnimationsDirectoryName = Path.Combine( outputDirectoryName, "Animations"  );
		if( !Directory.Exists( outputAnimationsDirectoryName ) ) {
			AssetDatabase.CreateFolder( outputDirectoryName, "Animations" );
		}

		foreach( AnimationClip animationClip in animationClips ) {
			string assetPath = Path.Combine( outputAnimationsDirectoryName, animationClip.name + ".anim" );
			AnimationClip newClip = (AnimationClip)AnimationClip.Instantiate( animationClip );
			AssetDatabase.CreateAsset( newClip, assetPath );
		}
	}

#if false
	public void ProcessAnimations()
	{
		if( this.pmx2fbxProperty == null || this.pmx2fbxConfig == null ) {
			if( !Setup() ) {
				Debug.LogError("");
				return;
			}
		}

		if( this.fbxAsset == null || string.IsNullOrEmpty( this.fbxAssetPath ) ) {
			Debug.LogError("");
			return;
		}

		if( this.pmx2fbxConfig == null || this.pmx2fbxConfig.mmd4MecanimProperty == null ) {
			Debug.LogError("");
			return;
		}

		ModelImporter modelImporter = ModelImporter.GetAtPath( this.fbxAssetPath ) as ModelImporter;
		if( modelImporter == null ) {
			Debug.LogWarning( "ModelImporter is null." );
			return;
		}

		var mmd4MecanimProperty = this.pmx2fbxConfig.mmd4MecanimProperty;

		bool keepOriginalOrientation = (mmd4MecanimProperty.rootTransformRotationBasedUpon == PMX2FBXConfig.RotationBasedUpon.Original);
		bool keepOriginalPositionY = (mmd4MecanimProperty.rootTransformPositionYBasedUpon == PMX2FBXConfig.PositionBasedUpon.Original);
		bool keepOriginalPositionXZ = (mmd4MecanimProperty.rootTransformPositionXZBasedUpon == PMX2FBXConfig.PositionBasedUpon.Original);

		bool modifiedProperties = false;

		//DumpModelImporter( modelImporter );

		var clipAnimations = modelImporter.clipAnimations;
		if( clipAnimations != null ) {
			foreach( var clipAnimation in clipAnimations ) {
				if( clipAnimation.name != "Null" ) {
					if( clipAnimation.lockRootRotation != mmd4MecanimProperty.rootTransformRotationBakeIntoPose ) {
						clipAnimation.lockRootRotation = mmd4MecanimProperty.rootTransformRotationBakeIntoPose;
						modifiedProperties = true;
					}
					if( clipAnimation.lockRootRotation ) {
						if( clipAnimation.keepOriginalOrientation != keepOriginalOrientation ) {
							clipAnimation.keepOriginalOrientation = keepOriginalOrientation;
							modifiedProperties = true;
						}
					}
					if( clipAnimation.lockRootHeightY != mmd4MecanimProperty.rootTransformPositionYBakeIntoPose ) {
						clipAnimation.lockRootHeightY = mmd4MecanimProperty.rootTransformPositionYBakeIntoPose;
						modifiedProperties = true;
					}
					if( clipAnimation.lockRootHeightY ) {
						if( clipAnimation.keepOriginalPositionY != keepOriginalPositionY ) {
							clipAnimation.keepOriginalPositionY = keepOriginalPositionY;
							modifiedProperties = true;
						}
					}
					if( clipAnimation.lockRootPositionXZ != mmd4MecanimProperty.rootTransformPositionXZBakeIntoPose ) {
						clipAnimation.lockRootPositionXZ = mmd4MecanimProperty.rootTransformPositionXZBakeIntoPose;
						modifiedProperties = true;
					}
					if( clipAnimation.lockRootPositionXZ ) {
						if( clipAnimation.keepOriginalPositionXZ != keepOriginalPositionXZ ) {
							clipAnimation.keepOriginalPositionXZ = keepOriginalPositionXZ;
							modifiedProperties = true;
						}
					}

#if UNITY_4_0 || UNITY_4_1 || UNITY_4_2
#else
					// Unity 4.3 or later
					if( clipAnimation.maskType == ClipAnimationMaskType.CreateFromThisModel ) {
						if( clipAnimation.maskSource != null ) {
							int transformCount = clipAnimation.maskSource.transformCount;
							bool[] transformMasks = new bool[transformCount];
							for( int i = 0; i < transformCount; ++i ) {
								transformMasks[i] = clipAnimation.maskSource.GetTransformActive( i );
							}
							if( mmd4MecanimProperty.keepAdditionalBones ) {
								for( int i = 0; i < transformCount; ++i ) {
									clipAnimation.maskSource.SetTransformActive( i, true );
								}
							} else {
								clipAnimation.maskSource.Reset();
							}
							for( int i = 0; i < transformCount; ++i ) {
								modifiedProperties |= (transformMasks[i] != clipAnimation.maskSource.GetTransformActive( i ));
							}
						} else {
							Debug.LogWarning( "clipAnimation.maskSource is null." );
						}
					}
#endif
				}
			}
			if( modifiedProperties ) {
				modelImporter.clipAnimations = clipAnimations;
			}
		}

		if( modifiedProperties ) {
			MMD4MecanimEditorCommon.UpdateImportSettings( this.fbxAssetPath );
		}

		Debug.Log( "Processed animations." );
	}
#endif
}

