using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using MorphCategory		= MMD4MecanimData.MorphCategory;
using MorphType			= MMD4MecanimData.MorphType;
using MorphData			= MMD4MecanimData.MorphData;
using MorphMotionData	= MMD4MecanimData.MorphMotionData;
using BoneData			= MMD4MecanimData.BoneData;
using RigidBodyData		= MMD4MecanimData.RigidBodyData;
using FileType			= MMD4MecanimData.FileType;
using PMDBoneType		= MMD4MecanimData.PMDBoneType;
using PMXBoneFlags		= MMD4MecanimData.PMXBoneFlags;
using FastVector3		= MMD4MecanimCommon.FastVector3;
using FastQuaternion	= MMD4MecanimCommon.FastQuaternion;
using RigidBody			= MMD4MecanimModel.RigidBody;

public class MMD4MecanimBone : MonoBehaviour
{
	public MMD4MecanimModel	model;
	public int				boneID = -1;
	public bool				ikEnabled = false;
	public float			ikWeight = 1.0f;
	[System.NonSerialized]
	public int				humanBodyBones = -1;

	BoneData				_boneData;

	[System.NonSerialized]
	public Vector3			_userPosition = Vector3.zero; // from MMD4MecanimBulletPhysics
	[System.NonSerialized]
	public Vector3			_userEulerAngles = Vector3.zero;
	[System.NonSerialized]
	public Quaternion		_userRotation = Quaternion.identity; // from MMD4MecanimBulletPhysics

	[System.NonSerialized]
	public bool				_userPositionIsZero = true; // from MMD4MecanimBulletPhysics
	[System.NonSerialized]
	public bool				_userRotationIsIdentity = true; // from MMD4MecanimBulletPhysics

	public BoneData boneData { get { return _boneData; } }

	public Vector3 userPosition {
		get {
			return _userPosition;
		}
		set {
			if( _userPosition != value ) {
				_userPosition = value;
				_userPositionIsZero = MMD4MecanimCommon.FuzzyZero( value );
			}
		}
	}

	public Vector3 userEulerAngles {
		get {
			return _userEulerAngles;
		}
		set {
			if( _userEulerAngles != value ) {
				if( MMD4MecanimCommon.FuzzyZero( value ) ) {
					_userRotation = Quaternion.identity;
					_userEulerAngles = Vector3.zero;
					_userRotationIsIdentity = true;
				} else {
					_userRotation = Quaternion.Euler( value );
					_userEulerAngles = value;
					_userRotationIsIdentity = false;
				}
			}
		}
	}

	public Quaternion userRotation {
		get {
			return _userRotation;
		}
		set {
			if( _userRotation != value ) {
				if( MMD4MecanimCommon.FuzzyIdentity( value ) ) { // Optimized: userRotation == (0,0,0)
					_userRotation = Quaternion.identity;
					_userEulerAngles = Vector3.zero;
					_userRotationIsIdentity = true;
				} else {
					_userRotation = value;
					_userEulerAngles = value.eulerAngles;
					_userRotationIsIdentity = false;
				}
			}
		}
	}

	public void Setup()
	{
		if( this.model == null || this.model.modelData == null || this.model.modelData.boneDataList == null ||
		    this.boneID < 0 || this.boneID >= this.model.modelData.boneDataList.Length ) {
			return;
		}

		_boneData = this.model.modelData.boneDataList[this.boneID];
	}
	
	public void Bind()
	{
		if( this.model == null || _boneData == null ) {
			#if MMD4MECANIM_DEBUG
			Debug.LogError("");
			#endif
			return;
		}
	}
	
	public void Destroy()
	{
		_boneData = null;
	}
}
