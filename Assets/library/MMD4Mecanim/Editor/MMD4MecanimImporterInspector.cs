using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

[CustomEditor(typeof(MMD4MecanimImporter))]
public class MMD4MecanimImporterInspector : Editor
{
	public void OnEnable()
	{
	}
	
	public override void OnInspectorGUI()
	{
		MMD4MecanimImporter importer = this.target as MMD4MecanimImporter;
		importer.OnInspectorGUI();
	}
	
#if false
	public override bool HasPreviewGUI()
	{
		return true;
	}
	
	private bool _togglePreview = false;
	private Vector2 _previewScrollPos;
	
	public override void OnPreviewSettings()
	{
		if( _togglePreview ) {
			if( GUILayout.Button("_", EditorStyles.miniButton) ) {
				_togglePreview = !_togglePreview;
			}
		} else {
			if( GUILayout.Button("□", EditorStyles.miniButton) ) {
				_togglePreview = !_togglePreview;
			}
		}
	}

	public override void OnInteractivePreviewGUI( Rect r, GUIStyle background )
	{
		MMD4MecanimImporter importer = this.target as MMD4MecanimImporter;

		string consoleLog = importer.consoleLog;
		string lastLineConsoleLog = importer.lastLineConsoleLog;
		if( string.IsNullOrEmpty( consoleLog ) ) {
			return;
		}
		
		if( lastLineConsoleLog == null ) {
			lastLineConsoleLog = "";
		}
		
		if( _togglePreview ) {
			EditorGUILayout.LabelField( lastLineConsoleLog, GUILayout.ExpandWidth( true ), GUILayout.ExpandHeight( false ) );

			EditorGUILayout.BeginHorizontal();
			_previewScrollPos = EditorGUILayout.BeginScrollView( _previewScrollPos, false, true );
			EditorGUILayout.TextArea( consoleLog );
			EditorGUILayout.EndScrollView();
			EditorGUILayout.LabelField( "", GUILayout.Width(0.0f), GUILayout.Height(200.0f) );
			EditorGUILayout.EndHorizontal();
		} else {
			EditorGUILayout.LabelField( lastLineConsoleLog, GUILayout.ExpandWidth( true ), GUILayout.ExpandHeight( false ) );
			EditorGUILayout.BeginHorizontal();
			_previewScrollPos = EditorGUILayout.BeginScrollView( _previewScrollPos, false, true );
			EditorGUILayout.TextArea( consoleLog, GUILayout.ExpandWidth( true ), GUILayout.ExpandHeight( true ) );
			EditorGUILayout.EndScrollView();
			EditorGUILayout.EndHorizontal();
		}
	}
#endif
}
