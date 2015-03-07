using UnityEditor;
using UnityEngine;
using System.Collections;

[CustomEditor(typeof(MMD4MecanimBone))]
public class MMD4MecanimBoneInspector : Editor
{
	Vector3 _eulerAngles;

	void _RefreshEulerAngles( MMD4MecanimBone bone )
	{
		Vector3 eulerAngles = MMD4MecanimCommon.NormalizeAsDegree( bone.userEulerAngles );
		for( int i = 0; i < 3; ++i ) {
			if( Mathf.Abs(eulerAngles[i] - MMD4MecanimCommon.NormalizeAsDegree(_eulerAngles[i])) > 0.001f ) {
				_eulerAngles[i] = Mathf.Ceil( eulerAngles[i] * 1000.0f ) / 1000.0f;
				_eulerAngles[i] = Mathf.Clamp( _eulerAngles[i], -180.0f, 180.0f );
			}
		}
	}

	public override void OnInspectorGUI()
	{
		MMD4MecanimBone bone = this.target as MMD4MecanimBone;
		_RefreshEulerAngles( bone );

		GUILayout.Label( "Information" );
		if( bone.boneData != null ) {
			EditorGUILayout.TextField( "Name", "" + bone.boneID + " : " + bone.boneData.nameJp );
			#if MMD4MECANIM_DEBUG
			if( bone.model != null ) {
				MMD4MecanimBone parentBone = bone.model.GetBone( bone.boneData.parentBoneID );
				MMD4MecanimBone originalParentBone = bone.model.GetBone( bone.boneData.originalParentBoneID );
				bool isModifiedHierarchy = (bone.boneData.parentBoneID != bone.boneData.originalParentBoneID);
				EditorGUILayout.ObjectField( "ParentBone", ( parentBone != null ) ? parentBone.gameObject : null, typeof(GameObject), false );
				EditorGUILayout.ObjectField( "OriginalParentBone", ( originalParentBone != null ) ? originalParentBone.gameObject : null, typeof(GameObject), false );
				EditorGUILayout.Toggle( "ModifiedHierarchy", isModifiedHierarchy );
			}
			#endif
		}
		EditorGUILayout.Separator();
		GUILayout.Label( "UserRotation" );
		Vector3 eulerAngles2 = Vector3.zero;
		for( int i = 0; i < 3; ++i ) {
			EditorGUILayout.BeginHorizontal();
			switch( i ) {
			case 0: GUILayout.Label("X"); break;
			case 1: GUILayout.Label("Y"); break;
			case 2: GUILayout.Label("Z"); break;
			}
			eulerAngles2[i] = EditorGUILayout.Slider( _eulerAngles[i], -180.0f, 180.0f );
			EditorGUILayout.EndHorizontal();
		}

		bone.ikEnabled = EditorGUILayout.Toggle("IKEnabled", bone.ikEnabled);
		bone.ikWeight = EditorGUILayout.Slider( "IKWeight", bone.ikWeight, 0.0f, 1.0f );

		if( Mathf.Abs(_eulerAngles.x - eulerAngles2.x) > Mathf.Epsilon ||
		    Mathf.Abs(_eulerAngles.y - eulerAngles2.y) > Mathf.Epsilon ||
		    Mathf.Abs(_eulerAngles.z - eulerAngles2.z) > Mathf.Epsilon ) {
			_eulerAngles = eulerAngles2;
			bone.userEulerAngles = eulerAngles2;
		}
	}
}
