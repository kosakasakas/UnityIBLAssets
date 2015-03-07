using UnityEngine;
using UnityEditor;

public class MMD4MecanimAssetPostprocessor : AssetPostprocessor
{
	static void OnPostprocessAllAssets(
		string[] importedAssets,
		string[] deletedAssets,
		string[] movedAssets,
		string[] movedFromPaths )
	{
		if( importedAssets != null ) {
			foreach( string importedAsset in importedAssets ) {
				if( MMD4MecanimEditorCommon.IsExtensionFBX( importedAsset ) ) {
					MMD4MecanimImporterEditor._OnRegistImportedFBXAsset( importedAsset );
				} else if( MMD4MecanimEditorCommon.IsExtensionPMDorPMX( importedAsset ) ) {
					MMD4MecanimImporterEditor._OnRegistImportedMMDAsset( importedAsset );
				}
			}
		}
		if( deletedAssets != null ) {
			foreach( string deletedAsset in deletedAssets ) {
				if( MMD4MecanimEditorCommon.IsExtensionFBX( deletedAsset ) ) {
					MMD4MecanimImporterEditor._OnDeletedFBXAsset( deletedAsset );
				} else if( MMD4MecanimEditorCommon.IsExtensionPMDorPMX( deletedAsset ) ) {
					MMD4MecanimImporterEditor._OnDeletedMMDAsset( deletedAsset );
				}
			}
		}
		if( movedAssets != null ) {
			for( int i = 0; i < movedAssets.Length; ++i ) {
				string movedAsset = movedAssets[i];
				string movedFromPath = movedFromPaths[i];
				if( MMD4MecanimEditorCommon.IsExtensionFBX( movedAsset ) ) {
					MMD4MecanimImporterEditor._OnMovedFBXAsset( movedAsset, movedFromPath );
				} else if( MMD4MecanimEditorCommon.IsExtensionPMDorPMX( movedAsset ) ) {
					MMD4MecanimImporterEditor._OnMovedMMDAsset( movedAsset, movedFromPath );
				}
			}
		}
	}
}
