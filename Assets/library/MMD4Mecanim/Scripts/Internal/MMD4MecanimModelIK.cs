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
using IKData			= MMD4MecanimData.IKData;
using IKLinkData		= MMD4MecanimData.IKLinkData;
using FileType			= MMD4MecanimData.FileType;

using Bone				= MMD4MecanimBone;

public partial class MMD4MecanimModel
{
	public class IK
	{
		int					_ikID;
		IKData				_ikData;

		public int ikID { get { return _ikID; } }
		public IKData ikData { get { return _ikData; } }

		public class IKLink
		{
			public IKLinkData	ikLinkData;
			public Bone			bone;
		}

		Bone				_destBone;
		Bone				_targetBone;
		IKLink[]			_ikLinkList;

		public Bone destBone { get { return _destBone; } }
		public Bone targetBone { get { return _targetBone; } }
		public IKLink[] ikLinkList { get { return _ikLinkList; } }

		public bool ikEnabled {
			get {
				if(_destBone != null ) {
					return _destBone.ikEnabled;
				}
				return false;
			}
			set {
				if( _destBone != null ) {
					_destBone.ikEnabled = value;
				}
			}
		}

		public float ikWeight {
			get {
				if(_destBone != null ) {
					return _destBone.ikWeight;
				}
				return 0.0f;
			}
			set {
				if( _destBone != null ) {
					_destBone.ikWeight = value;
				}
			}
		}

		public IK( MMD4MecanimModel model, int ikID )
		{
			if( model == null || model.modelData == null || model.modelData.ikDataList == null ||
			    ikID >= model.modelData.ikDataList.Length ) {
				Debug.LogError("");
				return;
			}
			
			_ikID	= ikID;
			_ikData	= model.modelData.ikDataList[ikID];

			if( _ikData != null ) {
				_destBone = model.GetBone( _ikData.destBoneID );
				_targetBone = model.GetBone( _ikData.targetBoneID );
				if( _ikData.ikLinkDataList != null ) {
					_ikLinkList = new IKLink[_ikData.ikLinkDataList.Length];
					for( int i = 0; i < _ikData.ikLinkDataList.Length; ++i ) {
						_ikLinkList[i] = new IKLink();
						_ikLinkList[i].ikLinkData = _ikData.ikLinkDataList[i];
					}
				}
			}
		}

		public void Destroy()
		{
			_ikData = null;
			_destBone = null;
			_targetBone = null;
			_ikLinkList = null;
		}
	}
}
