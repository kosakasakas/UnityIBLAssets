using UnityEngine;
using System.Collections;

public partial class MMD4MecanimModel
{
	void _InitializeRigidBody()
	{
		if( _modelData == null ) {
			this.rigidBodyList = null;
			return;
		}

		if( _modelData.rigidBodyDataList == null ) {
			this.rigidBodyList = null;
			return;
		}

		if( this.rigidBodyList != null ) {
			for( int i = 0; i < this.rigidBodyList.Length; ++i ) {
				if( this.rigidBodyList[i] == null || this.rigidBodyList[i].rigidBodyData == null ) {
					this.rigidBodyList = null;
					break;
				}
			}
		}

		if( this.rigidBodyList == null || this.rigidBodyList.Length != _modelData.rigidBodyDataList.Length ) {
			this.rigidBodyList = new RigidBody[_modelData.rigidBodyDataList.Length];
			for( int i = 0; i < this.rigidBodyList.Length; ++i ) {
				this.rigidBodyList[i] = new RigidBody();
				this.rigidBodyList[i].rigidBodyData = _modelData.rigidBodyDataList[i];
				this.rigidBodyList[i].freezed = this.rigidBodyList[i].rigidBodyData.isFreezed;
			}
		}
	}

	void _InitializePhysicsEngine()
	{
		if( this.modelFile == null ) {
			Debug.LogWarning( this.gameObject.name + ":modelFile is nothing." );
			return;
		}
		
		if( this.physicsEngine == PhysicsEngine.None ||
		    this.physicsEngine == PhysicsEngine.BulletPhysics ) {
			MMD4MecanimBulletPhysics instance = MMD4MecanimBulletPhysics.instance;
			if( instance != null ) {
				_bulletPhysicsMMDModel = instance.CreateMMDModel( this );
			}
		}
	}

	public void SetGravity( float gravityScale, float gravityNoise, Vector3 gravityDirection )
	{
		// for local world only.
		if( this.bulletPhysics != null && this.bulletPhysics.worldProperty != null ) {
			this.bulletPhysics.worldProperty.gravityScale = gravityScale;
			this.bulletPhysics.worldProperty.gravityNoise = gravityNoise;
			this.bulletPhysics.worldProperty.gravityDirection = gravityDirection;
		}
	}
}
