using UnityEngine;
using System.Collections;

public class MMD4MecanimRigidBody : MonoBehaviour
{	
	public MMD4MecanimInternal.Bullet.RigidBodyProperty bulletPhysicsRigidBodyProperty;
	private MMD4MecanimBulletPhysics.RigidBody _bulletPhysicsRigidBody;
	
	void Start()
	{
		MMD4MecanimBulletPhysics instance = MMD4MecanimBulletPhysics.instance;
		if( instance != null ) {
			_bulletPhysicsRigidBody = instance.CreateRigidBody( this );
		}
	}
	
	void OnDestroy()
	{
		if( _bulletPhysicsRigidBody != null && !_bulletPhysicsRigidBody.isExpired ) {
			MMD4MecanimBulletPhysics instance = MMD4MecanimBulletPhysics.instance;
			if( instance != null ) {
				instance.DestroyRigidBody( _bulletPhysicsRigidBody );
			}
		}
		_bulletPhysicsRigidBody = null;
	}
}
