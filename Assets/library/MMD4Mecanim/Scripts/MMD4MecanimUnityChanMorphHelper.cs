using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using Morph				= UnityChanMorph.Morph;
using MorphCategory		= MMD4MecanimData.MorphCategory;
using MorphType			= MMD4MecanimData.MorphType;
using MorphData			= MMD4MecanimData.MorphData;
using MorphMotionData	= MMD4MecanimData.MorphMotionData;

using AnimData			= MMD4MecanimData.AnimData;
using IAnim				= MMD4MecanimAnim.IAnim;
using IMorph			= MMD4MecanimAnim.IMorph;
using IAnimModel		= MMD4MecanimAnim.IAnimModel;
using MorphMotion		= MMD4MecanimAnim.MorphMotion;

public class MMD4MecanimUnityChanMorphHelper : MonoBehaviour, IAnimModel
{
	public bool								playManually = false;
	public string							manualAnimatorStateName = "";
	public string							playingAnimatorStateName = "";

	public bool								animEnabled = true;
	public bool								animPauseOnEnd = false;
	public bool								initializeOnAwake = false;
	public bool								animSyncToAudio = true;
	public float							morphSpeed = 0.1f;

	[System.Serializable]
	public class Anim : MMD4MecanimAnim.Anim
	{
	}

	public Anim[]							animList;

	private bool							_initialized;
	private UnityChanMorph					_unityChanMorph;
	private Animator						_animator;
	private AudioSource						_audioSource;
	private Anim							_playingAnim;
	private float							_manualAnimTime;
	private float							_morphWeight;
	private float							_prevDeltaTime;
	private HashSet<Morph>					_inactiveModelMorphSet = new HashSet<Morph>();

	public void PlayManually( string animatorStateName )
	{
		this.playManually = true;
		this.manualAnimatorStateName = animatorStateName;
	}
	
	public void StopManually()
	{
		this.playManually = false;
		this.playingAnimatorStateName = "";
	}

	void Awake()
	{
		if( this.initializeOnAwake ) {
			_Initialize();
		}
	}

	void Start()
	{
		_Initialize();
	}

	void Update()
	{
		MMD4MecanimAnim.PreUpdateAnimModel( this );

		_UpdateAnim();
		_UpdateMorph();

		MMD4MecanimAnim.PostUpdateAnimModel( this );
	}

	void _Initialize()
	{
		if( _initialized ) {
			return;
		}

		_initialized = true;
		_animator = this.gameObject.GetComponent< Animator >();
		_unityChanMorph = this.gameObject.GetComponent< UnityChanMorph >();
		if( _unityChanMorph == null ) {
			return;
		}

		MMD4MecanimAnim.InitializeAnimModel( this );
	}

	void _UpdateAnim()
	{
		if( !this.animEnabled ) {
			_StopAnim();
			return;
		}

		if( this.playManually ) {
			if( _playingAnim != null ) {
				if( string.IsNullOrEmpty(this.playingAnimatorStateName) ||
					_playingAnim.animatorStateName == null ||
				    _playingAnim.animatorStateName != this.playingAnimatorStateName ) {
					_StopAnim();
				}
			}

			bool updatedAnimModel = false;
			if( _playingAnim == null && !string.IsNullOrEmpty(this.manualAnimatorStateName) ) {
				if( this.animList != null ) {
					for( int i = 0; i < this.animList.Length; ++i ) {
						if( this.animList[i].animatorStateName != null &&
							this.animList[i].animatorStateName == this.manualAnimatorStateName ) {
							_PlayAnim( this.animList[i] );
							updatedAnimModel = true;
							break;
						}
					}
				}
			}

			if( _playingAnim != null ) {
				if( _playingAnim.animData != null ) {
					if( !this.animPauseOnEnd ) {
						if( _manualAnimTime == (float)(_playingAnim.animData.maxFrame / 30.0f) ) {
							_StopAnim();
						}
					}
				} else {
					_StopAnim();
				}
			}

			if( _playingAnim == null ) {
				return;
			}

			if( !updatedAnimModel ) {
				MMD4MecanimAnim.UpdateAnimModel( this, _playingAnim, _manualAnimTime );
			}

			// Sync with Manually.
			if( _playingAnim.audioClip != null && _audioSource != null && _audioSource.isPlaying && this.animSyncToAudio ) {
				_manualAnimTime = _audioSource.time;
			} else {
				_manualAnimTime += Time.deltaTime;
			}
			
			if( _playingAnim.animData != null ) {
				_manualAnimTime = Mathf.Min( _manualAnimTime, (float)(_playingAnim.animData.maxFrame / 30.0f) );
			} else {
				_manualAnimTime = 0.0f;
			}
		} else {
			MMD4MecanimAnim.UpdateAnimModel( this );
		}
	}

	void _PlayAnim( Anim anim )
	{
		_StopAnim(); // _playingAnim = null;

		_manualAnimTime = 0.0f;
		this.manualAnimatorStateName = "";
		if( anim != null ) {
			this.playingAnimatorStateName = anim.animatorStateName;
		}

		MMD4MecanimAnim.UpdateAnimModel( this, anim, _manualAnimTime ); // _playingAnim = anim;

		if( _playingAnim != null && _inactiveModelMorphSet != null ) {
			if( _playingAnim.morphMotionList != null ) {
				for( int i = 0; i < _playingAnim.morphMotionList.Length; ++i ) {
					_playingAnim.morphMotionList[i].lastKeyFrameIndex = 0;
					Morph morph = (Morph)_playingAnim.morphMotionList[i].morph;
					if( morph != null ) {
						_inactiveModelMorphSet.Remove( morph );
					}
				}
			}
		}
	}

	void _StopAnim()
	{
		if( _playingAnim != null && _inactiveModelMorphSet != null ) {
			if( _playingAnim.morphMotionList != null ) {
				for( int i = 0; i != _playingAnim.morphMotionList.Length; ++i ) {
					_playingAnim.morphMotionList[i].lastKeyFrameIndex = 0;
					Morph morph = (Morph)_playingAnim.morphMotionList[i].morph;
					if( morph != null && morph.weight != 0.0f ) {
						_inactiveModelMorphSet.Add( morph );
					}
				}
			}
		}

		MMD4MecanimAnim.StopAnimModel( this ); // _playingAnim = null;
		_manualAnimTime = 0.0f;
		this.playingAnimatorStateName = "";
	}

	void _UpdateMorph()
	{
		float stepValue = 1.0f;
		if( this.morphSpeed > 0.0f ) {
			stepValue = Time.deltaTime / this.morphSpeed;
		}

		if( _playingAnim != null ) {
			MMD4MecanimCommon.Approx( ref _morphWeight, 1.0f, stepValue );
		} else {
			MMD4MecanimCommon.Approx( ref _morphWeight, 0.0f, stepValue );
		}
		if( _inactiveModelMorphSet != null ) {
			foreach( var morph in _inactiveModelMorphSet ) {
				MMD4MecanimCommon.Approx( ref morph.weight, 0.0f, stepValue );
			}
			_inactiveModelMorphSet.RemoveWhere( s => s.weight == 0.0f );
		}
	}

	//------------------------------------------------------------------------------------------------------------------------------------------------
	
	IMorph IAnimModel.GetMorph( string name )
	{
		if( _unityChanMorph != null ) {
			return _unityChanMorph.GetMorph( name );
		}
		return null;
	}
	
	IMorph IAnimModel.GetMorph( string name, bool startsWith )
	{
		if( _unityChanMorph != null ) {
			return _unityChanMorph.GetMorph( name, startsWith );
		}
		return null;
	}
	
	int IAnimModel.morphCount
	{
		get
		{
			if( _unityChanMorph != null && _unityChanMorph.morphs != null ) {
				return _unityChanMorph.morphs.Length;
			}

			return 0;
		}
	}
	
	IMorph IAnimModel.GetMorphAt( int index )
	{
		if( _unityChanMorph != null && _unityChanMorph.morphs != null ) {
			if( index >= 0 && index < _unityChanMorph.morphs.Length ) {
				return _unityChanMorph.morphs[index];
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
			if( _audioSource == null ) {
				_audioSource = MMD4MecanimCommon.WeakAddComponent< AudioSource >( this.gameObject );
			}
			
			return _audioSource;
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
		morph.weight = weight;
	}
	
	//------------------------------------------------------------------------------------------------------------------------------------------------
}
