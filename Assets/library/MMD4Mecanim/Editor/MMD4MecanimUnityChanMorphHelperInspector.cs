using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

[CustomEditor(typeof(MMD4MecanimUnityChanMorphHelper))]
public class MMD4MecanimUnityChanMorphHelperInspector : Editor
{
	public static bool _overrideEditorStyle = true;
	
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
				EditorStyles.textField.wordWrap = false;
			} catch ( System.Exception ) {
			}
		}
	}
	
	public override void OnInspectorGUI()
	{
		MMD4MecanimUnityChanMorphHelper animMorphHelper = this.target as MMD4MecanimUnityChanMorphHelper;

		if( animMorphHelper.GetComponent< UnityChanMorph >() == null ) {
			_overrideEditorStyle = true;
			EditorStyles.textField.wordWrap = true;
			EditorGUILayout.TextArea("\u30e6\u30cb\u30c6\u30a3\u3061\u3083\u3093\u30e2\u30c7\u30eb\u3092MMD\u306e\u30e2\u30fc\u30b7\u30e7\u30f3\u3068\u4e00\u7dd2\u306b\u4f7f\u3046\u969b\u306b\u306f\u3001\u5fc5\u305aUCL\u3092\u78ba\u8a8d\u306e\u4e0a\u3001\u5404\u30e2\u30fc\u30b7\u30e7\u30f3\u306e\u8457\u4f5c\u6a29\u8005\u306b\u4f7f\u7528\u8a31\u8afe\u3092\u53d6\u308b\u3088\u3046\u306b\u304a\u9858\u3044\u81f4\u3057\u307e\u3059\u3002 http://unity-chan.com/download/guideline.html");
		}

		EditorGUI.BeginChangeCheck();

		EditorGUILayout.Separator();

		animMorphHelper.playManually = EditorGUILayout.Toggle( "Play Manually", animMorphHelper.playManually );
		animMorphHelper.manualAnimatorStateName = EditorGUILayout.TextField( "Manual Animator State Name", animMorphHelper.manualAnimatorStateName );
		animMorphHelper.playingAnimatorStateName = EditorGUILayout.TextField( "Playing Animator State Name", animMorphHelper.playingAnimatorStateName );

		EditorGUILayout.Separator();

		animMorphHelper.animEnabled = EditorGUILayout.Toggle( "Enabled", animMorphHelper.animEnabled );
		animMorphHelper.animPauseOnEnd = EditorGUILayout.Toggle( "Pause On End", animMorphHelper.animPauseOnEnd );
		animMorphHelper.animSyncToAudio = EditorGUILayout.Toggle( "Sync To Audio", animMorphHelper.animSyncToAudio );

		EditorGUILayout.Separator();

		animMorphHelper.initializeOnAwake = EditorGUILayout.Toggle( "Initialize On Awake", animMorphHelper.initializeOnAwake );
		animMorphHelper.morphSpeed = EditorGUILayout.FloatField( "Morph Speed (Start/End)", animMorphHelper.morphSpeed );
		//animMorphHelper.overrideWeight = EditorGUILayout.Toggle( "Override Weight", animMorphHelper.overrideWeight );

		EditorGUILayout.Separator();

		if( animMorphHelper.animList != null ) {
			GUILayout.Label( "Animations", EditorStyles.boldLabel );

			for( int animIndex = 0; animIndex < animMorphHelper.animList.Length; ) {
				MMD4MecanimUnityChanMorphHelper.Anim anim = animMorphHelper.animList[animIndex];

				EditorGUILayout.BeginHorizontal();
				bool isRemove = GUILayout.Button("-", EditorStyles.miniButton, GUILayout.ExpandWidth(false) );
				anim.animatorStateName = EditorGUILayout.TextField( "Anim State Name", anim.animatorStateName );
				EditorGUILayout.EndHorizontal();

				TextAsset animFile = anim.animFile;
				EditorGUILayout.BeginHorizontal();
				GUILayout.Space(26.0f);
				animFile = (TextAsset)EditorGUILayout.ObjectField( "Anim File", (Object)animFile, typeof(TextAsset), false );
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
							anim.animatorStateName = "Base Layer." + System.IO.Path.GetFileNameWithoutExtension( animFile.name ) + ".vmd";
						}
					}
				} else {
					isRemove = true;
					anim.animFile = null;
				}
				if( isRemove ) {
					for( int i = animIndex; i + 1 < animMorphHelper.animList.Length; ++i ) {
						animMorphHelper.animList[i] = animMorphHelper.animList[i + 1];
					}
					System.Array.Resize( ref animMorphHelper.animList, animMorphHelper.animList.Length - 1 );
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
					MMD4MecanimUnityChanMorphHelper.Anim anim = new MMD4MecanimUnityChanMorphHelper.Anim();
					anim.animFile = animFile;
					anim.animatorStateName = "Base Layer." + System.IO.Path.GetFileNameWithoutExtension( animFile.name ) + ".vmd";
					if( animMorphHelper.animList == null ) {
						animMorphHelper.animList = new MMD4MecanimUnityChanMorphHelper.Anim[1];
						animMorphHelper.animList[0] = anim;
					} else {
						int animIndex = animMorphHelper.animList.Length;
						System.Array.Resize( ref animMorphHelper.animList, animIndex + 1 );
						animMorphHelper.animList[animIndex] = anim;
					}
				}
			}
		}

		if( EditorGUI.EndChangeCheck() ) {
			EditorUtility.SetDirty( target );
		}
	}
}
