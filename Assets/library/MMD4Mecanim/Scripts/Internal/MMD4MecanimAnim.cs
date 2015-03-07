using UnityEngine;
using System.Collections;
using MMD4Mecanim;

using AnimData = MMD4MecanimData.AnimData;
using MorphMotionData = MMD4MecanimData.MorphMotionData;

public static class MMD4MecanimAnim
{
	[System.Serializable]
	public struct MorphMotion
	{
		public IMorph			morph;
		public int				lastKeyFrameIndex;
	}
	
	public interface IAnim
	{
		string					animatorStateName { get; set; }
		int						animatorStateNameHash { get; set; }
		TextAsset				animFile { get; set; }
		AudioClip				audioClip { get; set; }
		
		AnimData				animData { get; set; }
		
		MorphMotion[]			morphMotionList { get; set; }
	}
	
	[System.Serializable]
	public class Anim : IAnim
	{
		public string			animatorStateName;
		[System.NonSerialized]
		public int				animatorStateNameHash;
		public TextAsset		animFile;
		public AudioClip		audioClip;

		[System.NonSerialized]
		public AnimData			animData;
		[System.NonSerialized]
		public MorphMotion[]	morphMotionList;
		
		string IAnim.animatorStateName
		{
			get { return this.animatorStateName; }
			set { this.animatorStateName = value; }
		}
		
		int IAnim.animatorStateNameHash
		{
			get { return this.animatorStateNameHash; }
			set { this.animatorStateNameHash = value; }
		}
		
		TextAsset IAnim.animFile
		{
			get { return this.animFile; }
			set { this.animFile = value; }
		}
		
		AudioClip IAnim.audioClip
		{
			get { return this.audioClip; }
			set { this.audioClip = value; }
		}
		
		AnimData IAnim.animData
		{
			get { return this.animData; }
			set { this.animData = value; }
		}
		
		MorphMotion[] IAnim.morphMotionList
		{
			get { return this.morphMotionList; }
			set { this.morphMotionList = value; }
		}
	}
	
	public interface IMorph
	{
		string name { get; }
		float weight { get; set; }
	}
	
	public interface IAnimModel
	{
		IMorph GetMorph( string name );
		IMorph GetMorph( string name, bool startsWith );

		int morphCount { get; }
		IMorph GetMorphAt( int index );

		int animCount { get; }
		IAnim GetAnimAt( int index );
		
		Animator animator { get; }
		AudioSource audioSource { get; }
		bool animSyncToAudio { get; }
		float prevDeltaTime { get; set; }
		IAnim playingAnim { get; set; }

		void _SetAnimMorphWeight( IMorph morph, float weight );
	}

	public static void InitializeAnimModel( IAnimModel animModel )
	{
		if( animModel == null ) {
			return;
		}
		
		int animCount = animModel.animCount;
		for( int i = 0; i < animCount; ++i ) {
			IAnim anim = animModel.GetAnimAt( i );
			if( anim == null ) {
				continue;
			}
			
			if( anim.animatorStateName != null ) {
				anim.animatorStateNameHash = Animator.StringToHash(anim.animatorStateName);
			}
			
			if( anim.animData == null ) {
				anim.animData = MMD4MecanimData.BuildAnimData( anim.animFile );
				if( anim.animData == null ) {
					continue;
				}
			}
			
			MorphMotionData[] morphMotionDataList = anim.animData.morphMotionDataList;
			if( morphMotionDataList != null ) {
				anim.morphMotionList = new MorphMotion[morphMotionDataList.Length];
				
				if( anim.animData.supportNameIsFull ) {
					for( int n = 0; n != morphMotionDataList.Length; ++n ) {
						anim.morphMotionList[n].morph = animModel.GetMorph(
							morphMotionDataList[n].name, morphMotionDataList[n].nameIsFull );
					}
				} else { // Legacy supports.
					for( int n = 0; n != morphMotionDataList.Length; ++n ) {
						anim.morphMotionList[n].morph = animModel.GetMorph( morphMotionDataList[n].name, false );
					}
					for( int n = 0; n != morphMotionDataList.Length; ++n ) {
						if( anim.morphMotionList[n].morph == null ) {
							IMorph morph = animModel.GetMorph( morphMotionDataList[n].name, true );
							if( morph != null ) {
								bool findAnything = false;
								for( int m = 0; m != morphMotionDataList.Length && !findAnything; ++m ) {
									findAnything = (anim.morphMotionList[m].morph == morph);
								}
								if( !findAnything ) {
									anim.morphMotionList[n].morph = morph;
								}
							}
						}
					}
				}
			}
		}
	}

	// Prefix prevUpdateTime
	public static void PreUpdateAnimModel( IAnimModel animModel )
	{
		if( animModel == null ) {
			return;
		}

		if( animModel.prevDeltaTime == 0.0f ) {
			animModel.prevDeltaTime = Time.deltaTime;
		}
	}

	// Postfix prevUpdateTime
	public static void PostUpdateAnimModel( IAnimModel animModel )
	{
		if( animModel == null ) {
			return;
		}

		animModel.prevDeltaTime = Time.deltaTime;
	}

	public static void UpdateAnimModel( IAnimModel animModel )
	{
		if( animModel == null ) {
			return;
		}

		Animator animator = animModel.animator;
		if( animator != null && animator.enabled ) {
			AnimatorStateInfo animatorStateInfo = animator.GetCurrentAnimatorStateInfo(0);
			int nameHash = animatorStateInfo.nameHash;
			float animationTime = animatorStateInfo.normalizedTime * animatorStateInfo.length;
			int animCount = animModel.animCount;
			for( int i = 0; i < animCount; ++i ) {
				IAnim anim = animModel.GetAnimAt( i );
				if( anim == null ) {
					continue;
				}
				if( anim.animatorStateNameHash == nameHash ) {
					UpdateAnimModel( animModel, anim, animationTime );
					return;
				}
			}
			
			StopAnimModel( animModel );
		} else {
			StopAnimModel( animModel );
		}
	}

	public static void UpdateAnimModel( IAnimModel animModel, IAnim anim, float animationTime )
	{
		if( animModel == null ) {
			return;
		}

		if( anim == null ) {
			StopAnimModel( animModel );
			return;
		}

		float f_frameNo = animationTime * 30.0f;
		int frameNo = (int)f_frameNo;
		
		if( animModel.playingAnim != anim ) {
			StopAnimModel( animModel );
		}

		bool isPlayAudioSourceAtFirst = false;// Play audioSource at first.
		if( animModel.playingAnim == null && anim.audioClip != null ) {
			AudioSource audioSource = animModel.audioSource;
			if( audioSource != null ) {
				// Memo: Ready for multiple playing in unique audioSource.
				if( audioSource.clip != anim.audioClip ) {
					audioSource.clip = anim.audioClip;
					audioSource.Play();
					isPlayAudioSourceAtFirst = true;
				} else {
					if( !audioSource.isPlaying ) {
						audioSource.Play();
						isPlayAudioSourceAtFirst = true;
					}
				}
			}
		}
		
		animModel.playingAnim = anim;

		_SyncToAudio( animModel, animationTime, isPlayAudioSourceAtFirst );

		MorphMotion[] morphMotionList = anim.morphMotionList;
		AnimData animData = anim.animData;
		if( morphMotionList != null && animData != null && animData.morphMotionDataList != null ) {
			for( int i = 0; i != morphMotionList.Length; ++i ) {
				MorphMotionData morphMotionData = animData.morphMotionDataList[i];
				if( morphMotionList[i].morph == null ) {
					continue;
				}

				if( morphMotionData.frameNos == null ||
					morphMotionData.f_frameNos == null ||
					morphMotionData.weights == null ) {
					continue;
				}

				if( morphMotionList[i].lastKeyFrameIndex < morphMotionData.frameNos.Length &&
				    morphMotionData.frameNos[morphMotionList[i].lastKeyFrameIndex] > frameNo ) {
					morphMotionList[i].lastKeyFrameIndex = 0;
				}

				bool isProcessed = false;
				for( int keyFrameIndex = morphMotionList[i].lastKeyFrameIndex;
				    keyFrameIndex != morphMotionData.frameNos.Length; ++keyFrameIndex ) {
					int keyFrameNo = morphMotionData.frameNos[keyFrameIndex];
					if( frameNo >= keyFrameNo ) {
						morphMotionList[i].lastKeyFrameIndex = keyFrameIndex;
					} else {
						if( morphMotionList[i].lastKeyFrameIndex + 1 < morphMotionData.frameNos.Length ) {
							_ProcessKeyFrame2( animModel,
												morphMotionList[i].morph, morphMotionData,
												morphMotionList[i].lastKeyFrameIndex + 0,
												morphMotionList[i].lastKeyFrameIndex + 1,
												frameNo, f_frameNo );
							isProcessed = true;
						}
						break;
					}
				}
				if( !isProcessed ) {
					if( morphMotionList[i].lastKeyFrameIndex < morphMotionData.frameNos.Length ) {
						_ProcessKeyFrame( animModel, morphMotionList[i].morph, morphMotionData, morphMotionList[i].lastKeyFrameIndex );
					}
				}
			}
		}
	}

	static void _ProcessKeyFrame2(
		IAnimModel animModel,
		IMorph morph,
		MorphMotionData motionMorphData,
		int keyFrameIndex0,
		int keyFrameIndex1,
		int frameNo, float f_frameNo )
	{
		int frameNo0 = motionMorphData.frameNos[keyFrameIndex0];
		int frameNo1 = motionMorphData.frameNos[keyFrameIndex1];
		float f_frameNo0 = motionMorphData.f_frameNos[keyFrameIndex0];
		float f_frameNo1 = motionMorphData.f_frameNos[keyFrameIndex1];
		if( frameNo <= frameNo0 || frameNo1 - frameNo0 == 1 ) { /* memo: Don't interpolate adjacent keyframes. */
			animModel._SetAnimMorphWeight( morph, motionMorphData.weights[keyFrameIndex0] );
		} else if( frameNo >= frameNo1 ) {
			animModel._SetAnimMorphWeight( morph, motionMorphData.weights[keyFrameIndex1] );
		} else {
			float r1 = (f_frameNo - f_frameNo0) / (f_frameNo1 - f_frameNo0);
			float w0 = motionMorphData.weights[keyFrameIndex0];
			float w1 = motionMorphData.weights[keyFrameIndex1];
			animModel._SetAnimMorphWeight( morph, w0 + (w1 - w0) * r1 );
		}
	}
	
	static void _ProcessKeyFrame(
		IAnimModel animModel,
		IMorph morph,
		MorphMotionData motionMorphData,
		int keyFrameIndex )
	{
		animModel._SetAnimMorphWeight( morph, motionMorphData.weights[keyFrameIndex] );
	}

	public static void StopAnimModel( IAnimModel animModel )
	{
		if( animModel == null ) {
			return;
		}

		IAnim playingAnim = animModel.playingAnim;
		if( playingAnim == null ) {
			return;
		}
		
		if( playingAnim.audioClip != null ) {
			AudioSource audioSource = animModel.audioSource;
			if( audioSource != null && audioSource.clip == playingAnim.audioClip ) {
				audioSource.Stop();
				audioSource.clip = null;
				
				if( animModel.animSyncToAudio ) {
					Animator animator = animModel.animator;
					if( animator != null ) {
						animator.speed = 1.0f;
					}
				}
			}
		}
		
		animModel.playingAnim = null;
	}

	static void _SyncToAudio( IAnimModel animModel, float animationTime, bool isPlayAudioSourceAtFirst )
	{
		M4MDebug.Assert( animModel != null );
		if( !animModel.animSyncToAudio )  {
			return;
		}

		IAnim playingAnim = animModel.playingAnim;
		if( playingAnim == null || playingAnim.audioClip == null ) {
			return;
		}

		AudioSource audioSource = animModel.audioSource;
		if( audioSource == null ) {
			return;
		}

		Animator animator = animModel.animator;
		if( animator != null && animator.enabled ) {
			float prevDeltaTime = animModel.prevDeltaTime;
			if( audioSource.isPlaying ) {
				float audioTime = audioSource.time;
				if( audioTime == 0.0f ) { // Support for delayed.
					animator.speed = 0.0f;
				} else {
					float deltaTime = (prevDeltaTime + Time.deltaTime) * 0.5f;
					float diffTime = audioTime - animationTime;
					if( Mathf.Abs( diffTime ) <= deltaTime ) {
						animator.speed = 1.0f;
						//Debug.Log( "Safe" );
					} else {
						if( !isPlayAudioSourceAtFirst || ( deltaTime > Mathf.Epsilon && deltaTime < 0.1f ) ) {
							float targetSpeed = 1.0f + diffTime / deltaTime;
							targetSpeed = Mathf.Clamp( targetSpeed, 0.5f, 2.0f );
							if( animator.speed == 0.0f ) {
								animator.speed = targetSpeed;
							} else {
								animator.speed = animator.speed * 0.95f + targetSpeed * 0.05f;
							}
						} else {
							//Debug.Log("Force synchronized.");
							audioSource.time = animationTime; // Force synchronize.(Don't call isPlayAudioSourceAtFirst == false)
							animator.speed = 1.0f;
						}
						//Debug.Log( "Unsafe:" + diffTime + ":" + deltaTime + ":" + (diffTime / deltaTime) + ":" + _animator.speed );
					}
				}
			} else {
				animator.speed = 1.0f;
			}
		}
	}
}
