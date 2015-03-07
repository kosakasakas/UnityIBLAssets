using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using MMD4MecanimProperty = MMD4MecanimImporter.PMX2FBXConfig.MMD4MecanimProperty;

public partial class MMD4MecanimImporter : ScriptableObject
{
	private void _OnInspectorGUI_Rig()
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
		
		GUILayout.Label( "Reset Humanoid Mapping", EditorStyles.boldLabel );
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if( GUILayout.Button("Process") ) {
			_ProcessAvatarHumanoid();
		}
		GUILayout.EndHorizontal();
	}

	public class FBXMetaData
	{
		public HumanDescription		humanDescription = new HumanDescription();
		public List<HumanBone>		humanBones = new List<HumanBone>();
		public List<SkeletonBone>	skeletonBones = new List<SkeletonBone>();
		public int					humanDescriptionTab = 0;
		public string				headerString = "";
		public string				middleString = "";
		public string				footerString = "";
	}

	const string HumanDescriptionLine	= "humanDescription:";
	const string HumanLine				= "human:";
	const string Human0Line				= "human: []";
	const string SkeletonLine			= "skeleton:";
	const string Skeleton0Line			= "skeleton: []";

	public static bool ParseLine( string lineString, string name, out string valueString )
	{
		valueString = null;
		if( lineString.StartsWith(name + ": ") ) {
			valueString = lineString.Substring( name.Length + 2 );
			return true;
		} else if( lineString.StartsWith(name + ":") ) {
			return true;
		}
		return false;
	}

	public static string ParseText( string valueString )
	{
		return valueString;
	}

	public static int ParseInt( string valueString )
	{
		try {
			return int.Parse( valueString );
		} catch(Exception) {
		}
		return 0;
	}

	public static float ParseFloat( string valueString )
	{
		try {
			return float.Parse( valueString );
		} catch(Exception) {
		}
		return 0.0f;
	}

	public struct Range
	{
		public int startIndex;
		public int count;
		public int endIndex { get { return startIndex + count; } }

		public Range(int s, int c)
		{
			startIndex = s;
			count = c;
		}
	}

	public static Range GetRange( string text, string str )
	{
		int p = text.IndexOf(str);
		if( p >= 0 ) {
			return new Range(p, str.Length);
		}
		return new Range(-1,0);
	}

	public static Range GetRange( string text, string str, int pos )
	{
		if( pos < 0 ) {
			return new Range(-1,0);
		}
		int p = text.IndexOf(str, pos);
		if( p >= 0 ) {
			return new Range(p, str.Length);
		}
		return new Range(-1,0);
	}

	public static string GetMiddleString( string str, Range r0, Range r1 )
	{
		if( r0.startIndex >= 0 && r1.startIndex >= 0 ) {
			return str.Substring( r0.endIndex, r1.startIndex - r0.endIndex );
		}
		return null;
	}
	
	public static Vector3 ParseVector3( string valueString )
	{
		if( valueString == "{x: 0, y: 0, z: 0}" ) {
			return Vector3.zero;
		} else if( valueString == "{x: 1, y: 1, z: 1}" ) {
			return Vector3.one;
		}

		Range p0 = GetRange(valueString, "{x: ");
		Range p1 = GetRange(valueString, ", y: ", p0.endIndex);
		Range p2 = GetRange(valueString, ", z: ", p1.endIndex);
		Range p3 = GetRange(valueString, "}", p2.endIndex);
		if( p0.startIndex >= 0 && p1.startIndex >= 0 && p2.startIndex >= 0 && p3.startIndex >= 0 ) {
			float x = ParseFloat( GetMiddleString( valueString, p0, p1 ) );
			float y = ParseFloat( GetMiddleString( valueString, p1, p2 ) );
			float z = ParseFloat( GetMiddleString( valueString, p2, p3 ) );
			return new Vector3( x, y, z );
		} else {
			Debug.LogError("ParseVector3: Error." + valueString);
			return Vector3.zero;
		}
	}

	public static Quaternion ParseQuaternion( string valueString )
	{
		if( valueString == "{x: 0, y: 0, z: 0, w: 1}" ) {
			return Quaternion.identity;
		}

		Range p0 = GetRange(valueString, "{x: ");
		Range p1 = GetRange(valueString, ", y: ", p0.endIndex);
		Range p2 = GetRange(valueString, ", z: ", p1.endIndex);
		Range p3 = GetRange(valueString, ", w: ", p2.endIndex);
		Range p4 = GetRange(valueString, "}", p3.endIndex);
		if( p0.startIndex >= 0 && p1.startIndex >= 0 && p2.startIndex >= 0 && p3.startIndex >= 0 && p4.startIndex >= 0 ) {
			float x = ParseFloat( GetMiddleString( valueString, p0, p1 ) );
			float y = ParseFloat( GetMiddleString( valueString, p1, p2 ) );
			float z = ParseFloat( GetMiddleString( valueString, p2, p3 ) );
			float w = ParseFloat( GetMiddleString( valueString, p3, p4 ) );
			return new Quaternion( x, y, z, w );
		} else {
			Debug.LogError("ParseQuaternion: Error." + valueString);
			return Quaternion.identity;
		}
	}

	public static FBXMetaData Deserialize(string text)
	{
		FBXMetaData metaData = new FBXMetaData();

		bool foundHumanDescription = false;
		bool foundHuman = false;
		bool foundSkeleton = false;
		int headerEndPos = 0;
		int middleStartPos = -1;
		int middleEndPos = -1;
		int pos = 0;

		List<HumanBone> humanBones = new List<HumanBone>();
		List<SkeletonBone> skeletonBones = new List<SkeletonBone>();
		bool processHuman = false;
		bool processHumanLimit = false;
		bool processSkeleton = false;

		int lineNum = 1;
		for(;pos < text.Length; ++lineNum) {
			int startPos = pos;
			int endPos = text.IndexOf( '\n', pos );
			int nextLinePos = endPos + 1;
			if( endPos < 0 ) {
				endPos = text.Length;
				nextLinePos = text.Length;
			}

			int tab = 0;
			for(; text[pos] == ' ' && pos < endPos; ++pos, ++tab);

			string lineString = text.Substring( pos, endPos - pos );

			if( !foundHumanDescription ) {
				if( lineString == HumanDescriptionLine ) {
					foundHumanDescription = true;
					headerEndPos = nextLinePos;
					metaData.humanDescriptionTab = tab;
				}
			} else {
				if( !foundHuman && !foundSkeleton ) {
					headerEndPos = startPos;
				}
				int parentTab = metaData.humanDescriptionTab + 2;
				if( tab > parentTab || (tab == parentTab && lineString.StartsWith("- ")) ) {
					if( lineString.StartsWith( "- " ) ) {
						lineString = lineString.Substring( 2 );
						tab += 2;
						if( processHuman ) {
							humanBones.Add( new HumanBone() );
						} else if( processSkeleton ) {
							skeletonBones.Add( new SkeletonBone() );
						}
					}
					int childTab = parentTab + 2;
					string value = null;
					if( processHuman ) {
						HumanBone humanBone = humanBones[humanBones.Count - 1];
						if( tab == childTab && ParseLine( lineString, "boneName", out value ) ) {
							processHumanLimit = false;
							humanBone.boneName = ParseText( value );
						} else if( tab == childTab && ParseLine( lineString, "humanName", out value ) ) {
							processHumanLimit = false;
							humanBone.humanName = ParseText( value );
						} else if( tab == childTab && ParseLine( lineString, "limit", out value ) ) {
							processHumanLimit = true;
							humanBone.limit = new HumanLimit();
						} else if( processHumanLimit && tab == childTab + 2 && ParseLine( lineString, "min", out value ) ) {
							humanBone.limit.min = ParseVector3( value );
						} else if( processHumanLimit && tab == childTab + 2 && ParseLine( lineString, "max", out value ) ) {
							humanBone.limit.max = ParseVector3( value );
						} else if( processHumanLimit && tab == childTab + 2 && ParseLine( lineString, "value", out value ) ) {
							humanBone.limit.center = ParseVector3( value );
						} else if( processHumanLimit && tab == childTab + 2 && ParseLine( lineString, "length", out value ) ) {
							humanBone.limit.axisLength = ParseFloat( value );
						} else if( processHumanLimit && tab == childTab + 2 && ParseLine( lineString, "modified", out value ) ) {
							humanBone.limit.useDefaultValues = (ParseInt( value ) == 0);
						} else {
							if( tab == childTab ) {
								processHumanLimit = false;
							}
							// Unknown elements.
						}
						humanBones[humanBones.Count - 1] = humanBone;
					} else if( processSkeleton ) {
						SkeletonBone skeletonBone = skeletonBones[skeletonBones.Count - 1];
						if( tab == childTab && ParseLine( lineString, "name", out value ) ) {
							skeletonBone.name = ParseText( value );
						} else if( tab == childTab && ParseLine( lineString, "position", out value ) ) {
							skeletonBone.position = ParseVector3( value );
						} else if( tab == childTab && ParseLine( lineString, "rotation", out value ) ) {
							skeletonBone.rotation = ParseQuaternion( value );
						} else if( tab == childTab && ParseLine( lineString, "scale", out value ) ) {
							skeletonBone.scale = ParseVector3( value );
						} else if( tab == childTab && ParseLine( lineString, "transformModified", out value ) ) {
							skeletonBone.transformModified = ParseInt( value );
						} else {
							// Unknown elements.
						}
						skeletonBones[skeletonBones.Count - 1] = skeletonBone;
					}
				} else if( tab == parentTab ) {
					processHuman = false;
					processHumanLimit = false;
					processSkeleton = false;
					if( lineString == HumanLine || lineString == Human0Line ) {
						if( foundHuman ) {
							Debug.LogError("Already found human. Line:" + lineNum);
							return null;
						}
						if( middleStartPos != -1 && middleEndPos == -1 ) {
							middleEndPos = startPos;
						}
						foundHuman = true;
						if( lineString == HumanLine ) {
							processHuman = true;
						}
					} else if( lineString == SkeletonLine || lineString == Skeleton0Line ) {
						if( foundSkeleton ) {
							Debug.LogError("Already found skeleton. Line:" + lineNum);
							return null;
						}
						if( middleStartPos != -1 && middleEndPos == -1 ) {
							middleEndPos = startPos;
						}
						foundSkeleton = true;
						if( lineString == SkeletonLine ) {
							processSkeleton = true;
						}
					} else {
						if( foundHuman != foundSkeleton ) {
							if( middleStartPos == -1 ) {
								middleStartPos = startPos;
								middleEndPos = nextLinePos;
							}
						} else if( foundHuman && foundSkeleton ) {
							pos = startPos;
							break;
						}
					}
				} else {
					pos = startPos;
					break;
				}
			}
			pos = nextLinePos;
		}

		if( !foundHumanDescription || !foundHuman || !foundSkeleton ) {
			if( !foundHumanDescription ) {
				Debug.LogError( "Not found humanDescription line." );
			}
			if( !foundHuman ) {
				Debug.LogError( "Not found human line." );
			}
			if( !foundSkeleton ) {
				Debug.LogError( "Not found skeleton line." );
			}
			return null;
		}

		metaData.humanDescription.human = humanBones.ToArray();
		metaData.humanDescription.skeleton = skeletonBones.ToArray();
		metaData.humanBones = humanBones;
		metaData.skeletonBones = skeletonBones;
		metaData.headerString = text.Substring( 0, headerEndPos );
		if( pos < text.Length ) {
			metaData.footerString = text.Substring( pos );
		}
		if( middleEndPos != -1 ) {
			metaData.middleString = text.Substring( middleStartPos, middleEndPos - middleStartPos );
		}
		return metaData;
	}

	public static string ToString( int v )
	{
		return v.ToString();
	}

	public static string ToString( float v )
	{
		string str = v.ToString("g10");
		if( str.StartsWith("0.") ) {
			return str.Substring( 1 );
		} else if( str.StartsWith("-0.") ) {
			return "-" + str.Substring( 2 );
		} else {
			return str;
		}
	}

	public static string ToString( Vector3 v )
	{
		if( v == Vector3.zero ) {
			return "{x: 0, y: 0, z: 0}";
		} else if( v == Vector3.one ) {
			return "{x: 1, y: 1, z: 1}";
		}

		System.Text.StringBuilder str = new System.Text.StringBuilder();
		str.Append("{x: ");
		str.Append( ToString( v.x ) );
		str.Append(", y: ");
		str.Append( ToString( v.y ) );
		str.Append(", z: ");
		str.Append( ToString( v.z ) );
		str.Append("}");
		return str.ToString();
	}

	public static string ToString( Quaternion v )
	{
		if( v == Quaternion.identity ) {
			return "{x: 0, y: 0, z: 0, w: 1}";
		}

		System.Text.StringBuilder str = new System.Text.StringBuilder();
		str.Append("{x: ");
		str.Append( ToString( v.x ) );
		str.Append(", y: ");
		str.Append( ToString( v.y ) );
		str.Append(", z: ");
		str.Append( ToString( v.z ) );
		str.Append(", w: ");
		str.Append( ToString( v.w ) );
		str.Append("}");
		return str.ToString();
	}

	public static void SerializeLine( System.Text.StringBuilder str, int tab, string name, string value )
	{
		str.Append( ' ', tab );
		str.Append( name );
		if( value != null ) {
			str.Append( ": " );
			str.Append( value );
			str.Append( '\n' );
		} else {
			str.Append( ":" );
			str.Append( '\n' );
		}
	}

	public static string Serialize(FBXMetaData metaData)
	{
		if( metaData == null ||
		    metaData.humanDescription.human == null ||
		    metaData.humanDescription.skeleton == null ) {
			Debug.LogError("");
			return null;
		}

		System.Text.StringBuilder str = new System.Text.StringBuilder();

		int parentTab = metaData.humanDescriptionTab + 2;
		int childTab = parentTab + 2;

		if( !string.IsNullOrEmpty(metaData.headerString) ) {
			str.Append(metaData.headerString);
		}

		if( metaData.humanDescription.human.Length > 0 ) {
			str.Append( ' ', parentTab );
			str.Append( HumanLine );
			str.Append( '\n' );
			for( int index = 0; index < metaData.humanDescription.human.Length; ++index ) {
				HumanBone humanBone = metaData.humanDescription.human[index];
				SerializeLine( str, parentTab, "- boneName", humanBone.boneName );
				SerializeLine( str, childTab, "humanName", humanBone.humanName );
				SerializeLine( str, childTab, "limit", null );
				SerializeLine( str, childTab + 2, "min", ToString( humanBone.limit.min ) );
				SerializeLine( str, childTab + 2, "max", ToString( humanBone.limit.max ) );
				SerializeLine( str, childTab + 2, "value", ToString( humanBone.limit.center ) );
				SerializeLine( str, childTab + 2, "length", ToString( humanBone.limit.axisLength ) );
				SerializeLine( str, childTab + 2, "modified", ToString( humanBone.limit.useDefaultValues ? 0 : 1 ) );
			}
		} else {
			str.Append( ' ', parentTab );
			str.Append( Human0Line );
			str.Append( '\n' );
		}

		if( !string.IsNullOrEmpty(metaData.middleString) ) {
			str.Append(metaData.middleString);
		}

		if( metaData.humanDescription.skeleton.Length > 0 ) {
			str.Append( ' ', parentTab );
			str.Append( SkeletonLine );
			str.Append( '\n' );
			for( int index = 0; index < metaData.humanDescription.skeleton.Length; ++index ) {
				SkeletonBone skeletonBone = metaData.humanDescription.skeleton[index];
				SerializeLine( str, parentTab, "- name", skeletonBone.name );
				SerializeLine( str, childTab, "position", ToString( skeletonBone.position ) );
				SerializeLine( str, childTab, "rotation", ToString( skeletonBone.rotation ) );
				SerializeLine( str, childTab, "scale", ToString( skeletonBone.scale ) );
				SerializeLine( str, childTab, "transformModified", ToString( skeletonBone.transformModified ) );
			}
		} else {
			str.Append( ' ', parentTab );
			str.Append( Skeleton0Line );
			str.Append( '\n' );
		}

		if( !string.IsNullOrEmpty(metaData.footerString) ) {
			str.Append(metaData.footerString);
		}

		return str.ToString();
	}

	void _ProcessAvatarHumanoid()
	{
		if( !Setup() ) {
			return;
		}

		if( this.fbxAsset == null || this.fbxAssetPath == null || this.mmdModel == null ) {
			return;
		}

		string metaDataPath = AssetDatabase.GetTextMetaFilePathFromAssetPath( this.fbxAssetPath );
		if( string.IsNullOrEmpty( metaDataPath ) ) {
			Debug.LogError(".meta data is not found. " + this.fbxAssetPath);
			return;
		}

		string text = File.ReadAllText( metaDataPath );
		if( string.IsNullOrEmpty(text) ) {
			Debug.LogError(".meta data is empty. " + this.fbxAssetPath);
			return;
		}

		FBXMetaData metaData = Deserialize( text );
		if( metaData == null ) {
			Debug.LogError(".meta Deserialize failed. " + this.fbxAssetPath);
			return;
		}

		GameObject root = null;
		foreach( Transform childTransform in this.fbxAsset.transform ) {
			if( childTransform.name.Contains("Root") ) {
				root = childTransform.gameObject;
				break;
			}
		}
		
		if( root == null ) {
			Debug.LogError("");
			return;
		}

		Animator animator = this.fbxAsset.GetComponent< Animator >();
		if( animator == null ) {
			Debug.LogError("");
			return;
		}

		List<HumanBone> humanBones = metaData.humanBones;
		List<SkeletonBone> skeletonBones = metaData.skeletonBones;

		_RemoveHuman( humanBones, "Jaw" );

		if( mmdModel.boneList != null ) {
			for( int i = 0; i < mmdModel.boneList.Length; ++i ) {
				MMDModel.Bone bone = mmdModel.boneList[i];
				if( !string.IsNullOrEmpty( bone.humanType ) ) {
					_AddHuman( humanBones, root, bone.skeletonName, bone.humanType );
				}
			}
		}

		/*
		_AddHuman( humanBones, root, "13.joint_HipMaster", "Hips" );
		_AddHuman( humanBones, root, "49.joint_LeftHip", "LeftUpperLeg" );
		_AddHuman( humanBones, root, "86.joint_RightHip", "RightUpperLeg" );
		_AddHuman( humanBones, root, "50.joint_LeftKnee", "LeftLowerLeg" );
		_AddHuman( humanBones, root, "87.joint_RightKnee", "RightLowerLeg" );
		_AddHuman( humanBones, root, "51.joint_LeftFoot", "LeftFoot" );
		_AddHuman( humanBones, root, "88.joint_RightFoot", "RightFoot" );
		_AddHuman( humanBones, root, "4.joint_Torso", "Spine" );
		_AddHuman( humanBones, root, "5.joint_Torso2", "Chest" );
		_AddHuman( humanBones, root, "6.joint_Neck", "Neck" );
		_AddHuman( humanBones, root, "7.joint_Head", "Head" );
		_AddHuman( humanBones, root, "22.joint_LeftShoulder", "LeftShoulder" );
		_AddHuman( humanBones, root, "59.joint_RightShoulder", "RightShoulder" );
		_AddHuman( humanBones, root, "24.joint_LeftArm", "LeftUpperArm" );
		_AddHuman( humanBones, root, "61.joint_RightArm", "RightUpperArm" );
		_AddHuman( humanBones, root, "26.joint_LeftElbow", "LeftLowerArm" );
		_AddHuman( humanBones, root, "63.joint_RightElbow", "RightLowerArm" );
		_AddHuman( humanBones, root, "28.joint_LeftWrist", "LeftHand" );
		_AddHuman( humanBones, root, "65.joint_RightWrist", "RightHand" );
		_AddHuman( humanBones, root, "8.joint_LeftEye", "LeftEye" );
		_AddHuman( humanBones, root, "9.joint_RightEye", "RightEye" );
		_AddHuman( humanBones, root, "92.joint_FrontHair2", "Jaw" );
		_AddHuman( humanBones, root, "167.!joint_LeftThumb0M", "Left Thumb Proximal" );
		_AddHuman( humanBones, root, "32.joint_LeftThumb1", "Left Thumb Intermediate" );
		_AddHuman( humanBones, root, "33.joint_LeftThumb2", "Left Thumb Distal" );
		_AddHuman( humanBones, root, "34.joint_LeftIndex1", "Left Index Proximal" );
		_AddHuman( humanBones, root, "35.joint_LeftIndex2", "Left Index Intermediate" );
		_AddHuman( humanBones, root, "36.joint_LeftIndex3", "Left Index Distal" );
		_AddHuman( humanBones, root, "37.joint_LeftFingers1", "Left Middle Proximal" );
		_AddHuman( humanBones, root, "38.joint_LeftFingers2", "Left Middle Intermediate" );
		_AddHuman( humanBones, root, "39.joint_LeftFingers3", "Left Middle Distal" );
		_AddHuman( humanBones, root, "40.joint_LeftRing1", "Left Ring Proximal" );
		_AddHuman( humanBones, root, "41.joint_LeftRing2", "Left Ring Intermediate" );
		_AddHuman( humanBones, root, "42.joint_LeftRing3", "Left Ring Distal" );
		_AddHuman( humanBones, root, "44.joint_LeftPinky1", "Left Little Proximal" );
		_AddHuman( humanBones, root, "45.joint_LeftPinky2", "Left Little Intermediate" );
		_AddHuman( humanBones, root, "46.joint_LeftPinky3", "Left Little Distal" );
		_AddHuman( humanBones, root, "168.!joint_RightThumb0M", "Right Thumb Proximal" );
		_AddHuman( humanBones, root, "69.joint_RightThumb1", "Right Thumb Intermediate" );
		_AddHuman( humanBones, root, "70.joint_RightThumb2", "Right Thumb Distal" );
		_AddHuman( humanBones, root, "71.joint_RightIndex1", "Right Index Proximal" );
		_AddHuman( humanBones, root, "72.joint_RightIndex2", "Right Index Intermediate" );
		_AddHuman( humanBones, root, "73.joint_RightIndex3", "Right Index Distal" );
		_AddHuman( humanBones, root, "74.joint_RightFingers1", "Right Middle Proximal" );
		_AddHuman( humanBones, root, "75.joint_RightFingers2", "Right Middle Intermediate" );
		_AddHuman( humanBones, root, "76.joint_RightFingers3", "Right Middle Distal" );
		_AddHuman( humanBones, root, "77.joint_RightRing1", "Right Ring Proximal" );
		_AddHuman( humanBones, root, "78.joint_RightRing2", "Right Ring Intermediate" );
		_AddHuman( humanBones, root, "79.joint_RightRing3", "Right Ring Distal" );
		_AddHuman( humanBones, root, "81.joint_RightPinky1", "Right Little Proximal" );
		_AddHuman( humanBones, root, "82.joint_RightPinky2", "Right Little Intermediate" );
		_AddHuman( humanBones, root, "83.joint_RightPinky3", "Right Little Distal" );
		*/

		_AddSkeletonRoot( skeletonBones, this.fbxAsset );

		metaData.humanDescription.human = humanBones.ToArray();
		metaData.humanDescription.skeleton = skeletonBones.ToArray();

		text = Serialize( metaData );
		if( string.IsNullOrEmpty(text) ) {
			Debug.LogError(".meta Serialize failed. " + this.fbxAssetPath);
			return;
		}

		File.WriteAllText( metaDataPath, text );
		Debug.Log( ".meta file processed. " + metaDataPath );
		AssetDatabase.ImportAsset( this.fbxAssetPath,
			ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate );
	}

	public static GameObject _FindContains( GameObject go, string name )
	{
		if( go.name.Contains( name ) ) {
			return go;
		}
		
		foreach( Transform transform in go.transform ) {
			GameObject go2 = _FindContains( transform.gameObject, name );
			if( go2 != null ) {
				return go2;
			}
		}
		
		return null;
	}
	
	public static void _AddHuman( List<HumanBone> humanBones, HumanBone humanBone )
	{
		for( int i = 0; i < humanBones.Count; ++i ) {
			if( humanBones[i].humanName == humanBone.humanName ) {
				HumanBone tempHumanBone = humanBones[i];
				tempHumanBone.boneName = humanBone.boneName; // Replace bone name only.
				humanBones[i] = tempHumanBone;
				return;
			}
		}

		humanBones.Add( humanBone );
	}
	
	public static void _AddSkeleton( List<SkeletonBone> skeletonBones, SkeletonBone skeletonBone )
	{
		for( int i = 0; i < skeletonBones.Count; ++i ) {
			if( skeletonBones[i].name == skeletonBone.name ) {
				skeletonBones.RemoveAt( i );
				break;
			}
		}

		skeletonBones.Add( skeletonBone );
	}
	
	public static void _AddHuman( List<HumanBone> humanBones, GameObject root, string boneName, string humanName )
	{
		GameObject go = _FindContains( root, boneName );
		if(go != null) {
			HumanBone humanBone = new HumanBone();
			humanBone.boneName = go.name;
			humanBone.humanName = humanName;
			_AddHuman( humanBones, humanBone );
		}
	}

	public static void _RemoveHuman( List<HumanBone> humanBones, string humanName )
	{
		for( int i = 0; i < humanBones.Count; ++i ) {
			if( humanBones[i].humanName == humanName ) {
				humanBones.RemoveAt( i );
				return;
			}
		}
	}

	public static void _AddSkeleton( List<SkeletonBone> skeletonBones, GameObject go )
	{
		SkeletonBone skeletonBone = new SkeletonBone();
		skeletonBone.name = go.name;
		skeletonBone.position = go.transform.localPosition;
		skeletonBone.rotation = go.transform.localRotation;
		_AddSkeleton( skeletonBones, skeletonBone );
		foreach( Transform transform in go.transform ) {
			_AddSkeleton( skeletonBones, transform.gameObject );
		}
	}
	
	public static void _AddSkeletonRoot( List<SkeletonBone> skeletonBones, GameObject go )
	{
		foreach( Transform transform in go.transform ) {
			_AddSkeleton( skeletonBones, transform.gameObject );
		}
	}
}

