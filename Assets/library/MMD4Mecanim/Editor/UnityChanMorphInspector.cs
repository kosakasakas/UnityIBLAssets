using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(UnityChanMorph))]
public class UnityChanMorphInspector : Editor
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
		_overrideEditorStyle = true;
		EditorStyles.textField.wordWrap = true;
		EditorGUILayout.TextArea("\u30e6\u30cb\u30c6\u30a3\u3061\u3083\u3093\u30e2\u30c7\u30eb\u3092MMD\u306e\u30e2\u30fc\u30b7\u30e7\u30f3\u3068\u4e00\u7dd2\u306b\u4f7f\u3046\u969b\u306b\u306f\u3001\u5fc5\u305aUCL\u3092\u78ba\u8a8d\u306e\u4e0a\u3001\u5404\u30e2\u30fc\u30b7\u30e7\u30f3\u306e\u8457\u4f5c\u6a29\u8005\u306b\u4f7f\u7528\u8a31\u8afe\u3092\u53d6\u308b\u3088\u3046\u306b\u304a\u9858\u3044\u81f4\u3057\u307e\u3059\u3002 http://unity-chan.com/download/guideline.html");

		DrawDefaultInspector();

		UnityChanMorph unityChanMorph = target as UnityChanMorph;
		unityChanMorph.InitializeSkinnedMeshes();
	}
}
