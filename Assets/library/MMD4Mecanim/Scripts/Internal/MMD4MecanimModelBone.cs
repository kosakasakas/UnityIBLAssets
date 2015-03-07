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
using FileType			= MMD4MecanimData.FileType;
using PMDBoneType		= MMD4MecanimData.PMDBoneType;
using PMXBoneFlags		= MMD4MecanimData.PMXBoneFlags;

// pending: Inherence RigidBody support
// pending: Transform after physics support

public partial class MMD4MecanimModel
{
	// from _InitializeModel()
	void _BindBone()
	{
		Transform transform = this.gameObject.transform;
		foreach( Transform trn in transform ) {
			_BindBone( trn );
		}
	}
	
	void _BindBone( Transform trn )
	{
		if( !string.IsNullOrEmpty( trn.gameObject.name ) ) {
			int boneID = 0;
			if( _modelData.boneDataDictionary.TryGetValue( trn.gameObject.name, out boneID ) ) {
				MMD4MecanimBone bone = trn.gameObject.GetComponent< MMD4MecanimBone >();
				if( bone == null ) {
					bone = trn.gameObject.AddComponent< MMD4MecanimBone >();
				}
				bone.model = this;
				bone.boneID = boneID;
				bone.Setup();
				this.boneList[boneID] = bone;
				if( this.boneList[boneID].boneData != null && this.boneList[boneID].boneData.isRootBone ) {
					_rootBone = this.boneList[boneID];
				}
			}
		}
		foreach( Transform t in trn ) {
			_BindBone( t );
		}
	}
	
	//--------------------------------------------------------------------------------------------------------------------------------------------

	void _UpdateBone()
	{
		// Nothing.
	}

	void _LateUpdateBone()
	{
		_UpdatePPHBones();
	}

	//--------------------------------------------------------------------------------------------------------------------------------------------

	bool _isGenericAnimation {
		get {
			if( _animator == null ) {
				return false;
			}

			AnimatorClipInfo[] animationInfos = _animator.GetCurrentAnimatorClipInfo(0);
			if( animationInfos == null || animationInfos.Length == 0 ) {
				return false;
			}
			return !_animator.isHuman;
		}
	}

	//--------------------------------------------------------------------------------------------------------------------------------------------

	void _InitializePPHBones()
	{
		if( _animator == null || _animator.avatar == null || !_animator.avatar.isValid || !_animator.avatar.isHuman ) {
			return;
		}
		{
			Transform leftShoulderTransform = _animator.GetBoneTransform(HumanBodyBones.LeftShoulder);
			Transform leftArmTransform = _animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
			if( leftShoulderTransform != null && leftArmTransform != null ) {
				MMD4MecanimBone leftShoulderBone = leftShoulderTransform.gameObject.GetComponent< MMD4MecanimBone >();
				MMD4MecanimBone leftArmBone = leftArmTransform.gameObject.GetComponent< MMD4MecanimBone >();
				if( leftShoulderBone != null ) {
					leftShoulderBone.humanBodyBones = (int)HumanBodyBones.LeftShoulder;
				}
				if( leftArmBone != null ) {
					leftArmBone.humanBodyBones = (int)HumanBodyBones.LeftUpperArm;
				}
			}
		}
		{
			Transform rightShoulderTransform = _animator.GetBoneTransform(HumanBodyBones.RightShoulder);
			Transform rightArmTransform = _animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
			if( rightShoulderTransform != null && rightArmTransform != null ) {
				MMD4MecanimBone rightShoulderBone = rightShoulderTransform.gameObject.GetComponent< MMD4MecanimBone >();
				MMD4MecanimBone rightArmBone = rightArmTransform.gameObject.GetComponent< MMD4MecanimBone >();
				if( rightShoulderBone != null ) {
					rightShoulderBone.humanBodyBones = (int)HumanBodyBones.RightShoulder;
				}
				if( rightArmBone != null ) {
					rightArmBone.humanBodyBones = (int)HumanBodyBones.RightUpperArm;
				}
			}
		}
	}

	public void ForceUpdatePPHBones()
	{
		_UpdatePPHBones();
	}

	void _UpdatePPHBones()
	{
		_pphShoulderFixRateImmediately = 0.0f;

		if( !this.pphEnabled ) {
			return;
		}
		if( _animator == null ) {
			return;
		}

		bool isNoAnimation = false;
		AnimatorClipInfo[] animationInfos = _animator.GetCurrentAnimatorClipInfo(0);
		if( animationInfos == null || animationInfos.Length == 0 ) {
			isNoAnimation = true;
			if( !this.pphEnabledNoAnimation ) {
				return; // No playing animation.
			}
		}

		float pphRate = 0.0f;
		if( isNoAnimation ) {
			pphRate = 1.0f; // pphEnabledNoAnimation
		} else {
			foreach( AnimatorClipInfo animationInfo in animationInfos ) {
				if( !animationInfo.clip.name.EndsWith( ".vmd" ) ) {
					pphRate += animationInfo.weight;
				}
			}
			if( pphRate <= Mathf.Epsilon ) {
				return;
			}
		}
		
		float pphShoulderFixRate = this.pphShoulderFixRate * pphRate;
		_pphShoulderFixRateImmediately = pphShoulderFixRate;
	}
}
