using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

public static class MMD4MecanimData
{
	public enum FileType
	{
		None		= 0,
		PMD			= 1,
		PMX			= 2,
	}

	public enum MorphCategory
	{
		Base,
		EyeBrow,
		Eye,
		Lip,
		Other,
		Max,
	}

	public enum ShapeType
	{
		Sphere,
		Box,
		Capsule,
	}

	public enum RigidBodyType
	{
		Kinematics,
		Simulated,
		SimulatedAligned,
	}

	public enum BoneTransform
	{
		Unknown = -1,
		BDEF1,
		BDEF2,
		BDEF4,
		SDEF,
		QDEF,
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

	public enum MorphAutoLumninousType
	{
		None,
		LightUp,
		LightOff,
		LightBlink,
		LightBS,
	}

	public enum PMDBoneType
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

	[System.Flags]
	public enum BoneAdditionalFlags
	{
		None						= 0,
		IsKnee						= 0x00000001,
		//IsRigidBody					= 0x00000002, // Legacy(Not supported now.)
		//IsKinematic					= 0x00000004, // Legacy(Not supported now.)

		BoneTypeMask				= unchecked((int)0xff000000),
		BoneTypeRoot				= unchecked((int)0x80000000),
		//BoneTypeChar				= unchecked((int)0xc0000000), // Legacy(Not supported now.)
		//BoneTypeModLeftThumb0		= unchecked((int)0x08000000), // Legacy(Not supported now.)
		//BoneTypeModRightThumb0	= unchecked((int)0x88000000), // Legacy(Not supported now.)
		//BoneTypeModLeftShoulder	= unchecked((int)0x48000000), // Legacy(Not supported now.)
		//BoneTypeModRightShoulder	= unchecked((int)0xc8000000), // Legacy(Not supported now.)
	}

	[System.Flags]
	public enum RigidBodyAdditionalFlags
	{
		None						= 0,
		IsFreezed					= 0x00000001,
	}

	[System.Flags]
	public enum PMXBoneFlags
	{
		None						= 0,
		Destination					= 0x0001,
		Rotate						= 0x0002,
		Translate					= 0x0004,
		Visible						= 0x0008,
		Controllable				= 0x0010,
		IK							= 0x0020,
		IKChild						= 0x0040,
		InherenceLocal				= 0x0080,
		InherenceRotate				= 0x0100,
		InherenceTranslate			= 0x0200,
		FixedAxis					= 0x0400,
		LocalAxis					= 0x0800,
		TransformAfterPhysics		= 0x1000,
		TransformExternalParent		= 0x2000,
	}

	[System.Flags]
	public enum IKAdditionalFlags
	{
	}
	
	[System.Flags]
	public enum IKLinkFlags
	{
		None						= 0,
		HasAngleJoint				= 0x01,
	}

	[System.Flags]
	public enum PMXBoneFlag
	{
		None						= 0,
		Destination					= 0x0001,
		Rotate						= 0x0002,
		Translate					= 0x0004,
		Visible						= 0x0008,
		Controllable				= 0x0010,
		IK							= 0x0020,
		IKChild						= 0x0040,
		InherenceLocal				= 0x0080,
		InherenceRotate				= 0x0100,
		InherenceTranslate			= 0x0200,
		FixedAxis					= 0x0400,
		LocalAxis					= 0x0800,
		TransformAfterPhysics		= 0x1000,
		TransformExternalParent		= 0x2000,
	}

	[System.Flags]
	public enum MeshFlags
	{
		None						= 0,
		BlendShapes					= 0x0001,
		VertexMorph					= 0x0002,
		XDEF						= 0x0004,
	}

	[System.Serializable]
	public class BoneData
	{
		public BoneAdditionalFlags				boneAdditionalFlags;
		public string							nameJp;
		//public string							nameEn;
		public string							skeletonName;
		public int								parentBoneID;
		public int								sortedBoneID;
		public int								originalParentBoneID;
		public int								originalSortedBoneID;
		public Vector3							baseOrigin;
		public PMDBoneType						pmdBoneType;			// for PMD
		public int								childBoneID;			// for PMD
		public int								targetBoneID;			// for PMD
		public float							followCoef;				// for PMD
		public int								transformLayerID;		// for PMX
		public PMXBoneFlags						pmxBoneFlags;			// for PMX
		public int								inherenceParentBoneID;	// for PMX
		public float							inherenceWeight;		// for PMX
		public int								externalID;				// for PMX

		//public bool								isRigidBodySimulated;

		public bool isKnee						{ get { return (boneAdditionalFlags & BoneAdditionalFlags.IsKnee) != BoneAdditionalFlags.None; } }
		//public bool isRigidBody					{ get { return (boneAdditionalFlags & BoneAdditionalFlags.IsRigidBody) != BoneAdditionalFlags.None; } }
		//public bool isKinematic					{ get { return (boneAdditionalFlags & BoneAdditionalFlags.IsKinematic) != BoneAdditionalFlags.None; } }
		public bool isRootBone					{ get { return (boneAdditionalFlags & BoneAdditionalFlags.BoneTypeMask) == BoneAdditionalFlags.BoneTypeRoot; } }
		//public bool isCharBone				{ get { return (boneAdditionalFlags & BoneAdditionalFlags.BoneTypeMask) == BoneAdditionalFlags.BoneTypeChar; } }
		//public bool isModLeftThumb0Bone		{ get { return (boneAdditionalFlags & BoneAdditionalFlags.BoneTypeMask) == BoneAdditionalFlags.BoneTypeModLeftThumb0; } }
		//public bool isModRightThumb0Bone		{ get { return (boneAdditionalFlags & BoneAdditionalFlags.BoneTypeMask) == BoneAdditionalFlags.BoneTypeModRightThumb0; } }
		//public bool isModLeftShoulderBone		{ get { return (boneAdditionalFlags & BoneAdditionalFlags.BoneTypeMask) == BoneAdditionalFlags.BoneTypeModLeftShoulder; } }
		//public bool isModRightShoulderBone	{ get { return (boneAdditionalFlags & BoneAdditionalFlags.BoneTypeMask) == BoneAdditionalFlags.BoneTypeModRightShoulder; } }
	}

	[System.Serializable]
	public class IKLinkData
	{
		public int							ikLinkBoneID;
		public IKLinkFlags					ikLinkFlags;
		public Vector3						lowerLimit;
		public Vector3						upperLimit;
		public Vector3						lowerLimitAsDegree; // for IK
		public Vector3						upperLimitAsDegree; // for IK

		public bool hasAngleJoint { get { return (ikLinkFlags & IKLinkFlags.HasAngleJoint) != IKLinkFlags.None; } }
	}

	[System.Serializable]
	public class IKData
	{
		public IKAdditionalFlags			ikAdditionalFlags;
		public int							destBoneID;
		public int							targetBoneID;
		public int							iteration;
		public float						angleConstraint;
		public IKLinkData[]					ikLinkDataList;
	}

	[System.Serializable]
	public class RigidBodyData
	{
		public RigidBodyAdditionalFlags		rigidBodyAdditionalFlags;
		public string						nameJp;
		public string						nameEn;
		public int							boneID;
		public int							collisionGroupID;
		public int							collisionMask;
		public ShapeType					shapeType;
		public RigidBodyType				rigidBodyType;
		public Vector3						shapeSize;
		public Vector3						position;
		public Vector3						rotation;
		public float						mass;
		public float						linearDamping;
		public float						angularDamping;
		public float						restitution;
		public float						friction;

		public bool isFreezed { get { return (rigidBodyAdditionalFlags & RigidBodyAdditionalFlags.IsFreezed) != RigidBodyAdditionalFlags.None; } }
	}

	[System.Serializable]
	public class JointData
	{
		public int							rigidBodyIDA;
		public int							rigidBodyIDB;
	}

	public enum MorphMaterialOperation
	{
		Multiply,
		Adding,
	}

	public struct MorphMaterialData
	{
		public int							materialID;
		public MorphMaterialOperation		operation;
		public Color						diffuse;
		public Color						specular;
		public float						shininess;
		public Color						ambient;
		public Color						edgeColor;
		public float						edgeScale;
		public float						edgeSize;
		public Color						textureColor;
		public Color						sphereColor;
		public Color						toonTextureColor;

		// for AutoLuminous
		public float						alPower;
	}

	[System.Serializable]
	public class MorphData
	{
		public string						nameJp;
		public MorphCategory				morphCategory;
		public MorphType					morphType;
		
		[NonSerialized]
		public bool							isMorphBaseVertex; // for PMD
		[NonSerialized]
		public int[]						indices;
		[NonSerialized]
		public float[]						weights;
		[NonSerialized]
		public Vector3[]					positions;
		[NonSerialized]
		public MorphMaterialData[]			materialData;
	}
	
	public class ModelData
	{
		public FileType						fileType;
		public int							vertexCount;
		public float						vertexScale;
		public float						importScale;
		public BoneData[]					boneDataList;
		public Dictionary< string, int >	boneDataDictionary;
		public IKData[]						ikDataList;
		public MorphData[]					morphDataList;
		public Dictionary< string, int >	morphDataDictionary;
		public RigidBodyData[]				rigidBodyDataList;
		public JointData[]					jointDataList;

		public int GetMorphDataIndex( string morphName, bool isStartsWith )
		{
			if( morphName != null && this.morphDataList != null ) {
				if( morphDataDictionary != null ) {
					int morphIndex = 0;
					if( morphDataDictionary.TryGetValue( morphName, out morphIndex ) ) {
						return morphIndex;
					}
				}
				if( isStartsWith ) {
					for( int i = 0; i < this.morphDataList.Length; ++i ) {
						if( this.morphDataList[i].nameJp != null &&
						   this.morphDataList[i].nameJp.StartsWith( morphName ) ) {
							return i;
						}
					}
				}
			}

			return -1;
		}

		public MorphData GetMorphData( string morphName, bool isStartsWith )
		{
			int morphDataIndex = GetMorphDataIndex( morphName, isStartsWith );
			if( morphDataIndex != -1 ) {
				return this.morphDataList[morphDataIndex];
			}
			
			return null;
		}

		public MorphData GetMorphData( string morphName )
		{
			return GetMorphData( morphName, false );
		}
	}

	//------------------------------------------------------------------------------------------------------------

	[System.Flags]
	public enum VertexFlags
	{
		None		= 0,
		NoEdge		= 0x01,
	}

	public struct VertexData // Optimized: Not class.
	{
		public VertexFlags					flags;
		public Vector3						position;
		public Vector3						normal;
		public Vector2						uv;
		public float						edgeScale;

		public BoneTransform				boneTransform;
		public BoneWeight					boneWeight;

		public Vector3						sdefC;
		public Vector3						sdefR0;
		public Vector3						sdefR1;
	}

	public class ExtraData
	{
		public int							vertexCount;
		public float						vertexScale;
		public float						importScale;
		public VertexData[]					vertexDataList;
	}

	//------------------------------------------------------------------------------------------------------------

	[System.Flags]
	public enum MorphMotionAdditionalFlags
	{
		None				= 0x00000000,
		NameIsFull			= 0x00000001, // Require use startsWith
	}

	public class MorphMotionData
	{
		public string						name;
		public MorphMotionAdditionalFlags	morphMotionAdditionalFlags;
		public int[]						frameNos;
		public float[]						f_frameNos;
		public float[]						weights;

		public bool nameIsFull { get { return
				(this.morphMotionAdditionalFlags & MorphMotionAdditionalFlags.NameIsFull)
					!= MorphMotionAdditionalFlags.None; } }
	}

	[System.Flags]
	public enum AnimAdditionalFlags
	{
		None				= 0x00000000,
		SupportNameIsFull	= 0x00000001,
	}

	public class AnimData
	{
		public AnimAdditionalFlags			animAdditionalFlags;
		public int							maxFrame;
		public MorphMotionData[]			morphMotionDataList;

		public bool supportNameIsFull { get { return
				(this.animAdditionalFlags & AnimAdditionalFlags.SupportNameIsFull)
					!= AnimAdditionalFlags.None; } }
	}

	//------------------------------------------------------------------------------------------------------------

	public static ModelData BuildModelData( TextAsset modelFile )
	{
		if( modelFile == null ) {
			Debug.LogError( "BuildModelData: modelFile is norhing." );
			return null;
		}
		
		byte[] modelBytes = modelFile.bytes;
		
		if( modelBytes == null || modelBytes.Length == 0 ) {
			Debug.LogError( "BuildModelData: Nothing modelBytes." );
			return null;
		}
		
		MMD4MecanimCommon.BinaryReader binaryReader = new MMD4MecanimCommon.BinaryReader( modelBytes );
		if( !binaryReader.Preparse() ) {
			Debug.LogError( "BuildModelData:modelFile is unsupported fomart." );
			return null;
		}
		
		ModelData modelData = new ModelData();
		
		binaryReader.BeginHeader();
		modelData.fileType = (FileType)binaryReader.ReadHeaderInt(); // fileType
		binaryReader.ReadHeaderFloat(); // fileVersion
		binaryReader.ReadHeaderInt(); // fileVersion(BIN)
		binaryReader.ReadHeaderInt(); // additionalFlags
		modelData.vertexCount = binaryReader.ReadHeaderInt();
		binaryReader.ReadHeaderInt(); // vertexIndexCount
		modelData.vertexScale = binaryReader.ReadHeaderFloat(); // vertexScale
		modelData.importScale = binaryReader.ReadHeaderFloat(); // importScale
		binaryReader.EndHeader();
		
		int structListLength = binaryReader.structListLength;
		for( int structListIndex = 0; structListIndex < structListLength; ++structListIndex ) {
			if( !binaryReader.BeginStructList() ) {
				Debug.LogError("BuildModelData: Parse error.");
				return null;
			}
			int structFourCC = binaryReader.currentStructFourCC;
			if( structFourCC == MMD4MecanimCommon.BinaryReader.MakeFourCC( "BONE" ) ) {
				if( !_ParseBoneData( modelData, binaryReader ) ) {
					Debug.LogError("BuildModelData: Parse error.");
					return null;
				}
			} else if( structFourCC == MMD4MecanimCommon.BinaryReader.MakeFourCC( "IK__" ) ) {
				if( !_ParseIKData( modelData, binaryReader ) ) {
					Debug.LogError("BuildModelData: Parse error.");
					return null;
				}
			} else if( structFourCC == MMD4MecanimCommon.BinaryReader.MakeFourCC( "MRPH" ) ) {
				if( !_ParseMorphData( modelData, binaryReader ) ) {
					Debug.LogError("BuildModelData: Parse error.");
					return null;
				}
			} else if( structFourCC == MMD4MecanimCommon.BinaryReader.MakeFourCC( "RGBD" ) ) {
				if( !_ParseRigidBodyData( modelData, binaryReader ) ) {
					Debug.LogError("BuildModelData: Parse error.");
					return null;
				}
			} else if( structFourCC == MMD4MecanimCommon.BinaryReader.MakeFourCC( "JOIN" ) ) {
				if( !_ParseJointData( modelData, binaryReader ) ) {
					Debug.LogError("BuildModelData: Parse error.");
					return null;
				}
			}
			if( !binaryReader.EndStructList() ) {
				Debug.LogError("BuildModelData: Parse error.");
				return null;
			}
		}

		//RefreshModelData( modelData );
		return modelData;
	}

	/*
	public static void RefreshModelData( ModelData modelData )
	{
		if( modelData != null && modelData.boneDataList != null && modelData.rigidBodyDataList != null ) {
			for( int i = 0; i < modelData.boneDataList.Length; ++i ) {
				modelData.boneDataList[i].isRigidBodySimulated = false;
			}
			for( int i = 0; i < modelData.rigidBodyDataList.Length; ++i ) {
				RigidBodyData rigidBodyData = modelData.rigidBodyDataList[i];
				RigidBodyType rigidBodyType = rigidBodyData.rigidBodyType;
				if( rigidBodyType == RigidBodyType.Simulated || rigidBodyType == RigidBodyType.SimulatedAligned ) {
					if( rigidBodyData.boneID >= 0 && rigidBodyData.boneID < modelData.boneDataList.Length ) {
						modelData.boneDataList[rigidBodyData.boneID].isRigidBodySimulated = true;
					}
				}
			}
		}
	}
	*/

	private static bool _ParseBoneData( ModelData modelData, MMD4MecanimCommon.BinaryReader binaryReader )
	{
		modelData.boneDataDictionary = new Dictionary<string, int>();
		modelData.boneDataList = new BoneData[binaryReader.currentStructLength];
		for( int structIndex = 0; structIndex < binaryReader.currentStructLength; ++structIndex ) {
			if( !binaryReader.BeginStruct() ) {
				return false;
			}
			
			BoneData boneData = new BoneData();

			boneData.boneAdditionalFlags = (BoneAdditionalFlags)binaryReader.ReadStructInt();
			boneData.nameJp = binaryReader.GetName( binaryReader.ReadStructInt() );
			binaryReader.ReadStructInt(); // nameEn
			boneData.skeletonName = binaryReader.GetName( binaryReader.ReadStructInt() );
			boneData.parentBoneID = binaryReader.ReadStructInt();
			boneData.sortedBoneID = binaryReader.ReadStructInt();
			binaryReader.ReadStructInt(); // orderedBoneID
			boneData.originalParentBoneID = binaryReader.ReadStructInt(); // originalParentBoneID
			boneData.originalSortedBoneID = binaryReader.ReadStructInt(); // originalSortedBoneID
			boneData.baseOrigin = binaryReader.ReadStructVector3(); // baseOriginAsLeftHand
			// Z-Back to Z-Front(MMD/Unity as LeftHand)
			boneData.baseOrigin.x = -boneData.baseOrigin.x;
			boneData.baseOrigin.z = -boneData.baseOrigin.z;
			if( modelData.fileType == FileType.PMD ) {
				boneData.pmdBoneType = (PMDBoneType)binaryReader.ReadStructInt();
				boneData.childBoneID = binaryReader.ReadStructInt();
				boneData.targetBoneID = binaryReader.ReadStructInt();
				boneData.followCoef = binaryReader.ReadStructFloat();
			} else if( modelData.fileType == FileType.PMX ) {
				boneData.transformLayerID = binaryReader.ReadStructInt();
				boneData.pmxBoneFlags = (PMXBoneFlags)binaryReader.ReadStructInt();
				boneData.inherenceParentBoneID = binaryReader.ReadStructInt();
				boneData.inherenceWeight = binaryReader.ReadStructFloat();
				boneData.externalID = binaryReader.ReadStructInt();
			}

			if( !binaryReader.EndStruct() ) {
				return false;
			}
			
			modelData.boneDataList[structIndex] = boneData;
			if( !string.IsNullOrEmpty( boneData.skeletonName ) ) {
				modelData.boneDataDictionary[boneData.skeletonName] = structIndex;
			}
		}
		
		return true;
	}

	private static Vector3 _ToDegree( Vector3 radian )
	{
		return new Vector3(
			radian.x * Mathf.Rad2Deg,
			radian.y * Mathf.Rad2Deg,
			radian.z * Mathf.Rad2Deg );
	}

	private static bool _ParseIKData( ModelData modelData, MMD4MecanimCommon.BinaryReader binaryReader )
	{
		modelData.ikDataList = new IKData[binaryReader.currentStructLength];
		for( int structIndex = 0; structIndex < binaryReader.currentStructLength; ++structIndex ) {
			if( !binaryReader.BeginStruct() ) {
				return false;
			}
			
			IKData ikData					= new IKData();
			ikData.ikAdditionalFlags		= (IKAdditionalFlags)binaryReader.ReadStructInt();
			ikData.destBoneID				= binaryReader.ReadStructInt();
			ikData.targetBoneID				= binaryReader.ReadStructInt();
			ikData.iteration				= binaryReader.ReadStructInt();
			ikData.angleConstraint			= binaryReader.ReadStructFloat();
			int ikLinkCount					= binaryReader.ReadStructInt();
			ikData.ikLinkDataList			= new IKLinkData[ikLinkCount];
			for( int i = 0; i < ikLinkCount; ++i ) {
				IKLinkData ikLinkData		= new IKLinkData();
				ikLinkData.ikLinkBoneID		= binaryReader.ReadInt();
				ikLinkData.ikLinkFlags		= (IKLinkFlags)binaryReader.ReadInt();
				if( (ikLinkData.ikLinkFlags & IKLinkFlags.HasAngleJoint) != IKLinkFlags.None ) {
					Vector3 lowerLimit		= binaryReader.ReadVector3();
					Vector3 upperLimit		= binaryReader.ReadVector3();
					ikLinkData.lowerLimit	= lowerLimit;
					ikLinkData.upperLimit	= upperLimit;
					// Z-Back to Z-Front(MMD/Unity as LeftHand)
					ikLinkData.lowerLimit	= new Vector3( -upperLimit[0], lowerLimit[1], -upperLimit[2] );
					ikLinkData.upperLimit	= new Vector3( -lowerLimit[0], upperLimit[1], -lowerLimit[2] );
					// Radian to Degree(for IK)
					ikLinkData.lowerLimitAsDegree = _ToDegree( ikLinkData.lowerLimit );
					ikLinkData.upperLimitAsDegree = _ToDegree( ikLinkData.upperLimit );
				}
				ikData.ikLinkDataList[i]	= ikLinkData;
			}

			if( !binaryReader.EndStruct() ) {
				return false;
			}
			
			modelData.ikDataList[structIndex] = ikData;
		}
		
		return true;
	}

	private static bool _ParseMorphData( ModelData modelData, MMD4MecanimCommon.BinaryReader binaryReader )
	{
		modelData.morphDataDictionary = new Dictionary<string, int>();
		modelData.morphDataList = new MorphData[binaryReader.currentStructLength];
		for( int structIndex = 0; structIndex < binaryReader.currentStructLength; ++structIndex ) {
			if( !binaryReader.BeginStruct() ) {
				return false;
			}
			
			MorphData morphData = new MorphData();
			
			int additionalFlags = binaryReader.ReadStructInt();
			int nameJp = binaryReader.ReadStructInt();
			binaryReader.ReadStructInt();
			int morphCategory = binaryReader.ReadStructInt();
			int morphType = binaryReader.ReadStructInt();
			int indexCount = binaryReader.ReadStructInt();
			
			morphData.nameJp		= binaryReader.GetName( nameJp );
			morphData.morphCategory	= (MorphCategory)morphCategory;
			morphData.morphType		= (MorphType)morphType;
			if( (additionalFlags & 0x01) != 0 ) {
				morphData.isMorphBaseVertex = true;
			}
			
			switch( morphType ) {
			case (int)MorphType.Vertex:
				#if false
				morphData.indices = new int[indexCount];
				morphData.positions = new Vector3[indexCount];
				for( int i = 0; i < indexCount; ++i ) {
					morphData.indices[i] = binaryReader.ReadInt();
					morphData.positions[i] = binaryReader.ReadVector3();
					if( (uint)morphData.indices[i] >= modelData.vertexCount ) {
						Debug.LogError( "[" + structIndex + ":" + morphData.nameJp + "]:Invalid index. " + i + ":" + morphData.indices[i] );
						return false;
					}
				}
				#endif
				break;
			case (int)MorphType.Group:
				morphData.indices = new int[indexCount];
				morphData.weights = new float[indexCount];
				for( int i = 0; i < indexCount; ++i ) {
					morphData.indices[i] = binaryReader.ReadInt();
					morphData.weights[i] = binaryReader.ReadFloat();
				}
				break;
			case (int)MorphType.Material:
				morphData.materialData = new MorphMaterialData[indexCount];
				for( int i = 0; i < indexCount; ++i ) {
					MorphMaterialData materialData = new MorphMaterialData();
					materialData.materialID = binaryReader.ReadInt();
					materialData.operation = (MorphMaterialOperation)binaryReader.ReadInt();
					materialData.diffuse = binaryReader.ReadColor();
					materialData.specular = binaryReader.ReadColorRGB();
					materialData.shininess = binaryReader.ReadFloat();
					materialData.ambient = binaryReader.ReadColorRGB();
					materialData.edgeColor = binaryReader.ReadColor();
					materialData.edgeSize = binaryReader.ReadFloat();
					materialData.textureColor = binaryReader.ReadColor();
					materialData.sphereColor = binaryReader.ReadColor();
					materialData.toonTextureColor = binaryReader.ReadColor();

					if( materialData.operation == MorphMaterialOperation.Adding ) {
						materialData.specular.a = 0;
						materialData.ambient.a = 0;
					}

					morphData.materialData[i] = materialData;
				}
				break;
			}

			if( !binaryReader.EndStruct() ) {
				return false;
			}
			
			modelData.morphDataList[structIndex] = morphData;
			if( !string.IsNullOrEmpty( morphData.nameJp ) ) {
				modelData.morphDataDictionary[morphData.nameJp] = structIndex;
			}
		}
		
		return true;
	}

	private static bool _ParseRigidBodyData( ModelData modelData, MMD4MecanimCommon.BinaryReader binaryReader )
	{
		modelData.rigidBodyDataList = new RigidBodyData[binaryReader.currentStructLength];
		for( int structIndex = 0; structIndex < binaryReader.currentStructLength; ++structIndex ) {
			if( !binaryReader.BeginStruct() ) {
				return false;
			}
			
			RigidBodyData rigidBodyData = new RigidBodyData();
			int additionalFlags = binaryReader.ReadStructInt(); // _additionalFlags
			rigidBodyData.nameJp			= binaryReader.GetName( binaryReader.ReadStructInt() );
			rigidBodyData.nameEn			= binaryReader.GetName( binaryReader.ReadStructInt() );
			rigidBodyData.boneID			= binaryReader.ReadStructInt();
			rigidBodyData.collisionGroupID	= binaryReader.ReadStructInt();
			rigidBodyData.collisionMask		= binaryReader.ReadStructInt();
			rigidBodyData.shapeType			= (ShapeType)binaryReader.ReadStructInt();
			rigidBodyData.rigidBodyType		= (RigidBodyType)binaryReader.ReadStructInt();
			rigidBodyData.shapeSize			= binaryReader.ReadStructVector3();
			rigidBodyData.position			= binaryReader.ReadStructVector3();
			rigidBodyData.rotation			= binaryReader.ReadStructVector3();
			rigidBodyData.mass				= binaryReader.ReadStructFloat();
			rigidBodyData.linearDamping		= binaryReader.ReadStructFloat();
			rigidBodyData.angularDamping	= binaryReader.ReadStructFloat();
			rigidBodyData.restitution		= binaryReader.ReadStructFloat();
			rigidBodyData.friction			= binaryReader.ReadStructFloat();

			rigidBodyData.rigidBodyAdditionalFlags = (RigidBodyAdditionalFlags)additionalFlags;

			modelData.rigidBodyDataList[structIndex] = rigidBodyData;
			if( !binaryReader.EndStruct() ) {
				return false;
			}
		}
		
		return true;
	}

	private static bool _ParseJointData( ModelData modelData, MMD4MecanimCommon.BinaryReader binaryReader )
	{
		modelData.jointDataList = new JointData[binaryReader.currentStructLength];
		for( int structIndex = 0; structIndex < binaryReader.currentStructLength; ++structIndex ) {
			if( !binaryReader.BeginStruct() ) {
				return false;
			}
			
			JointData jointData = new JointData();
			binaryReader.ReadStructInt(); // _additionalFlags
			binaryReader.ReadStructInt(); // nameJp
			binaryReader.ReadStructInt(); // nameEn
			binaryReader.ReadStructInt(); // jointType
			jointData.rigidBodyIDA	= binaryReader.ReadStructInt();
			jointData.rigidBodyIDB	= binaryReader.ReadStructInt();
			modelData.jointDataList[structIndex] = jointData;

			if( !binaryReader.EndStruct() ) {
				return false;
			}
		}

		return true;
	}

	//------------------------------------------------------------------------------------------------

	public static ExtraData BuildExtraData( TextAsset extraFile )
	{
		if( extraFile == null ) {
			Debug.LogError( "BuildExtraData: extraFile is norhing." );
			return null;
		}
		
		byte[] extraBytes = extraFile.bytes;
		
		if( extraBytes == null || extraBytes.Length == 0 ) {
			Debug.LogError( "BuildModelData: Nothing extraBytes." );
			return null;
		}
		
		MMD4MecanimCommon.BinaryReader binaryReader = new MMD4MecanimCommon.BinaryReader( extraBytes );
		if( !binaryReader.Preparse() ) {
			Debug.LogError( "BuildModelData:extraFile is unsupported fomart." );
			return null;
		}
		
		ExtraData extraData = new ExtraData();
		
		binaryReader.BeginHeader();
		binaryReader.ReadHeaderInt(); // fileType
		binaryReader.ReadHeaderFloat(); // fileVersion
		binaryReader.ReadHeaderInt(); // fileVersion(EXT)
		binaryReader.ReadHeaderInt(); // additionalFlags
		extraData.vertexCount = binaryReader.ReadHeaderInt();
		binaryReader.ReadHeaderInt(); // vertexIndexCount
		extraData.vertexScale = binaryReader.ReadHeaderFloat(); // vertexScale
		extraData.importScale = binaryReader.ReadHeaderFloat(); // importScale
		binaryReader.EndHeader();
		
		int structListLength = binaryReader.structListLength;
		for( int structListIndex = 0; structListIndex < structListLength; ++structListIndex ) {
			if( !binaryReader.BeginStructList() ) {
				Debug.LogError("BuildExtraData: Parse error.");
				return null;
			}
			int structFourCC = binaryReader.currentStructFourCC;
			if( structFourCC == MMD4MecanimCommon.BinaryReader.MakeFourCC( "VTX_" ) ) {
				if( !_ParseVertexData( extraData, binaryReader ) ) {
					Debug.LogError("BuildExtraData: Parse error.");
					return null;
				}
			}
			if( !binaryReader.EndStructList() ) {
				Debug.LogError("BuildExtraData: Parse error.");
				return null;
			}
		}

		return extraData;
	}

	private static bool _ParseVertexData( ExtraData extraData, MMD4MecanimCommon.BinaryReader binaryReader )
	{
		extraData.vertexDataList = new VertexData[binaryReader.currentStructLength];
		for( int structIndex = 0; structIndex < binaryReader.currentStructLength; ++structIndex ) {
			if( !binaryReader.BeginStruct() ) {
				return false;
			}
			
			VertexData vertexData = new VertexData();

			unchecked {
				vertexData.flags			= (VertexFlags)binaryReader.ReadStructInt();
				vertexData.position			= binaryReader.ReadStructVector3();
				vertexData.normal			= binaryReader.ReadStructVector3();
				vertexData.uv				= binaryReader.ReadStructVector2();
				vertexData.edgeScale		= binaryReader.ReadStructFloat();
				vertexData.boneTransform	= (BoneTransform)binaryReader.ReadStructByte();
				vertexData.boneWeight		= new BoneWeight();


				switch( vertexData.boneTransform ) {
				case BoneTransform.BDEF1:
					vertexData.boneWeight.boneIndex0 = binaryReader.ReadInt();
					vertexData.boneWeight.weight0 = 1.0f;
					break;
				case BoneTransform.BDEF2:
				case BoneTransform.SDEF:
					vertexData.boneWeight.boneIndex0 = binaryReader.ReadInt();
					vertexData.boneWeight.boneIndex1 = binaryReader.ReadInt();
					vertexData.boneWeight.weight0 = binaryReader.ReadFloat();
					vertexData.boneWeight.weight1 = 1.0f - vertexData.boneWeight.weight0;
					break;
				case BoneTransform.BDEF4:
				case BoneTransform.QDEF:
					vertexData.boneWeight.boneIndex0 = binaryReader.ReadInt();
					vertexData.boneWeight.boneIndex1 = binaryReader.ReadInt();
					vertexData.boneWeight.boneIndex2 = binaryReader.ReadInt();
					vertexData.boneWeight.boneIndex3 = binaryReader.ReadInt();
					vertexData.boneWeight.weight0 = binaryReader.ReadFloat();
					vertexData.boneWeight.weight1 = binaryReader.ReadFloat();
					vertexData.boneWeight.weight2 = binaryReader.ReadFloat();
					vertexData.boneWeight.weight3 = binaryReader.ReadFloat();
					break;
				}

				if( vertexData.boneTransform == BoneTransform.SDEF ) {
					vertexData.sdefC = binaryReader.ReadVector3();
					vertexData.sdefR0 = binaryReader.ReadVector3();
					vertexData.sdefR1 = binaryReader.ReadVector3();
					//Debug.Log ( "structIndex:" + structIndex + " C:" + vertexData.sdefC + " R0:" + vertexData.sdefR0 + " R1:" + vertexData.sdefR1 );
				}
			}

			if( !binaryReader.EndStruct() ) {
				return false;
			}
			
			extraData.vertexDataList[structIndex] = vertexData;
		}
		
		return true;
	}

	public static AnimData BuildAnimData( TextAsset animFile )
	{
		if( animFile == null ) {
			Debug.LogError( "BuildAnimData: animFile is norhing." );
			return null;
		}
		
		byte[] animBytes = animFile.bytes;
		
		if( animBytes == null || animBytes.Length == 0 ) {
			Debug.LogError( "BuildAnimData: Nothing animBytes." );
			return null;
		}
		
		MMD4MecanimCommon.BinaryReader binaryReader = new MMD4MecanimCommon.BinaryReader( animBytes );
		if( !binaryReader.Preparse() ) {
			Debug.LogError( "BuildAnimData:animFile is unsupported fomart." );
			return null;
		}
		
		AnimData animData = new AnimData();
		
		binaryReader.BeginHeader();
		binaryReader.ReadHeaderInt(); // fileVersion(BIN)
		binaryReader.ReadHeaderInt(); // additionalFlags
		animData.maxFrame = binaryReader.ReadHeaderInt();
		binaryReader.EndHeader();
		
		int structListLength = binaryReader.structListLength;
		for( int structListIndex = 0; structListIndex < structListLength; ++structListIndex ) {
			if( !binaryReader.BeginStructList() ) {
				Debug.LogError("BuildAnimData: Parse error.");
				return null;
			}
			int structFourCC = binaryReader.currentStructFourCC;
			if( structFourCC == MMD4MecanimCommon.BinaryReader.MakeFourCC( "MRPH" ) ) {
				if( !_ParseMorphMotionData( animData, binaryReader ) ) {
					Debug.LogError("BuildAnimData: Parse error.");
					return null;
				}
			}
			if( !binaryReader.EndStructList() ) {
				Debug.LogError("BuildAnimData: Parse error.");
				return null;
			}
		}
		
		return animData;
	}

	private static bool _ParseMorphMotionData( AnimData animData, MMD4MecanimCommon.BinaryReader binaryReader )
	{
		animData.morphMotionDataList = new MMD4MecanimData.MorphMotionData[binaryReader.currentStructLength];
		for( int structIndex = 0; structIndex < binaryReader.currentStructLength; ++structIndex ) {
			if( !binaryReader.BeginStruct() ) {
				return false;
			}
			
			MorphMotionData morphMotionData = new MorphMotionData();
			binaryReader.ReadStructInt();
			morphMotionData.name = binaryReader.GetName( binaryReader.ReadStructInt() );
			int keyFrameLength = binaryReader.ReadStructInt();
			if( keyFrameLength < 0 ) {
				return false;
			}
			
			morphMotionData.frameNos = new int[keyFrameLength];
			morphMotionData.f_frameNos = new float[keyFrameLength];
			morphMotionData.weights = new float[keyFrameLength];
			
			for( int i = 0; i < keyFrameLength; ++i ) {
				morphMotionData.frameNos[i] = binaryReader.ReadInt();
				morphMotionData.weights[i] = binaryReader.ReadFloat();
				morphMotionData.f_frameNos[i] = (float)morphMotionData.frameNos[i];
			}
			
			animData.morphMotionDataList[structIndex] = morphMotionData;
			
			if( !binaryReader.EndStruct() ) {
				return false;
			}
		}
		
		return true;
	}
}
