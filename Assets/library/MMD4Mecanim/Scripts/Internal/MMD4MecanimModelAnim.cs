using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using MorphCategory				= MMD4MecanimData.MorphCategory;
using MorphType					= MMD4MecanimData.MorphType;
using MorphAutoLumninousType	= MMD4MecanimData.MorphAutoLumninousType;
using MorphData					= MMD4MecanimData.MorphData;
using MorphMotionData			= MMD4MecanimData.MorphMotionData;
using BoneData					= MMD4MecanimData.BoneData;
using RigidBodyData				= MMD4MecanimData.RigidBodyData;

using Bone						= MMD4MecanimBone;

using IAnim						= MMD4MecanimAnim.IAnim;
using IMorph					= MMD4MecanimAnim.IMorph;
using IAnimModel				= MMD4MecanimAnim.IAnimModel;

public partial class MMD4MecanimModel
{
	Animator				_animator;
	MMD4MecanimModel.Anim	_playingAnim;
	float					_prevDeltaTime;
	float[]					_animMorphCategoryWeights;

	//------------------------------------------------------------------------------------------------------------------------------------------------

	IMorph IAnimModel.GetMorph( string name )
	{
		return this.GetMorph( name );
	}

	IMorph IAnimModel.GetMorph( string name, bool startsWith )
	{
		return this.GetMorph( name, startsWith );
	}

	int IAnimModel.morphCount
	{
		get
		{
			if( this.morphList != null ) {
				return this.morphList.Length;
			}

			return 0;
		}
	}

	IMorph IAnimModel.GetMorphAt( int index )
	{
		if( this.morphList != null ) {
			if( index >= 0 && index < this.morphList.Length ) {
				return this.morphList[index];
			}
		}

		return null;
	}

	int IAnimModel.animCount
	{
		get
		{
			if( this.animList != null ) {
				return this.animList.Length;
			}

			return 0;
		}
	}

	IAnim IAnimModel.GetAnimAt( int index )
	{
		if( this.animList != null ) {
			if( index >= 0 && index < this.animList.Length ) {
				return this.animList[index];
			}
		}

		return null;
	}
	
	Animator IAnimModel.animator
	{
		get { return _animator; }
	}

	AudioSource IAnimModel.audioSource
	{
		get
		{
			if( this.audioSource == null ) {
				this.audioSource = MMD4MecanimCommon.WeakAddComponent< AudioSource >( this.gameObject );
			}

			return this.audioSource;
		}
	}

	bool IAnimModel.animSyncToAudio
	{
		get { return this.animSyncToAudio; }
	}

	float IAnimModel.prevDeltaTime
	{
		get { return _prevDeltaTime; }
		set { _prevDeltaTime = value; }
	}

	IAnim IAnimModel.playingAnim
	{
		get { return _playingAnim; }
		set { _playingAnim = (Anim)value; }
	}
	
	void IAnimModel._SetAnimMorphWeight( IMorph morph, float weight )
	{
		((Morph)morph)._animWeight = weight;
	}

	//------------------------------------------------------------------------------------------------------------------------------------------------

	void _InitializeAnimatoion()
	{
		_animator = this.GetComponent< Animator >();

		_animMorphCategoryWeights = new float[(int)MorphCategory.Max];

		if( !Application.isPlaying ) {
			return; // for Editor
		}

		MMD4MecanimAnim.InitializeAnimModel( this );
	}
	
	void _UpdateAnim()
	{
		if( !Application.isPlaying ) {
			return; // for Editor
		}

		MMD4MecanimAnim.PreUpdateAnimModel( this );

		if( !this.animEnabled ) {
			MMD4MecanimAnim.StopAnimModel( this );
		} else {
			MMD4MecanimAnim.UpdateAnimModel( this );
		}

		MMD4MecanimAnim.PostUpdateAnimModel( this );
	}
}
