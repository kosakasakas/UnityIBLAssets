//#define NOUSE_BULLETXNA_UNITY
#if MMD4MECANIM_DEBUG
//#define FORCE_BULLETXNA_UNITY
//#define FORCE_BULLETXNA_INITIALIZEENGINE // timeBeginPeriod / timeEndPeriod
//#define DEBUG_REMOVE_GLOBALWORLD
#endif

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using PropertyWriter = MMD4MecanimCommon.PropertyWriter;
using BinaryReader = MMD4MecanimCommon.BinaryReader;
using ShapeType = MMD4MecanimData.ShapeType;
using BoneData = MMD4MecanimData.BoneData;
using RigidBodyData = MMD4MecanimData.RigidBodyData;
using RigidBodyType = MMD4MecanimData.RigidBodyType;
using IKData = MMD4MecanimData.IKData;
using MeshFlags = MMD4MecanimData.MeshFlags;

#if !NOUSE_BULLETXNA_UNITY
using ThreadQueueHandle = MMD4MecanimInternal.Bullet.ThreadQueueHandle;
#endif

using WorldProperty = MMD4MecanimInternal.Bullet.WorldProperty;
using WorldUpdateProperty = MMD4MecanimInternal.Bullet.WorldUpdateProperty;
using MMDModelProperty = MMD4MecanimInternal.Bullet.MMDModelProperty;
using MMDRigidBodyProperty = MMD4MecanimInternal.Bullet.MMDRigidBodyProperty;
using RigidBodyProperty = MMD4MecanimInternal.Bullet.RigidBodyProperty;

public class MMD4MecanimBulletPhysics : MonoBehaviour
{
	public static readonly Matrix4x4 rotateMatrixX			= Matrix4x4.TRS( Vector3.zero, Quaternion.Euler( 0.0f, 0.0f, +90.0f ), Vector3.one );
	public static readonly Matrix4x4 rotateMatrixXInv		= Matrix4x4.TRS( Vector3.zero, Quaternion.Euler( 0.0f, 0.0f, -90.0f ), Vector3.one );
	public static readonly Matrix4x4 rotateMatrixZ			= Matrix4x4.TRS( Vector3.zero, Quaternion.Euler( +90.0f, 0.0f, 0.0f ), Vector3.one );
	public static readonly Matrix4x4 rotateMatrixZInv		= Matrix4x4.TRS( Vector3.zero, Quaternion.Euler( -90.0f, 0.0f, 0.0f ), Vector3.one );

	public static readonly Quaternion rotateQuaternionX		= Quaternion.Euler( 0.0f, 0.0f, 90.0f );
	public static readonly Quaternion rotateQuaternionZ		= Quaternion.Euler( 90.0f, 0.0f, 0.0f );
	public static readonly Quaternion rotateQuaternionXInv	= Quaternion.Euler( 0.0f, 0.0f, -90.0f );
	public static readonly Quaternion rotateQuaternionZInv	= Quaternion.Euler( -90.0f, 0.0f, 0.0f );

	public WorldProperty					globalWorldProperty;
	
	private List< MMDModel >				_mmdModelList = new List<MMDModel>();
	private List< RigidBody >				_rigidBodyList = new List<RigidBody>();
	private bool							_isAwaked;
	private World							_globalWorld;

	public World globalWorld {
		get {
			_ActivateGlobalWorld();
			return _globalWorld;
		}
	}

	#if !NOUSE_BULLETXNA_UNITY
	public class MMD4MecanimBulletBridge : MMD4MecanimInternal.Bullet.IBridge
	{
		// Interface for CachedThreadQueue.
		object MMD4MecanimInternal.Bullet.IBridge.CreateCachedThreadQueue( int maxThreads )
		{
			return new CachedThreadQueue( maxThreads );
		}

		MMD4MecanimInternal.Bullet.ThreadQueueHandle MMD4MecanimInternal.Bullet.IBridge.InvokeCachedThreadQueue(
			object cachedThreadQueue, System.Action action )
		{
			if( cachedThreadQueue != null ) {
				return ((CachedThreadQueue)cachedThreadQueue).Invoke( action );
			} else {
				return new MMD4MecanimInternal.Bullet.ThreadQueueHandle();
			}
		}

		void MMD4MecanimInternal.Bullet.IBridge.WaitEndCachedThreadQueue(
			object cachedThreadQueue, ref MMD4MecanimInternal.Bullet.ThreadQueueHandle threadQueueHandle )
		{
			if( cachedThreadQueue != null ) {
				((CachedThreadQueue)cachedThreadQueue).WaitEnd( ref threadQueueHandle );
			}
		}
		
		// Interface for CachedPararellThreadQueue.
		object MMD4MecanimInternal.Bullet.IBridge.CreatePararellCachedThreadQueue( int maxThreads )
		{
			return new CachedPararellThreadQueue( maxThreads );
		}

		MMD4MecanimInternal.Bullet.ThreadQueueHandle MMD4MecanimInternal.Bullet.IBridge.InvokeCachedPararellThreadQueue(
			object cachedPararellThreadQueue, MMD4MecanimInternal.Bullet.PararellFunction function, int length )
		{
			if( cachedPararellThreadQueue != null ) {
				return ((CachedPararellThreadQueue)cachedPararellThreadQueue).Invoke( function, length );
			} else {
				return new MMD4MecanimInternal.Bullet.ThreadQueueHandle();
			}
		}

		void MMD4MecanimInternal.Bullet.IBridge.WaitEndCachedPararellThreadQueue(
			object cachedPararellThreadQueue, ref MMD4MecanimInternal.Bullet.ThreadQueueHandle threadQueueHandle )
		{
			if( cachedPararellThreadQueue != null ) {
				((CachedPararellThreadQueue)cachedPararellThreadQueue).WaitEnd( ref threadQueueHandle );
			}
		}

		public static void InstantSleep()
		{
			// Memo: Don't use 1 ms. Unstable sleep time in Windows.
			System.Threading.Thread.Sleep( 0 );
		}
		
		public static void ShortlySleep()
		{
			System.Threading.Thread.Sleep( 1 );
		}
		
		public static int GetProcessCount()
		{
			return 4;
		}
		
		// Memo: Threadsafe every functions.
		public class CachedThreadQueue
		{
			System.Threading.ManualResetEvent	_invokeEvent = new System.Threading.ManualResetEvent(false);
			ArrayList							_threads = new ArrayList();
			int									_maxThreads = 0;
			bool								_isFinalized = false;
			uint								_uniqueID = 0;
			
			class Queue
			{
				public System.Action function;
				public uint queueID;
				public uint uniqueID;
				public bool processingWaitEnd;
				public System.Threading.ManualResetEvent processedEvent = new System.Threading.ManualResetEvent(true);
				
				public Queue( System.Action function )
				{
					this.function = function;
					this.queueID = 0;
					this.uniqueID = 0;
				}
				
				public void Unuse()
				{
					this.function = null;
					unchecked {
						++this.queueID;
					}
				}
				
				public void Reuse( System.Action function )
				{
					this.function = function;
				}
			}
			
			ArrayList _processingQueues = new ArrayList();
			ArrayList _reservedQueues = new ArrayList();
			ArrayList _unusedQueues = new ArrayList();
			
			static Queue _FindQueue( ArrayList queues, ref ThreadQueueHandle queueHandle )
			{
				if( queues != null ) {
					for( int i = 0; i != queues.Count; ++i ) {
						Queue queue = (Queue)queues[i];
						if( queue == queueHandle.queuePtr && queue.queueID == queueHandle.queueID ) {
							return queue;
						}
					}
				}
				
				return null;
			}
			
			public CachedThreadQueue()
			{
				_maxThreads = GetProcessCount();
			}
			
			public CachedThreadQueue( int maxThreads )
			{
				_maxThreads = maxThreads;
				if( _maxThreads <= 0 ) {
					_maxThreads = Mathf.Max( GetProcessCount(), 1 );
				}
			}
			
			~CachedThreadQueue()
			{
				if( _threads.Count != 0 ) {
					_Finalize();
				}
			}
			
			public void _Finalize()
			{
				bool isFinalized = false;
				lock(this) {
					isFinalized = _isFinalized;
					_isFinalized = true;
					if( !isFinalized ) {
						_invokeEvent.Set();
					}
				}
				
				if( isFinalized ) {
					return; // If finalizing, return function.
				}
				
				for( int i = 0; i != _threads.Count; ++i ) {
					((System.Threading.Thread)_threads[i]).Join();
				}
				
				_threads.Clear();
				
				lock(this) {
					_isFinalized = false;
				}
			}
			
			public ThreadQueueHandle Invoke( System.Action function )
			{
				ThreadQueueHandle r = new ThreadQueueHandle();
				if( function == null ) {
					return r;
				}
				bool isFinalized = false;
				lock(this) {
					isFinalized = _isFinalized;
					if( !isFinalized ) {
						// Extends thread pool automatically.
						int processingSize = _processingQueues.Count;
						if( processingSize == _threads.Count && _threads.Count < _maxThreads ) {
							System.Threading.Thread thread = new System.Threading.Thread( new System.Threading.ThreadStart( _Run ) );
							_threads.Add( thread );
							thread.Start();
						}
						
						Queue queue = null;
						for( int i = _unusedQueues.Count - 1; i >= 0; --i ) {
							queue = (Queue)_unusedQueues[ i ];
							if( !queue.processingWaitEnd ) {
								_unusedQueues.RemoveAt( i );
								queue.Reuse( function );
								break;
							} else {
								queue = null;
							}
						}
						if( queue == null ) {
							queue = new Queue( function );
						}
						queue.uniqueID = _uniqueID;
						unchecked {
							++_uniqueID;
						}
						_reservedQueues.Add( queue );
						r.queuePtr = queue;
						r.queueID = queue.queueID;
						r.uniqueID = queue.uniqueID;
						queue = null;
						_invokeEvent.Set();
					}
				}
				if( isFinalized ) {
					function(); // If finalizing, invoke directly.
				}
				return r;
			}
			
			public void WaitEnd( ref ThreadQueueHandle queueHandle )
			{
				if( queueHandle.queuePtr == null ) {
					return;
				}
				
				Queue queue = null;
				lock(this) {
					queue = _FindQueue( _processingQueues, ref queueHandle );
					if( queue == null ) {
						queue = _FindQueue( _reservedQueues, ref queueHandle );
					}
					if( queue != null ) {
						queue.processingWaitEnd = true; // Denied recycle.
					}
				}
				
				if( queue == null ) {
					queueHandle.Reset();
					return;
				}
				
				for(;;) {
					InstantSleep();
					
					queue.processedEvent.WaitOne();
					
					lock(this) {
						if( queue.queueID != queueHandle.queueID ) {
							queue.processingWaitEnd = false; // Accept recycle.
							queue = null;
						}
					}
					if( queue == null ) {
						queueHandle.Reset();
						break;
					}
				}
			}
			
			void _Run()
			{
				for(;;) {
					Queue queue = null;
					bool isProcessing = false;
					bool isFinalized = false;
					bool isEmpty = false;
					_invokeEvent.WaitOne();
					lock(this) {
						if( _reservedQueues.Count != 0 ) {
							queue = (Queue)_reservedQueues[0];
							_reservedQueues.RemoveAt( 0 );
							_processingQueues.Add( queue );
						}
						isProcessing = (queue != null);
						isFinalized = _isFinalized;
						isEmpty = _processingQueues.Count == 0 && _reservedQueues.Count == 0;
					}
					
					if( queue != null ) {
						if( queue.function != null ) {
							queue.function();
						}
						
						lock(this) {
							queue.Unuse();
							_processingQueues.Remove( queue );
							_unusedQueues.Add( queue );
							queue.processedEvent.Set();
							queue = null;
							isFinalized = _isFinalized;
							isEmpty = _processingQueues.Count == 0 && _reservedQueues.Count == 0;
							if( isEmpty ) {
								_invokeEvent.Reset();
							}
						}
					}
					
					if( isEmpty && isFinalized ) {
						break;
					}
					if( !isProcessing ) {
						InstantSleep();
					}
				}
			}
		}
		
		// Memo: Threadsafe every functions.
		public class CachedPararellThreadQueue
		{
			System.Threading.ManualResetEvent	_invokeEvent = new System.Threading.ManualResetEvent(false);
			System.Threading.Thread[]			_threads = null;
			int									_maxThreads = 0;
			bool								_isFinalized = false;
			uint								_uniqueID = 0;
			
			class Queue
			{
				public MMD4MecanimInternal.Bullet.PararellFunction function;
				public int length;
				public int processingThreads;
				public int processedThreads;
				public uint queueID;
				public uint uniqueID;
				public bool processingWaitEnd;
				public System.Threading.ManualResetEvent processedEvent = new System.Threading.ManualResetEvent(true);
				
				public Queue( MMD4MecanimInternal.Bullet.PararellFunction function, int length )
				{
					this.function = function;
					this.length = length;
					this.processingThreads = 0;
					this.processedThreads = 0;
					this.queueID = 0;
					this.uniqueID = 0;
				}
				
				public void Unuse()
				{
					this.function = null;
					this.length = 0;
					this.processingThreads = 0;
					this.processedThreads = 0;
					unchecked {
						++this.queueID;
					}
				}
				
				public void Reuse( MMD4MecanimInternal.Bullet.PararellFunction function, int length )
				{
					this.function = function;
					this.length = length;
				}
			}
			
			ArrayList _processedQueues = new ArrayList();
			Queue _processingQueue;
			ArrayList _reservedQueues = new ArrayList();
			ArrayList _unusedQueues = new ArrayList();
			
			static bool _IsEqualQueue( Queue queue, ref ThreadQueueHandle queueHandle )
			{
				if( queue != null ) {
					if( queue == queueHandle.queuePtr && queue.queueID == queueHandle.queueID ) {
						return true;
					}
				}
				
				return false;
			}
			
			static Queue _FindQueue( ArrayList queues, ref ThreadQueueHandle queueHandle )
			{
				if( queues != null ) {
					for( int i = 0; i != queues.Count; ++i ) {
						Queue queue = (Queue)queues[i];
						if( queue == queueHandle.queuePtr && queue.queueID == queueHandle.queueID ) {
							return queue;
						}
					}
				}
				
				return null;
			}
			
			void _AwakeThread()
			{
				if( _threads == null ) {
					_threads = new System.Threading.Thread[_maxThreads];
					for( int i = 0; i != _maxThreads; ++i ) {
						System.Threading.Thread thread = new System.Threading.Thread( new System.Threading.ThreadStart( _Run ) );
						_threads[i] = thread;
						thread.Start();
					}
				}
			}
			
			public CachedPararellThreadQueue()
			{
				_maxThreads = GetProcessCount();
			}
			
			public CachedPararellThreadQueue( int maxThreads )
			{
				_maxThreads = maxThreads;
				if( _maxThreads <= 0 ) {
					_maxThreads = Mathf.Max( GetProcessCount(), 1 );
				}
			}
			
			~CachedPararellThreadQueue()
			{
				if( _threads != null ) {
					_Finalize();
				}
			}
			
			public void _Finalize()
			{
				bool isFinalized = false;
				lock(this) {
					isFinalized = _isFinalized;
					_isFinalized = true;
					if( !isFinalized ) {
						_invokeEvent.Set();
					}
				}
				
				if( isFinalized ) {
					return; // If finalizing, return function.
				}
				
				if( _threads != null ) {
					for( int i = 0; i != _threads.Length; ++i ) {
						_threads[i].Join();
					}
					
					_threads = null;
				}
				
				lock(this) {
					_isFinalized = false;
				}
			}
			
			public ThreadQueueHandle Invoke( MMD4MecanimInternal.Bullet.PararellFunction function, int length )
			{
				ThreadQueueHandle r = new ThreadQueueHandle();
				if( function == null ) {
					return r;
				}
				bool isFinalized = false;
				lock(this) {
					isFinalized = _isFinalized;
					if( !isFinalized ) {
						_AwakeThread();
						
						Queue queue = null;
						for( int i = _unusedQueues.Count - 1; i >= 0; --i ) {
							queue = (Queue)_unusedQueues[ i ];
							if( !queue.processingWaitEnd ) {
								_unusedQueues.RemoveAt( i );
								queue.Reuse( function, length );
								break;
							} else {
								queue = null;
							}
						}
						if( queue == null ) {
							queue = new Queue( function, length );
						}
						queue.uniqueID = _uniqueID;
						unchecked {
							++_uniqueID;
						}
						
						_reservedQueues.Add( queue );
						r.queuePtr = queue;
						r.queueID = queue.queueID;
						r.uniqueID = queue.uniqueID;
						queue = null;
						_invokeEvent.Set();
					}
				}
				if( isFinalized ) {
					function( 0, length ); // If finalizing, invoke directly.
				}
				return r;
			}
			
			public void WaitEnd( ref ThreadQueueHandle queueHandle )
			{
				if( queueHandle.queuePtr == null ) {
					return;
				}
				
				Queue queue = null;
				lock(this) {
					queue = _FindQueue( _processedQueues, ref queueHandle );
					if( queue == null ) {
						if( _IsEqualQueue( _processingQueue, ref queueHandle ) ) {
							queue = _processingQueue;
						} else {
							queue = _FindQueue( _reservedQueues, ref queueHandle );
						}
					}
					if( queue != null ) {
						queue.processingWaitEnd = true; // Denied recycle.
					}
				}
				
				if( queue == null ) {
					queueHandle.Reset();
					return;
				}
				
				for(;;) {
					InstantSleep();
					
					queue.processedEvent.WaitOne();
					
					lock(this) {
						if( queue.queueID != queueHandle.queueID ) {
							queue.processingWaitEnd = false; // Accept recycle.
							queue = null;
						}
					}
					if( queue == null ) {
						queueHandle.Reset();
						break;
					}
				}
			}
			
			void _Run()
			{
				for(;;) {
					Queue queue = null;
					int threadIndex = 0;
					bool isProcessing = false;
					bool isFinalized = false;
					bool isEmpty = false;
					_invokeEvent.WaitOne();
					lock(this) {
						if( _processingQueue != null ) {
							queue = _processingQueue;
						} else if( _reservedQueues.Count != 0 ) {
							queue = (Queue)_reservedQueues[0];
							_reservedQueues.RemoveAt( 0 );
							_processingQueue = queue;
						}
						if( queue != null ) {
							threadIndex = queue.processingThreads;
							++(queue.processingThreads);
							if( queue.processingThreads == _maxThreads ) {
								_processingQueue = null;
								_processedQueues.Add( queue );
							}
						}
						isProcessing = (queue != null);
						isFinalized = _isFinalized;
						isEmpty = _processingQueue == null && _reservedQueues.Count == 0;
					}
					
					if( queue != null ) {
						int length = (queue.length + _maxThreads - 1) / _maxThreads;
						int index = threadIndex * length;
						if( index < queue.length ) {
							if( index + length > queue.length ) {
								length = queue.length - index;
							}
							if( queue.function != null ) {
								queue.function( index, length );
							}
						}
						
						lock(this) {
							if( ++(queue.processedThreads) == _maxThreads ) {
								queue.Unuse();
								_processedQueues.Remove( queue );
								_unusedQueues.Add( queue );
								queue.processedEvent.Set();
								queue = null;
								isFinalized = _isFinalized;
								isEmpty = _processingQueue == null && _reservedQueues.Count == 0;
								if( isEmpty ) {
									_invokeEvent.Reset();
								}
							}
						}
					}
					
					if( isEmpty && isFinalized ) {
						break;
					}
					if( !isProcessing ) {
						InstantSleep();
					}
				}
			}
		}
	}
	#endif
	
	static MMD4MecanimBulletPhysics _instance;
	bool _initialized;
	#if !NOUSE_BULLETXNA_UNITY
	static bool _isUseBulletXNA;
	public static bool isUseBulletXNA { get { return _isUseBulletXNA; } }
	#endif

	public static MMD4MecanimBulletPhysics instance
	{
		get {
			if( _instance == null ) {
				_instance = (MMD4MecanimBulletPhysics)MonoBehaviour.FindObjectOfType( typeof(MMD4MecanimBulletPhysics) );
				if( _instance == null ) {
					GameObject gameObject = new GameObject("MMD4MecanimBulletPhysics");
					MMD4MecanimBulletPhysics instance = gameObject.AddComponent<MMD4MecanimBulletPhysics>();
					if( _instance == null ) {
						_instance = instance;
					}
				}
				if( _instance != null ) {
					_instance._Initialize();
				}
			}

			return _instance;
		}
	}

	System.Diagnostics.Process _process;

	private void _Initialize()
	{
		if( _initialized ) {
			return;
		}
		_initialized = true;
		DontDestroyOnLoad( this.gameObject );

		#if !NOUSE_BULLETXNA_UNITY
		if( Application.HasProLicense() ) {
			#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
			_isUseBulletXNA = false;
			#else
			_isUseBulletXNA = true;
			#endif
		} else {
			_isUseBulletXNA = true;
		}
		#if FORCE_BULLETXNA_UNITY
		_isUseBulletXNA = true;
		#endif
		if( _isUseBulletXNA ) {
			if( MMD4MecanimInternal.Bullet.Global.bridge == null ) {
				MMD4MecanimInternal.Bullet.Global.bridge = new MMD4MecanimBulletBridge();
			}

			Debug.Log( "MMD4MecanimBulletPhysics:Awake BulletXNA." );
			#if FORCE_BULLETXNA_INITIALIZEENGINE
			_InitializeEngine();
			#endif
			#if  UNITY_STANDALONE_WIN
			// timeBeginPeriod(1) for Unity Standard License.(Fix mesh.vertices rewrite slowly)
			if( Application.platform == RuntimePlatform.WindowsEditor ||
			    Application.platform == RuntimePlatform.WindowsPlayer ) {
				try {
					System.Diagnostics.Process.Start(
						Application.streamingAssetsPath + "\\MMD4Mecanim\\MMD4MecanimAgent.exe", 
					    System.Diagnostics.Process.GetCurrentProcess().Id.ToString() );
				} catch( System.Exception ) {
				}
			}
			#endif
		} else {
			Debug.Log( "MMD4MecanimBulletPhysics:Awake Native Plugin." );
			// Initialize Engine.
			_InitializeEngine();
		}
		#endif

		// http://docs.unity3d.com/Documentation/Manual/ExecutionOrder.html
		StartCoroutine( DelayedAwake() );
	}
	
	public class World
	{
		public WorldProperty						worldProperty;
		public IntPtr								worldPtr;
		#if !NOUSE_BULLETXNA_UNITY
		public MMD4MecanimInternal.Bullet.PhysicsWorld	bulletPhysicsWorld;
		#endif

		float _gravityScaleCached				= 10.0f;
		float _gravityNoiseCached				= 0.0f;
		Vector3 _gravityDirectionCached			= new Vector3( 0.0f, -1.0f, 0.0f );

		bool _isDirtyProperty = true;
		PropertyWriter _updatePropertyWriter = new PropertyWriter();
		WorldUpdateProperty _worldUpdateProperty = new WorldUpdateProperty();

		~World()
		{
			Destroy();
		}

		public bool isExpired
		{
			get {
				#if !NOUSE_BULLETXNA_UNITY
				if( this.bulletPhysicsWorld != null ) {
					return false;
				}
				#endif
				return this.worldPtr == IntPtr.Zero;
			}
		}

		public bool Create()
		{
			return Create( null );
		}
		
		public bool Create( WorldProperty worldProperty )
		{
			Destroy();

			if( worldProperty != null ) {
				this.worldProperty = worldProperty;
			} else {
				this.worldProperty = new WorldProperty();
			}

			_gravityScaleCached = this.worldProperty.gravityScale;
			_gravityNoiseCached = this.worldProperty.gravityNoise;
			_gravityDirectionCached = this.worldProperty.gravityDirection;

			#if !NOUSE_BULLETXNA_UNITY
			if( _isUseBulletXNA ) {
				this.bulletPhysicsWorld = new MMD4MecanimInternal.Bullet.PhysicsWorld();
				if( !this.bulletPhysicsWorld.Create( this.worldProperty ) ) {
					this.bulletPhysicsWorld.Destroy();
					this.bulletPhysicsWorld = null;
					return false;
				}
				return true;
			}
			#endif
			
			if( this.worldProperty != null ) {
				Vector3 gravityDirection = this.worldProperty.gravityDirection;
				gravityDirection.z = -gravityDirection.z;

				PropertyWriter propertyWriter = new PropertyWriter();
				propertyWriter.Write( "accurateStep",					this.worldProperty.accurateStep );
				propertyWriter.Write( "multiThreading",					this.worldProperty.multiThreading );
				propertyWriter.Write( "framePerSecond",					this.worldProperty.framePerSecond );
				propertyWriter.Write( "resetFrameRate",					this.worldProperty.resetFrameRate );
				propertyWriter.Write( "limitDeltaFrames",				this.worldProperty.limitDeltaFrames );
				propertyWriter.Write( "axisSweepDistance",				this.worldProperty.axisSweepDistance );
				propertyWriter.Write( "gravityScale",					this.worldProperty.gravityScale );
				propertyWriter.Write( "gravityNoise",					this.worldProperty.gravityNoise );
				propertyWriter.Write( "gravityDirection",				gravityDirection );
				propertyWriter.Write( "vertexScale",					this.worldProperty.vertexScale );
				propertyWriter.Write( "importScale",					this.worldProperty.importScale );
				propertyWriter.Write( "worldSolverInfoNumIterations",	this.worldProperty.worldSolverInfoNumIterations );
				propertyWriter.Write( "worldSolverInfoSplitImpulse",	this.worldProperty.worldSolverInfoSplitImpulse );
				propertyWriter.Write( "worldAddFloorPlane",				this.worldProperty.worldAddFloorPlane );

				propertyWriter.Lock();
				this.worldPtr = _CreateWorld(
					propertyWriter.iValuesPtr, propertyWriter.iValueLength,
					propertyWriter.fValuesPtr, propertyWriter.fValueLength );
				propertyWriter.Unlock();
			} else {
				this.worldPtr = _CreateWorld( IntPtr.Zero, 0, IntPtr.Zero, 0 );
			}
			
			DebugLog();

			return ( this.worldPtr != IntPtr.Zero );
		}

		public void SetGravity( float gravityScale, float gravityNoise, Vector3 gravityDirection )
		{
			if( _gravityScaleCached != gravityScale || _gravityNoiseCached != gravityNoise || _gravityDirectionCached != gravityDirection ) {
				_gravityScaleCached = gravityScale;
				_gravityNoiseCached = gravityNoise;
				_gravityDirectionCached = gravityDirection;
				_isDirtyProperty = true;
				this.worldProperty.gravityScale = gravityScale;
				this.worldProperty.gravityNoise = gravityNoise;
				this.worldProperty.gravityDirection = gravityDirection;
			}
		}

		public void Destroy()
		{
			#if !NOUSE_BULLETXNA_UNITY
			if( this.bulletPhysicsWorld != null ) {
				this.bulletPhysicsWorld.Destroy();
				this.bulletPhysicsWorld = null;
			}
			#endif
			if( this.worldPtr != IntPtr.Zero ) {
				IntPtr worldPtr = this.worldPtr;
				this.worldPtr = IntPtr.Zero;
				_DestroyWorld( worldPtr );
			}
			this.worldProperty = null;
		}
		
		public void Update( float deltaTime )
		{
			if( this.worldProperty.gravityScale != _gravityScaleCached ||
				this.worldProperty.gravityNoise != _gravityNoiseCached ||
				this.worldProperty.gravityDirection != _gravityDirectionCached ) {
				_gravityScaleCached = this.worldProperty.gravityScale;
				_gravityNoiseCached = this.worldProperty.gravityNoise;
				_gravityDirectionCached = this.worldProperty.gravityDirection;
				_isDirtyProperty = true;
			}

			#if !NOUSE_BULLETXNA_UNITY
			if( this.bulletPhysicsWorld != null ) {
				if( _isDirtyProperty ) {
					_isDirtyProperty = false;
					_worldUpdateProperty.gravityScale = _gravityScaleCached;
					_worldUpdateProperty.gravityNoise = _gravityNoiseCached;
					_worldUpdateProperty.gravityDirection = _gravityDirectionCached;
					this.bulletPhysicsWorld.Update( deltaTime, _worldUpdateProperty );
				} else {
					this.bulletPhysicsWorld.Update( deltaTime, null );
				}
				return;
			}
			#endif

			if( this.worldPtr != IntPtr.Zero ) {
				if( _isDirtyProperty ) {
					_isDirtyProperty = false;
					_updatePropertyWriter.Clear();
					_updatePropertyWriter.Write( "gravityScale",		_gravityScaleCached );
					_updatePropertyWriter.Write( "gravityNoise",		_gravityNoiseCached );
					_updatePropertyWriter.Write( "gravityDirection",	_gravityDirectionCached );
					_updatePropertyWriter.Lock();
					_UpdateWorld( this.worldPtr, deltaTime,
					             _updatePropertyWriter.iValuesPtr, _updatePropertyWriter.iValueLength,
					             _updatePropertyWriter.fValuesPtr, _updatePropertyWriter.fValueLength );
					_updatePropertyWriter.Unlock();
				} else {
					_UpdateWorld( this.worldPtr, deltaTime, IntPtr.Zero, 0, IntPtr.Zero, 0 );
				}
			}
		}
	}
	
	public class RigidBody
	{
		[System.Flags]
		public enum UpdateFlags
		{
			Freezed = 0x01,
		}

		public MMD4MecanimRigidBody				rigidBody;
		public IntPtr							rigidBodyPtr;
		#if !NOUSE_BULLETXNA_UNITY
		public MMD4MecanimInternal.Bullet.RigidBody	bulletRigidBody;
		#endif

		private float[]							fValues = new float[7];

		private SphereCollider					_sphereCollider;
		private BoxCollider						_boxCollider;
		private CapsuleCollider					_capsuleCollider;
		
		private Vector3 _center {
			get {
				if( _sphereCollider != null ) {
					return _sphereCollider.center;
				} else if( _boxCollider != null ) {
					return _boxCollider.center;
				} else if( _capsuleCollider != null ) {
					return _capsuleCollider.center;
				}
				return Vector3.zero;
			}
		}
		
		~RigidBody()
		{
			Destroy();
		}
		
		public bool isExpired
		{
			get {
				#if !NOUSE_BULLETXNA_UNITY
				if( this.bulletRigidBody != null ) {
					return false;
				}
				#endif
				return this.rigidBodyPtr == IntPtr.Zero;
			}
		}
		
		public bool Create( MMD4MecanimRigidBody rigidBody )
		{
			Destroy();

			if( rigidBody == null ) {
				return false;
			}
			
			World joinWorld = null;
			if( MMD4MecanimBulletPhysics.instance != null ) {
				joinWorld = MMD4MecanimBulletPhysics.instance.globalWorld;
			}
			if( joinWorld == null ) {
				return false;
			}

			#if !NOUSE_BULLETXNA_UNITY
			MMD4MecanimInternal.Bullet.RigidBody.CreateProperty createProperty = new MMD4MecanimInternal.Bullet.RigidBody.CreateProperty();
			bool isUseBulletXNA = _isUseBulletXNA;
			#endif

			#if !NOUSE_BULLETXNA_UNITY
			PropertyWriter propertyWriter = isUseBulletXNA ? null : (new PropertyWriter());
			#else
			PropertyWriter propertyWriter = new PropertyWriter();
			#endif

			Matrix4x4 matrix = rigidBody.transform.localToWorldMatrix;
			Vector3 position = rigidBody.transform.position;
			Quaternion rotation = rigidBody.transform.rotation;
			Vector3 scale = MMD4MecanimCommon.ComputeMatrixScale( ref matrix );

			Vector3 center = this._center;
			if( center != Vector3.zero ) {
				position = matrix.MultiplyPoint3x4( center );
			}

			SphereCollider sphereCollider = rigidBody.gameObject.GetComponent< SphereCollider >();
			if( sphereCollider != null ) {
				float radiusSize = sphereCollider.radius;
				radiusSize *= Mathf.Max( Mathf.Max( scale.x, scale.y ), scale.z );
				if( propertyWriter != null ) {
					propertyWriter.Write( "shapeType", 0 );
					propertyWriter.Write( "shapeSize", new Vector3( radiusSize, 0.0f, 0.0f ) );
				}
				#if !NOUSE_BULLETXNA_UNITY
				if( isUseBulletXNA ) {
					createProperty.shapeType = 0;
					createProperty.shapeSize = new Vector3( radiusSize, 0.0f, 0.0f );
				}
				#endif
			}
			BoxCollider boxCollider = rigidBody.gameObject.GetComponent< BoxCollider >();
			if( boxCollider != null ) {
				Vector3 boxSize = boxCollider.size;
				boxSize.x *= scale.x;
				boxSize.y *= scale.y;
				boxSize.z *= scale.z;
				if( propertyWriter != null ) {
					propertyWriter.Write( "shapeType", 1 );
					propertyWriter.Write( "shapeSize", boxSize * 0.5f );
				}
				#if !NOUSE_BULLETXNA_UNITY
				if( isUseBulletXNA ) {
					createProperty.shapeType = 1;
					createProperty.shapeSize = boxSize * 0.5f;
				}
				#endif
			}
			CapsuleCollider capsuleCollider = rigidBody.gameObject.GetComponent< CapsuleCollider >();
			if( capsuleCollider != null ) {
				Vector3 capsuleSize = new Vector3( capsuleCollider.radius, capsuleCollider.height, 0.0f );
				capsuleSize.x *= Mathf.Max( scale.x, scale.z );
				capsuleSize.y *= scale.y;
				capsuleSize.y -= capsuleCollider.radius * 2.0f;
				if( propertyWriter != null ) {
					propertyWriter.Write( "shapeType", 2 );
					propertyWriter.Write( "shapeSize", capsuleSize );
				}
				#if !NOUSE_BULLETXNA_UNITY
				if( isUseBulletXNA ) {
					createProperty.shapeType = 2;
					createProperty.shapeSize = capsuleSize;
				}
				#endif
			}
			_sphereCollider		= sphereCollider;
			_boxCollider		= boxCollider;
			_capsuleCollider	= capsuleCollider;

			if( capsuleCollider != null ) {
				if( capsuleCollider.direction == 0 ) { // X axis
					rotation *= rotateQuaternionX;
				} else if( capsuleCollider.direction == 2 ) { // Z axis
					rotation *= rotateQuaternionZ;
				}
			}

			if( joinWorld.worldProperty != null ) {
				if( propertyWriter != null ) {
					propertyWriter.Write( "worldScale", joinWorld.worldProperty.worldScale );
				}
				#if !NOUSE_BULLETXNA_UNITY
				if( isUseBulletXNA ) {
					createProperty.unityScale = joinWorld.worldProperty.worldScale;
				}
				#endif
			} else {
				if( propertyWriter != null ) {
					propertyWriter.Write( "worldScale", 1.0f );
				}
				#if !NOUSE_BULLETXNA_UNITY
				if( isUseBulletXNA ) {
					createProperty.unityScale = 1.0f;
				}
				#endif
			}

			//position.x = -position.x;
			//rotation.y = -rotation.y;
			//rotation.z = -rotation.z;

			if( propertyWriter != null ) {
				propertyWriter.Write( "position",	position );
				propertyWriter.Write( "rotation",	rotation );
			}
			#if !NOUSE_BULLETXNA_UNITY
			if( isUseBulletXNA ) {
				createProperty.position = position;
				createProperty.rotation = rotation;
			}
			#endif

			int rigidBodyFlags = 0;
			if( rigidBody.bulletPhysicsRigidBodyProperty != null ) {
				if( rigidBody.bulletPhysicsRigidBodyProperty.isKinematic ) {
					rigidBodyFlags |= 0x01;
				}
				if( rigidBody.bulletPhysicsRigidBodyProperty.isAdditionalDamping ) {
					rigidBodyFlags |= 0x02;
				}
				#if !NOUSE_BULLETXNA_UNITY
				if( isUseBulletXNA ) {
					createProperty.isKinematic = rigidBody.bulletPhysicsRigidBodyProperty.isKinematic;
					createProperty.isAdditionalDamping = rigidBody.bulletPhysicsRigidBodyProperty.isAdditionalDamping;
				}
				#endif

				float mass = rigidBody.bulletPhysicsRigidBodyProperty.mass;
				if( rigidBody.bulletPhysicsRigidBodyProperty.isKinematic ) {
					mass = 0.0f; // Hotfix: Todo: Move to plugin/classes
				}
				
				if( propertyWriter != null ) {
					propertyWriter.Write( "mass",			mass );
					propertyWriter.Write( "linearDamping",	rigidBody.bulletPhysicsRigidBodyProperty.linearDamping );
					propertyWriter.Write( "angularDamping",	rigidBody.bulletPhysicsRigidBodyProperty.angularDamping );
					propertyWriter.Write( "restitution",	rigidBody.bulletPhysicsRigidBodyProperty.restitution );
					propertyWriter.Write( "friction",		rigidBody.bulletPhysicsRigidBodyProperty.friction );
				}
				#if !NOUSE_BULLETXNA_UNITY
				if( isUseBulletXNA ) {
					createProperty.mass = mass;
					createProperty.linearDamping = rigidBody.bulletPhysicsRigidBodyProperty.linearDamping;
					createProperty.angularDamping = rigidBody.bulletPhysicsRigidBodyProperty.angularDamping;
					createProperty.restitution = rigidBody.bulletPhysicsRigidBodyProperty.restitution;
					createProperty.friction = rigidBody.bulletPhysicsRigidBodyProperty.friction;
				}
				#endif
			}

			if( propertyWriter != null ) {
				propertyWriter.Write( "flags", rigidBodyFlags );
				propertyWriter.Write( "group", 65535 );
				propertyWriter.Write( "mask", 65535 );
			}
			#if !NOUSE_BULLETXNA_UNITY
			if( isUseBulletXNA ) {
				createProperty.group = 65535;
				createProperty.mask = 65535;
			}
			#endif

			#if !NOUSE_BULLETXNA_UNITY
			if( isUseBulletXNA ) {
				this.bulletRigidBody = new MMD4MecanimInternal.Bullet.RigidBody();
				if( !this.bulletRigidBody.Create( ref createProperty ) ) {
					this.bulletRigidBody.Destroy();
					this.bulletRigidBody = null;
					return false;
				}
				if( joinWorld.bulletPhysicsWorld != null ) {
					joinWorld.bulletPhysicsWorld.JoinWorld( this.bulletRigidBody );
				}
				this.rigidBody = rigidBody;
				return true;
			}
			#endif

			propertyWriter.Lock();
			IntPtr rigidBodyPtr = _CreateRigidBody(
				propertyWriter.iValuesPtr, propertyWriter.iValueLength,
				propertyWriter.fValuesPtr, propertyWriter.fValueLength );
			propertyWriter.Unlock();
			
			if( rigidBodyPtr != IntPtr.Zero ) {
				_JoinWorldRigidBody( joinWorld.worldPtr, rigidBodyPtr );
				DebugLog();
				this.rigidBody = rigidBody;
				this.rigidBodyPtr = rigidBodyPtr;
				return true;
			} else {
				DebugLog();
				return false;
			}
		}
		
		public void Update()
		{
			if( rigidBody != null && rigidBody.bulletPhysicsRigidBodyProperty != null ) {
				bool isKinematic = rigidBody.bulletPhysicsRigidBodyProperty.isKinematic;
				bool isFreezed = rigidBody.bulletPhysicsRigidBodyProperty.isFreezed;
				if( isKinematic || isFreezed ) {
					int updateFlags = 0;
					if( isFreezed ) {
						updateFlags |= (int)UpdateFlags.Freezed;
					}

					Vector3 position = rigidBody.transform.position;
					Quaternion rotation = rigidBody.transform.rotation;
					
					Vector3 center = this._center;
					if( center != Vector3.zero ) {
						position = rigidBody.transform.localToWorldMatrix.MultiplyPoint3x4( center );
					}
					
					if( _capsuleCollider != null ) {
						if( _capsuleCollider.direction == 0 ) { // X axis
							rotation *= rotateQuaternionX;
						} else if( _capsuleCollider.direction == 2 ) { // Z axis
							rotation *= rotateQuaternionZ;
						}
					}

					#if !NOUSE_BULLETXNA_UNITY
					if( this.bulletRigidBody != null ) {
						this.bulletRigidBody.Update( updateFlags, ref position, ref rotation );
					}
					#endif
					if( rigidBodyPtr != IntPtr.Zero ) {
						fValues[0] = position.x;
						fValues[1] = position.y;
						fValues[2] = position.z;
						fValues[3] = rotation.x;
						fValues[4] = rotation.y;
						fValues[5] = rotation.z;
						fValues[6] = rotation.w;

						GCHandle gch_fValues = GCHandle.Alloc(fValues, GCHandleType.Pinned);
						_UpdateRigidBody( rigidBodyPtr, updateFlags, IntPtr.Zero, 0, gch_fValues.AddrOfPinnedObject(), fValues.Length );
						gch_fValues.Free();
					}
				} else {
					#if !NOUSE_BULLETXNA_UNITY
					if( this.bulletRigidBody != null ) {
						this.bulletRigidBody.Update( 0 );
					}
					#endif
					if( rigidBodyPtr != IntPtr.Zero ) {
						GCHandle gch_fValues = GCHandle.Alloc(fValues, GCHandleType.Pinned);
						_UpdateRigidBody( rigidBodyPtr, 0, IntPtr.Zero, 0, IntPtr.Zero, 0 );
						gch_fValues.Free();
					}
				}
			}
		}
		
		public void LateUpdate()
		{
			if( rigidBody != null && rigidBody.bulletPhysicsRigidBodyProperty != null ) {
				bool isKinematic = rigidBody.bulletPhysicsRigidBodyProperty.isKinematic;
				bool isFreezed = rigidBody.bulletPhysicsRigidBodyProperty.isFreezed;
				if( !isKinematic && !isFreezed ) {
					Vector3 position = Vector3.one;
					Quaternion rotation = Quaternion.identity;

					int lateUpdated = 0;
					#if !NOUSE_BULLETXNA_UNITY
					if( this.bulletRigidBody != null ) {
						lateUpdated = this.bulletRigidBody.LateUpdate( ref position, ref rotation );
					}
					#endif
					if( rigidBodyPtr != IntPtr.Zero ) {
						GCHandle gch_fValues = GCHandle.Alloc(fValues, GCHandleType.Pinned);
						lateUpdated = _LateUpdateRigidBody( rigidBodyPtr, IntPtr.Zero, 0, gch_fValues.AddrOfPinnedObject(), fValues.Length );
						gch_fValues.Free();

						position = new Vector3( fValues[0], fValues[1], fValues[2] );
						rotation = new Quaternion( fValues[3], fValues[4], fValues[5], fValues[6] );
					}

					if( lateUpdated != 0 ) {
						if( _capsuleCollider != null ) {
							if( _capsuleCollider.direction == 0 ) { // X axis
								rotation *= rotateQuaternionXInv;
							} else if( _capsuleCollider.direction == 2 ) { // Z axis
								rotation *= rotateQuaternionZInv;
							}
						}

						rigidBody.gameObject.transform.position = position;
						rigidBody.gameObject.transform.rotation = rotation;

						Vector3 center = this._center;
						if( center != Vector3.zero ) {
							Vector3 localPosition = rigidBody.gameObject.transform.localPosition;
							localPosition -= center;
							rigidBody.gameObject.transform.localPosition = localPosition;
						}
					}
				}
			}
		}
		
		public void Destroy()
		{
			#if !NOUSE_BULLETXNA_UNITY
			if( this.bulletRigidBody != null ) {
				this.bulletRigidBody.Destroy();
				this.bulletRigidBody = null;
			}
			#endif
			if( this.rigidBodyPtr != IntPtr.Zero ) {
				IntPtr rigidBodyPtr = this.rigidBodyPtr;
				this.rigidBodyPtr = IntPtr.Zero;
				_DestroyRigidBody( rigidBodyPtr );
			}

			_sphereCollider		= null;
			_boxCollider		= null;
			_capsuleCollider	= null;
			this.rigidBody		= null;
		}
	};
	
	public class MMDModel
	{
		public World							localWorld;
		public MMD4MecanimModel					model;

		#if !NOUSE_BULLETXNA_UNITY
		public MMD4MecanimInternal.Bullet.MMDModel	bulletMMDModel;
		#endif
		public IntPtr							mmdModelPtr;

		Transform								_modelTransform = null;

		bool									_physicsEnabled = false;

		Transform[]								_boneTransformList = null;
		int[]									_updateRigidBodyFlagsList = null;
		float[]									_updateIKWeightList = null;
		float[]									_updateMorphWeightList = null;
		float[]									_ikWeightList = null;
		bool[]									_ikDisabledList = null;
		
		Vector3									_lossyScale = Vector3.one;
		int[]									_updateIValues;
		float[]									_updateFValues;
		int[]									_updateBoneFlagsList = null;
		Matrix4x4[]								_updateBoneTransformList = null;
		Vector3[]								_updateBonePositionList = null;
		Vector3[]								_updateBonePositionList2 = null;
		Quaternion[]							_updateBoneRotationList = null;
		Quaternion[]							_updateBoneRotationList2 = null;
		Vector3[]								_updateUserPositionList = null;
		Quaternion[]							_updateUserRotationList = null;
		uint[]									_updateUserPositionIsValidList = null;
		uint[]									_updateUserRotationIsValidList = null;
		int[]									_lateUpdateBoneFlagsList = null;
		Vector3[]								_lateUpdateBonePositionList = null;
		Quaternion[]							_lateUpdateBoneRotationList = null;
		int[]									_lateUpdateMeshFlagsList = null;

		bool									_isDirtyPreUpdate = true;
		bool									_isUpdatedAtLeastOnce = false;

		public enum UpdateIValues
		{
			AnimationHashName,
			Max,
		}

		public enum UpdateFValues
		{
			AnimationTime,
			PPHShoulderFixRate,
			Max,
		}

		[System.Flags]
		public enum LateUpdateMeshFlags
		{
			Vertices							= 0x00000001, // from DLL
			Normals								= 0x00000002, // from DLL
		}
		
		[System.Flags]
		public enum UpdateFlags
		{
			IKEnabled							= 0x00000001, // from Unity
			BoneInherenceEnabled				= 0x00000002, // from Unity
			BoneMorphEnabled					= 0x00000004, // from Unity
			PPHShoulderEnabled					= 0x00000008, // from Unity
		}

		[System.Flags]
		public enum UpdateBoneFlags
		{
			WorldTransform						= 0x00000001, // from DLL(PreUpdate)/Unity(Update)
			Position							= 0x00000002, // from DLL(PreUpdate)/Unity(Update)
			Rotation							= 0x00000004, // from DLL(PreUpdate)/Unity(Update)
			CheckPosition						= 0x00000008, // from DLL(PreUpdate)/Unity(Update)
			CheckRotation						= 0x00000010, // from DLL(PreUpdate)/Unity(Update)
			ChangedPosition						= 0x00000020, // from Unity(CheckPosition only)
			ChangedRotation						= 0x00000040, // from Unity(CheckRotation only)
			UserPosition						= 0x00000080, // from Unity userPosition != (0,0,0)
			UserRotation						= 0x00000100, // from Unity userRotation != (0,0,0,1)

			SkeletonMask						= unchecked( (int)0xff000000 ),
			SkeletonLeftShoulder				= 0x01000000,
			SkeletonLeftUpperArm				= 0x02000000,
			SkeletonRightShoulder				= 0x03000000,
			SkeletonRightUpperArm				= 0x04000000,

			UserTransform						= (UserPosition | UserRotation), // from Unity userPosition != (0,0,0) || userRotation != (0,0,0,1)
		}

		[System.Flags]
		public enum UpdateRigidBodyFlags
		{
			Freezed								= 0x00000001, // from Unity
		}

		[System.Flags]
		public enum LateUpdateFlags
		{
			Bone								= 0x00000001, // from DLL
			Mesh								= 0x00000002, // from DLL
		}

		[System.Flags]
		public enum LateUpdateBoneFlags
		{
			LateUpdated							= 0x00000001, // from DLL
			Position							= 0x00000002, // from DLL
			Rotation							= 0x00000004, // from DLL
		}

		~MMDModel()
		{
			Destroy();
		}
		
		public bool isExpired
		{
			get {
				#if !NOUSE_BULLETXNA_UNITY
				if( this.bulletMMDModel != null ) {
					return false;
				}
				#endif
				return this.mmdModelPtr == IntPtr.Zero && this.localWorld == null;
			}
		}
		
		private bool _Prepare( MMD4MecanimModel model )
		{
			if( model == null ) {
				return false;
			}

			this.model = model;
			_modelTransform = model.transform;
			MMD4MecanimData.ModelData modelData = model.modelData;
			if( modelData == null ||
			    modelData.boneDataList == null ||
				model.boneList == null ||
			    model.boneList.Length != modelData.boneDataList.Length ) {
				Debug.LogError( "_Prepare: Failed." );
				return false;
			}

			unchecked {
				if( modelData.boneDataList != null ) {
					int boneListLength = modelData.boneDataList.Length;
					_boneTransformList = new Transform[boneListLength];
					for( int i = 0; i < boneListLength; ++i ) {
						if( model.boneList[i] != null && model.boneList[i].gameObject != null ) {
							_boneTransformList[i] = model.boneList[i].gameObject.transform;
						}
					}
				} else {
					_boneTransformList = new Transform[0];
				}

				if( modelData.ikDataList != null ) {
					int ikLength = modelData.ikDataList.Length;
					_ikWeightList = new float[ikLength];
					_ikDisabledList = new bool[ikLength];
					_updateIKWeightList = new float[ikLength];
					for( int i = 0; i < ikLength; ++i ) {
						_ikWeightList[i] = 1.0f;
						_updateIKWeightList[i] = 1.0f;
					}
				} else {
					_ikWeightList = new float[0];
					_ikDisabledList = new bool[0];
					_updateIKWeightList = new float[0];
				}

				if( modelData.morphDataList != null ) {
					int morphLength = modelData.morphDataList.Length;
					_updateMorphWeightList = new float[morphLength];
				} else {
					_updateMorphWeightList = new float[0];
				}

				if( modelData.rigidBodyDataList != null ) {
					int rigidBodyLength = modelData.rigidBodyDataList.Length;
					this._updateRigidBodyFlagsList = new int[rigidBodyLength];
					for( int i = 0; i < rigidBodyLength; ++i ) {
						RigidBodyData rigidBodyData = modelData.rigidBodyDataList[i];
						if( rigidBodyData.isFreezed ) {
							this._updateRigidBodyFlagsList[i] |= (int)UpdateRigidBodyFlags.Freezed;
						}
					}
				} else {
					this._updateRigidBodyFlagsList = new int[0];
				}
			}

			_physicsEnabled = (model.physicsEngine != MMD4MecanimModel.PhysicsEngine.None);
			_isDirtyPreUpdate = true;
			_PrepareWork( model );
			return true;
		}

		public bool Create( MMD4MecanimModel model )
		{
			if( model == null ) {
				return false;
			}
			byte[] mmdModelBytes = model.modelFileBytes;
			if( mmdModelBytes == null ) {
				Debug.LogError("");
				return false;
			}
			if( !_Prepare( model ) ) {
				Debug.LogError("");
				return false;
			}

			if( _modelTransform != null ) {
				_lossyScale = _modelTransform.transform.lossyScale;
			} else {
				_lossyScale = Vector3.one;
			}

			bool joinLocalWorld = true;
			bool useOriginalScale = true;
			bool useCustomResetTime = false;
			bool optimizeBulletXNA = true;
			float resetMorphTime = 0.0f;
			float resetWaitTime = 0.0f;
			MMD4MecanimInternal.Bullet.MMDModelProperty mmdModelProperty = null;
			MMD4MecanimInternal.Bullet.WorldProperty localWorldProperty = null;
			if( model.bulletPhysics != null ) {
				mmdModelProperty = model.bulletPhysics.mmdModelProperty;
				localWorldProperty = model.bulletPhysics.worldProperty;
				joinLocalWorld = model.bulletPhysics.joinLocalWorld;
				useOriginalScale = model.bulletPhysics.useOriginalScale;
				useCustomResetTime = model.bulletPhysics.useCustomResetTime;
				resetMorphTime = model.bulletPhysics.resetMorphTime;
				resetWaitTime = model.bulletPhysics.resetWaitTime;
			}

			float worldScale = 0.0f;
			float importScale = model.importScale;
			World joinWorld = null;
			World localWorld = null;

			if( importScale == 0.0f ) {
				importScale = model.modelData.importScale;
			}

			if( joinLocalWorld ) {
				if( localWorldProperty == null ) {
					Debug.LogError( "localWorldProperty is null." );
					return false;
				}
				
				localWorld = new World();
				joinWorld = localWorld;

				if( !localWorld.Create( localWorldProperty ) ) {
					Debug.LogError("");
					return false;
				}
				
				if( useOriginalScale ) {
					worldScale = model.modelData.vertexScale * importScale;
				} else {
					worldScale = localWorldProperty.worldScale;
				}

				optimizeBulletXNA = localWorldProperty.optimizeBulletXNA;
			} else {
				if( MMD4MecanimBulletPhysics.instance != null ) {
					joinWorld = MMD4MecanimBulletPhysics.instance.globalWorld;
				}
				if( joinWorld == null ) {
					Debug.LogError("");
					return false;
				}
				if( joinWorld.worldProperty == null ) {
					Debug.LogError( "worldProperty is null." );
					return false;
				}

				worldScale = joinWorld.worldProperty.worldScale;
				optimizeBulletXNA = joinWorld.worldProperty.optimizeBulletXNA;
			}

			bool xdefEnabled = model.xdefEnabled;
			bool vertexMorphEnabled = model.vertexMorphEnabled;
			bool blendShapesEnabled = model.blendShapesEnabled;
			#if UNITY_IPHONE || UNITY_ANDROID
			xdefEnabled = xdefEnabled && model.xdefMobileEnabled;
			#endif

			byte[] mmdIndexBytes = null;
			byte[] mmdVertexBytes = null;
			int[] meshFlags = null;
			if( model.skinningEnabled ) {
				if( vertexMorphEnabled || xdefEnabled ) {
					bool blendShapesAnything = false;
					meshFlags = model._PrepareMeshFlags( out blendShapesAnything );
					// Override blendShapedEnabled.
					blendShapesEnabled = (vertexMorphEnabled && blendShapesEnabled && blendShapesAnything);

					if( (vertexMorphEnabled && !blendShapesEnabled) || xdefEnabled ) {
						mmdIndexBytes = model.indexFileBytes;
						if( mmdIndexBytes != null ) {
							mmdVertexBytes = model.vertexFileBytes;
						}
					}
				} else {
					meshFlags = model._PrepareMeshFlags();
				}
			}

			#if !NOUSE_BULLETXNA_UNITY
			bool isUseBulletXNA = _isUseBulletXNA;
			if( isUseBulletXNA ) {
				MMD4MecanimInternal.Bullet.MMDModel.ImportProperty importProperty = new MMD4MecanimInternal.Bullet.MMDModel.ImportProperty();
				importProperty.isPhysicsEnabled = _physicsEnabled;
				importProperty.isJoinedLocalWorld = joinLocalWorld;
				importProperty.isVertexMorphEnabled = vertexMorphEnabled;
				importProperty.isBlendShapesEnabled = blendShapesEnabled;
				importProperty.isXDEFEnabled = xdefEnabled;
				importProperty.isXDEFNormalEnabled = model.xdefNormalEnabled;
				importProperty.useCustomResetTime = useCustomResetTime;
				importProperty.resetMorphTime = resetMorphTime;
				importProperty.resetWaitTime = resetWaitTime;
				importProperty.mmdModelProperty = mmdModelProperty;
				importProperty.optimizeBulletXNA = optimizeBulletXNA;

				if( importProperty.mmdModelProperty != null ) {
					importProperty.mmdModelProperty.worldScale = worldScale;
					importProperty.mmdModelProperty.importScale = importScale;
					importProperty.mmdModelProperty.lossyScale = _lossyScale;
				}

				this.bulletMMDModel = new MMD4MecanimInternal.Bullet.MMDModel();
				if( !this.bulletMMDModel.Import(
					mmdModelBytes,
					mmdIndexBytes,
					mmdVertexBytes,
					meshFlags,
					ref importProperty ) ) {
					Debug.LogError("");
					this.bulletMMDModel.Destroy();
					if( localWorld != null ) {
						localWorld.Destroy();
					}
					return false;
				}

				if( joinWorld != null && joinWorld.bulletPhysicsWorld != null ) {
					joinWorld.bulletPhysicsWorld.JoinWorld( this.bulletMMDModel );
				}
				this.localWorld = localWorld;
				_UploadMesh( model, meshFlags );
				return true;
			}
			#endif

			MMD4MecanimCommon.PropertyWriter property = new MMD4MecanimCommon.PropertyWriter();
			property.Write( "worldScale", worldScale );
			property.Write( "importScale", importScale );
			property.Write( "lossyScale", _lossyScale );
			if( useCustomResetTime ) {
				property.Write( "resetMorphTime", resetMorphTime );
				property.Write( "resetWaitTime", resetWaitTime );
			}
			if( mmdModelProperty != null ) {
				property.Write( "isPhysicsEnabled",								_physicsEnabled );
				property.Write( "isJoinedLocalWorld",							joinLocalWorld );
				property.Write( "isVertexMorphEnabled",							vertexMorphEnabled );
				property.Write( "isBlendShapesEnabled",							blendShapesEnabled );
				property.Write( "isXDEFEnabled",								xdefEnabled );
				property.Write( "isXDEFNormalEnabled",							model.xdefNormalEnabled );

				property.Write( "rigidBodyIsAdditionalDamping",					mmdModelProperty.rigidBodyIsAdditionalDamping );
				property.Write( "rigidBodyIsEnableSleeping",					mmdModelProperty.rigidBodyIsEnableSleeping );
				property.Write( "rigidBodyIsUseCcd",							mmdModelProperty.rigidBodyIsUseCcd );
				property.Write( "rigidBodyCcdMotionThreshold",					mmdModelProperty.rigidBodyCcdMotionThreshold );
				property.Write( "rigidBodyShapeScale",							mmdModelProperty.rigidBodyShapeScale );
				property.Write( "rigidBodyMassRate",							mmdModelProperty.rigidBodyMassRate );
				property.Write( "rigidBodyLinearDampingRate",					mmdModelProperty.rigidBodyLinearDampingRate );
				property.Write( "rigidBodyAngularDampingRate",					mmdModelProperty.rigidBodyAngularDampingRate );
				property.Write( "rigidBodyRestitutionRate",						mmdModelProperty.rigidBodyRestitutionRate );
				property.Write( "rigidBodyFrictionRate",						mmdModelProperty.rigidBodyFrictionRate );

				property.Write( "rigidBodyAntiJitterRate",						mmdModelProperty.rigidBodyAntiJitterRate );
				property.Write( "rigidBodyAntiJitterRateOnKinematic",			mmdModelProperty.rigidBodyAntiJitterRateOnKinematic );
				property.Write( "rigidBodyPreBoneAlignmentLimitLength",			mmdModelProperty.rigidBodyPreBoneAlignmentLimitLength );
				property.Write( "rigidBodyPreBoneAlignmentLossRate",			mmdModelProperty.rigidBodyPreBoneAlignmentLossRate );
				property.Write( "rigidBodyPostBoneAlignmentLimitLength",		mmdModelProperty.rigidBodyPostBoneAlignmentLimitLength );
				property.Write( "rigidBodyPostBoneAlignmentLossRate",			mmdModelProperty.rigidBodyPostBoneAlignmentLossRate );
				property.Write( "rigidBodyLinearDampingLossRate",				mmdModelProperty.rigidBodyLinearDampingLossRate );
				property.Write( "rigidBodyAngularDampingLossRate",				mmdModelProperty.rigidBodyAngularDampingLossRate );
				property.Write( "rigidBodyLinearVelocityLimit",					mmdModelProperty.rigidBodyLinearVelocityLimit );
				property.Write( "rigidBodyAngularVelocityLimit",				mmdModelProperty.rigidBodyAngularVelocityLimit );

				property.Write( "rigidBodyIsUseForceAngularVelocityLimit",		mmdModelProperty.rigidBodyIsUseForceAngularVelocityLimit );
				property.Write( "rigidBodyIsUseForceAngularAccelerationLimit",	mmdModelProperty.rigidBodyIsUseForceAngularAccelerationLimit );
				property.Write( "rigidBodyForceAngularVelocityLimit",			mmdModelProperty.rigidBodyForceAngularVelocityLimit );

				property.Write( "rigidBodyIsAdditionalCollider",				mmdModelProperty.rigidBodyIsAdditionalCollider );
				property.Write( "rigidBodyAdditionalColliderBias",				mmdModelProperty.rigidBodyAdditionalColliderBias );

				property.Write( "rigidBodyIsForceTranslate",					mmdModelProperty.rigidBodyIsForceTranslate );

				property.Write( "jointRootAdditionalLimitAngle",				mmdModelProperty.jointRootAdditionalLimitAngle );
			}

			var gch_mmdModelBytes	= MMD4MecanimCommon.MakeGCHValues( mmdModelBytes );
			var gch_mmdIndexBytes	= MMD4MecanimCommon.MakeGCHValues( mmdIndexBytes );
			var gch_mmdVertexBytes	= MMD4MecanimCommon.MakeGCHValues( mmdVertexBytes );
			var gch_meshFlags		= MMD4MecanimCommon.MakeGCHValues( meshFlags );
			property.Lock();

			IntPtr mmdModelPtr = _CreateMMDModel(
				gch_mmdModelBytes, gch_mmdModelBytes.length,
				gch_mmdIndexBytes, gch_mmdIndexBytes.length,
				gch_mmdVertexBytes, gch_mmdVertexBytes.length,
				gch_meshFlags, gch_meshFlags.length,
				property.iValuesPtr, property.iValueLength,
				property.fValuesPtr, property.fValueLength );

			property.Unlock();
			gch_meshFlags.Free();
			gch_mmdVertexBytes.Free();
			gch_mmdIndexBytes.Free();
			gch_mmdModelBytes.Free();

			if( mmdModelPtr != IntPtr.Zero ) {
				_JoinWorldMMDModel( joinWorld.worldPtr, mmdModelPtr );
				DebugLog();
				this.localWorld = localWorld;
				this.mmdModelPtr = mmdModelPtr;
				_UploadMesh( model, meshFlags );
				return true;
			} else {
				if( localWorld != null ) {
					localWorld.Destroy();
				}
				DebugLog();
				Debug.LogError("");
				return false;
			}
		}

		void _UploadMesh( MMD4MecanimModel model, int[] meshFlags )
		{
			if( model == null || meshFlags == null || meshFlags.Length == 0 ) {
				return;
			}

			model._InitializeCloneMesh( meshFlags );

			#if !NOUSE_BULLETXNA_UNITY
			if( this.bulletMMDModel != null ) {
				for( int i = 0; i != meshFlags.Length; ++i ) {
					if( (meshFlags[i] & (int)(MeshFlags.VertexMorph | MeshFlags.XDEF)) != 0 ) {
						MMD4MecanimModel.CloneMesh cloneMesh = model._GetCloneMesh( i );
						if( cloneMesh != null ) {
							Vector3[] vertices = cloneMesh.vertices;
							Vector3[] normals = null;
							BoneWeight[] boneWeights = null;
							Matrix4x4[] bindposes = null;

							if( (meshFlags[i] & (int)MeshFlags.XDEF) != 0 ) {
								if( model.xdefNormalEnabled ) {
									normals = cloneMesh.normals;
								}
								boneWeights = cloneMesh.boneWeights;
								bindposes = cloneMesh.bindposes;
							}

							this.bulletMMDModel.UploadMesh( i, vertices, normals, boneWeights, bindposes );
						}
					}
				}
			}
			#endif

			if( this.mmdModelPtr != IntPtr.Zero ) {
				for( int i = 0; i != meshFlags.Length; ++i ) {
					if( (meshFlags[i] & (int)(MeshFlags.VertexMorph | MeshFlags.XDEF)) != 0 ) {
						MMD4MecanimModel.CloneMesh cloneMesh = model._GetCloneMesh( i );
						if( cloneMesh != null ) {
							Vector3[] vertices = cloneMesh.vertices;
							Vector3[] normals = null;
							BoneWeight[] boneWeights = null;
							Matrix4x4[] bindposes = null;

							if( (meshFlags[i] & (int)MeshFlags.XDEF) != 0 ) {
								if( model.xdefNormalEnabled ) {
									normals = cloneMesh.normals;
								}
								boneWeights = cloneMesh.boneWeights;
								bindposes = cloneMesh.bindposes;
							}

							var gch_vertices	= MMD4MecanimCommon.MakeGCHValues( vertices );
							var gch_normals		= MMD4MecanimCommon.MakeGCHValues( normals );
							var gch_boneWeights	= MMD4MecanimCommon.MakeGCHValues( boneWeights );
							var gch_bindposes	= MMD4MecanimCommon.MakeGCHValues( bindposes );

							_UploadMeshMMDModel( this.mmdModelPtr, i,
							                    gch_vertices,
							                    gch_normals,
							                    gch_boneWeights,
							                    gch_vertices.length,
							                    gch_bindposes,
							                    gch_bindposes.length );

							gch_boneWeights.Free();
							gch_normals.Free();
							gch_vertices.Free();
						}
					}
				}

				DebugLog();
			}

			model._CleanupCloneMesh();
		}

		void _PrepareWork( MMD4MecanimModel model )
		{
			int boneLength = 0;
			if( _boneTransformList != null ) {
				boneLength = _boneTransformList.Length;
			}

			_updateIValues						= new int[(int)UpdateIValues.Max];
			_updateFValues						= new float[(int)UpdateFValues.Max];
			_updateBoneFlagsList				= new int[boneLength];
			_updateBoneTransformList			= new Matrix4x4[boneLength];
			_updateBonePositionList				= new Vector3[boneLength];
			_updateBonePositionList2			= new Vector3[boneLength];
			_updateBoneRotationList				= new Quaternion[boneLength];
			_updateBoneRotationList2			= new Quaternion[boneLength];
			_updateUserPositionList				= new Vector3[boneLength];
			_updateUserRotationList				= new Quaternion[boneLength];
			_updateUserPositionIsValidList		= new uint[(boneLength + 31) / 32];
			_updateUserRotationIsValidList		= new uint[(boneLength + 31) / 32];
			for( int i = 0; i != boneLength; ++i ) {
				_updateUserPositionList[i]		= Vector3.zero;
				_updateUserRotationList[i]		= Quaternion.identity;
			}
		}
		
		public void Destroy()
		{
			#if !NOUSE_BULLETXNA_UNITY
			if( this.bulletMMDModel != null ) {
				this.bulletMMDModel.Destroy();
				this.bulletMMDModel = null;
			}
			#endif
			if( this.mmdModelPtr != IntPtr.Zero ) {
				IntPtr mmdModelPtr = this.mmdModelPtr;
				this.mmdModelPtr = IntPtr.Zero;
				_DestroyMMDModel( mmdModelPtr );
			}
			if( this.localWorld != null ) {
				this.localWorld.Destroy();
				this.localWorld = null;
			}
			_modelTransform	= null;
			this.model		= null;
		}

		public void SetRigidBodyFreezed( int rigidBodyID, bool isFreezed )
		{
			unchecked {
				if( _updateRigidBodyFlagsList != null && (uint)rigidBodyID < (uint)_updateRigidBodyFlagsList.Length ) {
					int flags = _updateRigidBodyFlagsList[rigidBodyID];
					bool isRigidBodyFreezed = ((flags & (int)UpdateRigidBodyFlags.Freezed) != 0);
					if( isFreezed != isRigidBodyFreezed ) {
						_isDirtyPreUpdate = true;
						if( isFreezed ) {
							flags |= (int)UpdateRigidBodyFlags.Freezed;
						} else {
							flags &= ~(int)UpdateRigidBodyFlags.Freezed;
						}
						this._updateRigidBodyFlagsList[rigidBodyID] = flags;
					}
				}
			}
		}

		public void SetIKProperty( int ikID, bool isEnabled, float ikWeight )
		{
			unchecked {
				if( this._ikWeightList != null &&
					this._ikDisabledList != null &&
					this._updateIKWeightList != null ) {
					if( (uint)ikID < (uint)this._ikWeightList.Length ) {
						if( this._ikDisabledList[ikID] != !isEnabled ||
						    this._ikWeightList[ikID] != ikWeight ) {
							_isDirtyPreUpdate = true;
							this._ikDisabledList[ikID] = !isEnabled;
							this._ikWeightList[ikID] = ikWeight;
							if( isEnabled ) {
								this._updateIKWeightList[ikID] = ikWeight;
							} else {
								this._updateIKWeightList[ikID] = 0.0f;
							}
						}
					}
				}
			}
		}
		
		static void _Swap< Type >( ref Type[] lhs, ref Type[] rhs )
		{
			Type[] t = lhs;
			lhs = rhs;
			rhs = t;
		}

		public void Update()
		{
			if( _boneTransformList			== null ||
			    _updateIValues				== null ||
			    _updateFValues				== null ||
			    _updateBoneFlagsList		== null ||
				_updateBoneTransformList	== null ||
				_updateBonePositionList		== null ||
				_updateBonePositionList2	== null ||
				_updateBoneRotationList		== null ||
				_updateBoneRotationList2	== null ||
				_updateUserPositionList		== null ||
				_updateUserRotationList		== null ||
				_updateRigidBodyFlagsList	== null ||
				_updateIKWeightList			== null ||
				_updateMorphWeightList		== null ||
				_modelTransform				== null ) {
				return;
			}

			_Swap( ref _updateBonePositionList, ref _updateBonePositionList2 );
			_Swap( ref _updateBoneRotationList, ref _updateBoneRotationList2 );

			// Feedback properties from MMDModel
			if( this.model != null ) {
				#if UNITY_IPHONE || UNITY_ANDROID
				//bool xdefEnabled = this.model.xdefEnabled && this.model.xdefMobileEnabled;
				#else
				//bool xdefEnabled = this.model.xdefEnabled;
				#endif

				// World Property(Local only)
				if( this.localWorld != null ) {
					this.localWorld.SetGravity(
						this.model.bulletPhysics.worldProperty.gravityScale,
						this.model.bulletPhysics.worldProperty.gravityNoise,
						this.model.bulletPhysics.worldProperty.gravityDirection );
				}

				// IK
				if( this.model.ikList != null ) {
					for( int i = 0; i != this.model.ikList.Length; ++i ) {
						if( this.model.ikList[i] != null ) {
							SetIKProperty( i, this.model.ikList[i].ikEnabled, this.model.ikList[i].ikWeight );
						}
					}
				}

				// Morph
				if( this.model.morphList != null && this.model.morphList.Length == this._updateMorphWeightList.Length ) {
					for( int i = 0; i != this.model.morphList.Length; ++i ) {
						this._updateMorphWeightList[i] = this.model.morphList[i]._updatedWeight;
					}
				}

				// RigidBody
				if( this.model.rigidBodyList != null ) {
					for( int i = 0; i != this.model.rigidBodyList.Length; ++i ) {
						if( this.model.rigidBodyList[i] != null ) {
							SetRigidBodyFreezed( i, this.model.rigidBodyList[i].freezed );
						}
					}
				}
			}

			int boneListLength = _boneTransformList.Length;

			int updateFlags = 0;
			updateFlags |= this.model.ikEnabled ? (int)UpdateFlags.IKEnabled : 0;
			updateFlags |= this.model.boneInherenceEnabled ? (int)UpdateFlags.BoneInherenceEnabled : 0;
			updateFlags |= this.model.boneMorphEnabled ? (int)UpdateFlags.BoneMorphEnabled : 0;

			if( this.model != null ) {
				float pphShoulderFixRate = this.model.pphShoulderFixRateImmediately;
				if( pphShoulderFixRate > 0.0f ) {
					updateFlags |= (int)UpdateFlags.PPHShoulderEnabled;
					if( _updateFValues[(int)UpdateFValues.PPHShoulderFixRate] != pphShoulderFixRate ) {
						_updateFValues[(int)UpdateFValues.PPHShoulderFixRate] = pphShoulderFixRate;
						_isDirtyPreUpdate = true;
					}
				}

				MMD4MecanimBone[] boneList = this.model.boneList;
				if( boneList != null ) {
					for( int i = 0; i != boneListLength; ++i ) {
						MMD4MecanimBone bone = boneList[i];
						if( bone != null ) {
							bool positionIsValid = ((_updateUserPositionIsValidList[i >> 5] >> (i & 0x1f)) & 0x01) != 0; // Optimized: Not call function.
							bool rotationIsValid = ((_updateUserRotationIsValidList[i >> 5] >> (i & 0x1f)) & 0x01) != 0; // Optimized: Not call function.
							if( !bone._userPositionIsZero != positionIsValid ) {
								_isDirtyPreUpdate = true;
								if( bone._userPositionIsZero ) {
									_updateUserPositionIsValidList[i >> 5] &= ~(1u << (i & 0x1f));
									_updateUserPositionList[i] = Vector3.zero;
								} else {
									_updateUserPositionIsValidList[i >> 5] |= (1u << (i & 0x1f));
									_updateUserPositionList[i] = bone._userPosition;
								}
							} else if( positionIsValid ) {
								_updateUserPositionList[i] = bone._userPosition;
							}
							if( !bone._userRotationIsIdentity != rotationIsValid ) {
								_isDirtyPreUpdate = true;
								if( bone._userRotationIsIdentity ) {
									_updateUserRotationIsValidList[i >> 5] &= ~(1u << (i & 0x1f));
									_updateUserRotationList[i] = Quaternion.identity;
								} else {
									_updateUserRotationIsValidList[i >> 5] |= (1u << (i & 0x1f));
									_updateUserRotationList[i] = bone._userRotation;
								}
							} else if( rotationIsValid ) {
								_updateUserRotationList[i] = bone._userRotation;
							}
						}
					}
				}
			}

			if( _isDirtyPreUpdate ) {
				for( int i = 0; i != boneListLength; ++i ) {
					_updateBoneFlagsList[i] = 0;
				}
				if( this.model != null ) {
					MMD4MecanimBone[] boneList = this.model.boneList;
					if( boneList != null && boneList.Length == boneListLength ) {
						for( int i = 0; i != boneListLength; ++i ) {
							if( boneList[i] != null ) {
								int humanBodyBones = boneList[i].humanBodyBones;
								if( humanBodyBones != -1 ) {
									switch( humanBodyBones ) {
									case (int)HumanBodyBones.LeftShoulder:
										_updateBoneFlagsList[i] |= (int)UpdateBoneFlags.SkeletonLeftShoulder;
										break;
									case (int)HumanBodyBones.LeftUpperArm:
										_updateBoneFlagsList[i] |= (int)UpdateBoneFlags.SkeletonLeftUpperArm;
										break;
									case (int)HumanBodyBones.RightShoulder:
										_updateBoneFlagsList[i] |= (int)UpdateBoneFlags.SkeletonRightShoulder;
										break;
									case (int)HumanBodyBones.RightUpperArm:
										_updateBoneFlagsList[i] |= (int)UpdateBoneFlags.SkeletonRightUpperArm;
										break;
									}
								}
							}

							bool positionIsValid = ((_updateUserPositionIsValidList[i >> 5] >> (i & 0x1f)) & 0x01) != 0; // Optimized: Not call function.
							bool rotationIsValid = ((_updateUserRotationIsValidList[i >> 5] >> (i & 0x1f)) & 0x01) != 0; // Optimized: Not call function.
							if( positionIsValid ) {
								_updateBoneFlagsList[i] |= (int)UpdateBoneFlags.UserPosition;
							}
							if( rotationIsValid ) {
								_updateBoneFlagsList[i] |= (int)UpdateBoneFlags.UserRotation;
							}
						}
					}
				}

				bool preUpdatedBulletXNA = false;
				#if !NOUSE_BULLETXNA_UNITY
				if( this.bulletMMDModel != null ) {
					preUpdatedBulletXNA = true;
					unchecked {
						this.bulletMMDModel.PreUpdate(
							(uint)updateFlags,
							_updateBoneFlagsList,
							_updateRigidBodyFlagsList,
							_updateIKWeightList,
							_updateMorphWeightList );
					}
				}
				#endif

				if( !preUpdatedBulletXNA && this.mmdModelPtr != IntPtr.Zero ) {
					var gch_updateBoneFlagsList			= MMD4MecanimCommon.MakeGCHValues( _updateBoneFlagsList );
					var gch_updateRigidBodyFlagsList	= MMD4MecanimCommon.MakeGCHValues( _updateRigidBodyFlagsList );
					var gch_updateIKWeightList			= MMD4MecanimCommon.MakeGCHValues( _updateIKWeightList );
					var gch_updateMorphWeightList		= MMD4MecanimCommon.MakeGCHValues( _updateMorphWeightList );

					_PreUpdateMMDModel( this.mmdModelPtr, updateFlags,
					                   gch_updateBoneFlagsList, gch_updateBoneFlagsList.length,
					                   gch_updateRigidBodyFlagsList, gch_updateRigidBodyFlagsList.length,
					                   gch_updateIKWeightList, gch_updateIKWeightList.length,
					                   gch_updateMorphWeightList, gch_updateMorphWeightList.length );

					gch_updateMorphWeightList.Free();
					gch_updateIKWeightList.Free();
					gch_updateRigidBodyFlagsList.Free();
					gch_updateBoneFlagsList.Free();
				}
			}

			for( int i = 0; i != boneListLength; ++i ) {
				unchecked {
					Transform boneTransform = _boneTransformList[i];
					if( boneTransform != null ) {
						if( (_updateBoneFlagsList[i] & (int)UpdateBoneFlags.WorldTransform) != 0 ) {
							_updateBoneTransformList[i] = boneTransform.localToWorldMatrix;
						}
						if( (_updateBoneFlagsList[i] & (int)UpdateBoneFlags.Position) != 0 ) {
							_updateBonePositionList[i] = boneTransform.localPosition;
						}
						if( (_updateBoneFlagsList[i] & (int)UpdateBoneFlags.Rotation) != 0 ) {
							_updateBoneRotationList[i] = boneTransform.localRotation;
						}

						if( _isUpdatedAtLeastOnce ) {
							if( ( _updateBoneFlagsList[i] & (int)UpdateBoneFlags.CheckPosition ) != 0 ) {
								if( _updateBonePositionList[i] != _updateBonePositionList2[i] ) {
									_updateBoneFlagsList[i] |= (int)UpdateBoneFlags.ChangedPosition;
								} else {
									_updateBoneFlagsList[i] &= ~(int)UpdateBoneFlags.ChangedPosition;
								}
							} else {
								_updateBoneFlagsList[i] &= ~(int)UpdateBoneFlags.ChangedPosition;
							}
							if( ( _updateBoneFlagsList[i] & (int)UpdateBoneFlags.CheckRotation ) != 0 ) {
								if( _updateBoneRotationList[i] != _updateBoneRotationList2[i] ) {
									_updateBoneFlagsList[i] |= (int)UpdateBoneFlags.ChangedRotation;
								} else {
									_updateBoneFlagsList[i] &= ~(int)UpdateBoneFlags.ChangedRotation;
								}
							} else {
								_updateBoneFlagsList[i] &= ~(int)UpdateBoneFlags.ChangedRotation;
							}
						} else { // for 1st update.
							if( ( _updateBoneFlagsList[i] & (int)UpdateBoneFlags.CheckPosition ) != 0 ) {
								_updateBoneFlagsList[i] |= (int)UpdateBoneFlags.ChangedPosition;
							}
							if( ( _updateBoneFlagsList[i] & (int)UpdateBoneFlags.CheckRotation ) != 0 ) {
								_updateBoneFlagsList[i] |= (int)UpdateBoneFlags.ChangedRotation;
							}
						}
					}
				}
			}

			_isUpdatedAtLeastOnce = true;

			Matrix4x4 modelTransform = _modelTransform.localToWorldMatrix;

			#if !NOUSE_BULLETXNA_UNITY
			if( this.bulletMMDModel != null ) {
				unchecked {
					this.bulletMMDModel.Update( (uint)updateFlags,
					    _updateIValues, _updateFValues, ref modelTransform,
						_updateBoneFlagsList,
					    _updateBoneTransformList,
					    _updateBonePositionList,
					    _updateBoneRotationList,
						_updateUserPositionList,
						_updateUserRotationList,
					    _updateRigidBodyFlagsList,
						_updateIKWeightList,
					    _updateMorphWeightList );
				}
				return;
			}
			#endif

			if( this.mmdModelPtr != IntPtr.Zero ) {
				var gch_iValues						= MMD4MecanimCommon.MakeGCHValues( _updateIValues );
				var gch_fValues						= MMD4MecanimCommon.MakeGCHValues( _updateFValues );
				var gch_modelTransform				= MMD4MecanimCommon.MakeGCHValue( ref modelTransform );
				var gch_updateBoneFlagsList			= MMD4MecanimCommon.MakeGCHValues( _updateBoneFlagsList );
				var gch_updateBoneTransformList		= MMD4MecanimCommon.MakeGCHValues( _updateBoneTransformList );
				var gch_updateBonePositionList		= MMD4MecanimCommon.MakeGCHValues( _updateBonePositionList );
				var gch_updateBoneRotationList		= MMD4MecanimCommon.MakeGCHValues( _updateBoneRotationList );
				var gch_updateUserPositionList		= MMD4MecanimCommon.MakeGCHValues( _updateUserPositionList );
				var gch_updateUserRotationList		= MMD4MecanimCommon.MakeGCHValues( _updateUserRotationList );
				var gch_updateRigidBodyFlagsList	= MMD4MecanimCommon.MakeGCHValues( _updateRigidBodyFlagsList );
				var gch_updateIKWeightList			= MMD4MecanimCommon.MakeGCHValues( _updateIKWeightList );
				var gch_updateMorphWeightList		= MMD4MecanimCommon.MakeGCHValues( _updateMorphWeightList );

				_UpdateMMDModel( this.mmdModelPtr, updateFlags,
					gch_iValues, gch_iValues.length,
					gch_fValues, gch_fValues.length,
					gch_modelTransform,
					gch_updateBoneFlagsList,
					gch_updateBoneTransformList,
					gch_updateBonePositionList,
					gch_updateBoneRotationList,
					gch_updateUserPositionList,
					gch_updateUserRotationList,
					gch_updateBoneFlagsList.length,
					gch_updateRigidBodyFlagsList, gch_updateRigidBodyFlagsList.length,
					gch_updateIKWeightList, gch_updateIKWeightList.length,
					gch_updateMorphWeightList, gch_updateMorphWeightList.length );

				gch_updateMorphWeightList.Free();
				gch_updateIKWeightList.Free();
				gch_updateRigidBodyFlagsList.Free();
				gch_updateUserRotationList.Free();
				gch_updateUserPositionList.Free();
				gch_updateBoneRotationList.Free();
				gch_updateBonePositionList.Free();
				gch_updateBoneTransformList.Free();
				gch_updateBoneFlagsList.Free();
				gch_modelTransform.Free();
				gch_fValues.Free();
				gch_iValues.Free();
			}
		}
		
		public void LateUpdate( float deltaTime )
		{
			if( _boneTransformList			== null ||
				_updateBoneTransformList	== null ||
				_updateBoneFlagsList		== null ||
				_updateBonePositionList		== null ||
				_updateBoneRotationList		== null ) {
				return;
			}

			if( this.localWorld != null ) {
				this.localWorld.Update( deltaTime );
			}

			int				lateUpdateFlags = 0;
			int[]			lateUpdateBoneFlagsList = null;
			Vector3[]		lateUpdateBonePositionList = null;
			Quaternion[]	lateUpdateBoneRotationList = null;
			int[]			lateUpdateMeshFlagsList = null;

			#if !NOUSE_BULLETXNA_UNITY
			MMD4MecanimInternal.Bullet.MMDModel.UpdateData updateDataBulletXNA = null;
			if( this.bulletMMDModel != null ) {
				updateDataBulletXNA = this.bulletMMDModel.LateUpdate();
				if( updateDataBulletXNA != null ) {
					unchecked {
						lateUpdateFlags				= (int)updateDataBulletXNA.lateUpdateFlags;
						lateUpdateBoneFlagsList		= updateDataBulletXNA.lateUpdateBoneFlagsList;
						lateUpdateBonePositionList	= updateDataBulletXNA.lateUpdateBonePositionList;
						lateUpdateBoneRotationList	= updateDataBulletXNA.lateUpdateBoneRotationList;
						lateUpdateMeshFlagsList		= updateDataBulletXNA.lateUpdateMeshFlagsList;
					}
				}
			}
			#endif

			if( this.mmdModelPtr != IntPtr.Zero ) {
				if( _lateUpdateBoneFlagsList == null ||
				    _lateUpdateBonePositionList == null ||
				    _lateUpdateBoneRotationList == null ) {
					int boneLength = (_boneTransformList != null) ? _boneTransformList.Length : 0;
					_lateUpdateBoneFlagsList = new int[boneLength];
					_lateUpdateBonePositionList = new Vector3[boneLength];
					_lateUpdateBoneRotationList = new Quaternion[boneLength];
				}
				if( _lateUpdateMeshFlagsList == null ) {
					int meshLength = (this.model != null) ? this.model._skinnedMeshCount : 0;
					_lateUpdateMeshFlagsList = new int[meshLength];
				}

				var gch_lateUpdateBoneFlagsList = MMD4MecanimCommon.MakeGCHValues( _lateUpdateBoneFlagsList );
				var gch_lateUpdateBonePositionList = MMD4MecanimCommon.MakeGCHValues( _lateUpdateBonePositionList );
				var gch_lateUpdateBoneRotationList = MMD4MecanimCommon.MakeGCHValues( _lateUpdateBoneRotationList );
				var gch_lateUpdateMeshFlags = MMD4MecanimCommon.MakeGCHValues( _lateUpdateMeshFlagsList );

				// lateUpdateMeshFlags
				lateUpdateFlags = _LateUpdateMMDModel( this.mmdModelPtr,
				    IntPtr.Zero, 0, IntPtr.Zero, 0,
				    gch_lateUpdateBoneFlagsList, gch_lateUpdateBonePositionList, gch_lateUpdateBoneRotationList, _lateUpdateBoneFlagsList.Length,
				    gch_lateUpdateMeshFlags, gch_lateUpdateMeshFlags.length,
				    IntPtr.Zero, 0 );

				gch_lateUpdateMeshFlags.Free();
				gch_lateUpdateBoneRotationList.Free();
				gch_lateUpdateBonePositionList.Free();
				gch_lateUpdateBoneFlagsList.Free();

				lateUpdateBoneFlagsList		= _lateUpdateBoneFlagsList;
				lateUpdateBonePositionList	= _lateUpdateBonePositionList;
				lateUpdateBoneRotationList	= _lateUpdateBoneRotationList;
				lateUpdateMeshFlagsList		= _lateUpdateMeshFlagsList;
			}

			unchecked {
				if( (lateUpdateFlags & (int)LateUpdateFlags.Bone) != 0 &&
					lateUpdateBoneFlagsList != null &&
					lateUpdateBonePositionList != null &&
					lateUpdateBoneRotationList != null ) {
					int boneListLength = _boneTransformList.Length;
					for( int i = 0; i != boneListLength; ++i ) {
						Transform boneTransform = _boneTransformList[i];
						if( boneTransform != null ) {
							if( (lateUpdateBoneFlagsList[i] & (int)LateUpdateBoneFlags.LateUpdated) != 0 ) {
								if( (lateUpdateBoneFlagsList[i] & (int)LateUpdateBoneFlags.Position) != 0 ) {
									boneTransform.localPosition = lateUpdateBonePositionList[i];
									if( (_updateBoneFlagsList[i] & (int)UpdateBoneFlags.CheckPosition) != 0 ) {
										_updateBonePositionList[i] = boneTransform.localPosition; // for ChangePosition check.
									}
								}
								if( (lateUpdateBoneFlagsList[i] & (int)LateUpdateBoneFlags.Rotation) != 0 ) {
									boneTransform.localRotation = lateUpdateBoneRotationList[i];
									if( (_updateBoneFlagsList[i] & (int)UpdateBoneFlags.CheckRotation) != 0 ) {
										_updateBoneRotationList[i] = boneTransform.localRotation; // for ChangeRotation check.
									}
								}
							}
						}
					}
				}
				if( (lateUpdateFlags & (int)LateUpdateFlags.Mesh) != 0 && lateUpdateMeshFlagsList != null ) {
					#if !NOUSE_BULLETXNA_UNITY
					if( this.bulletMMDModel != null && updateDataBulletXNA != null ) {
						MMD4MecanimInternal.Bullet.MMDModel.LateUpdateMeshData[] lateUpdateMeshData = updateDataBulletXNA.lateUpdateMeshDataList;
						int meshCount = model._cloneMeshCount;
						if( lateUpdateMeshData != null && lateUpdateMeshData.Length == meshCount && lateUpdateMeshFlagsList.Length == meshCount ) {
							for( int i = 0; i != meshCount; ++i ) {
								int lateUpdateMeshFlags = lateUpdateMeshFlagsList[i];
								if( ( lateUpdateMeshFlags & (int)(LateUpdateMeshFlags.Vertices | LateUpdateMeshFlags.Normals) ) != 0 ) {
									Vector3[] vertices = null;
									Vector3[] normals = null;
									if( lateUpdateMeshData[i] != null ) {
										if( ( lateUpdateMeshFlags & (int)LateUpdateMeshFlags.Vertices ) != 0 ) {
											vertices = lateUpdateMeshData[i].vertices;
										}
										if( ( lateUpdateMeshFlags & (int)LateUpdateMeshFlags.Normals ) != 0 ) {
											normals = lateUpdateMeshData[i].normals;
										}

										this.model._UploadMeshVertex( i, vertices, normals );
									}
								}
							}
						}
						return;
					}
					#endif

					if( this.mmdModelPtr != IntPtr.Zero ) {
						int meshCount = model._cloneMeshCount;
						for( int i = 0; i != meshCount; ++i ) {
							int lateUpdateMeshFlags = lateUpdateMeshFlagsList[i];
							if( ( lateUpdateMeshFlags & (int)(LateUpdateMeshFlags.Vertices | LateUpdateMeshFlags.Normals) ) != 0 ) {
								MMD4MecanimModel.CloneMesh cloneMesh = model._GetCloneMesh( i );
								if( cloneMesh != null ) {
									Vector3[] vertices = null;
									Vector3[] normals = null;
									if( ( lateUpdateMeshFlags & (int)LateUpdateMeshFlags.Vertices ) != 0 ) {
										vertices = cloneMesh.vertices;
									}
									if( ( lateUpdateMeshFlags & (int)LateUpdateMeshFlags.Normals ) != 0 ) {
										normals = cloneMesh.normals;
									}
									var gch_vertices = MMD4MecanimCommon.MakeGCHValues( vertices );
									var gch_normals = MMD4MecanimCommon.MakeGCHValues( normals );
									int r = _LateUpdateMeshMMDModel( this.mmdModelPtr, i, gch_vertices, gch_normals, gch_vertices.length );
									gch_normals.Free();
									gch_vertices.Free();
									if( r != 0 ) {
										this.model._UploadMeshVertex( i, vertices, normals );
									}
								}
							}
						}
					}
				}
			}
		}
	}
	
	void Awake()
	{
		if( _instance == null ) {
			_instance = this;
		} else {
			if( _instance != this ) {
				Destroy( this.gameObject );
				return;
			}
		}

		_Initialize();
	}
	
	void LateUpdate()
	{
		_InternalUpdate();
	}
	
	void _InternalUpdate()
	{
		if( _isAwaked ) {
			foreach( RigidBody rigidBody in _rigidBodyList ) {
				rigidBody.Update();
			}
			foreach( MMDModel mmdModel in _mmdModelList ) {
				mmdModel.Update();
			}

			#if DEBUG_REMOVE_GLOBALWORLD
			#else
			World globalWorld = this.globalWorld;
			if( globalWorld != null ) {
				globalWorld.Update( Time.deltaTime );
			}
			#endif
			
			foreach( RigidBody rigidBody in _rigidBodyList ) {
				rigidBody.LateUpdate();
			}
			foreach( MMDModel mmdModel in _mmdModelList ) {
				mmdModel.LateUpdate( Time.deltaTime );
			}

			DebugLog();
		}
	}

	void OnDestroy()
	{
		foreach( RigidBody rigidBody in _rigidBodyList ) {
			rigidBody.Destroy();
		}
		foreach( MMDModel mmdModel in _mmdModelList ) {
			mmdModel.Destroy();
		}
		_rigidBodyList.Clear();
		_mmdModelList.Clear();
		if( _globalWorld != null ) {
			_globalWorld.Destroy();
			_globalWorld = null;
		}
		if( _instance == this ) {
			_instance = null;
		}

		if( _isUseBulletXNA ) {
			#if FORCE_BULLETXNA_INITIALIZEENGINE
			_FinalizeEngine();
			#endif
		} else {
			// Finalize Engine.
			_FinalizeEngine();
		}
	}

	IEnumerator DelayedAwake()
	{
		yield return new WaitForEndOfFrame();
		_isAwaked = true;
		yield break;
	}
	
	private void _ActivateGlobalWorld()
	{
		if( _globalWorld == null ) {
			_globalWorld = new World();
		}
		if( this.globalWorldProperty == null ) {
			this.globalWorldProperty = new WorldProperty();
		}
		if( _globalWorld.isExpired ) {
			_globalWorld.Create( this.globalWorldProperty );
		}
	}
	
	public static void DebugLog()
	{
		#if !NOUSE_BULLETXNA_UNITY
		if( _isUseBulletXNA ) {
			return;
		}
		#endif
		IntPtr debugLogPtr = _DebugLog( 1 );
		if( debugLogPtr != IntPtr.Zero ) {
			Debug.Log( Marshal.PtrToStringUni( debugLogPtr ) );
		}
	}
	
	public MMDModel CreateMMDModel( MMD4MecanimModel model )
	{
		MMDModel mmdModel = new MMDModel();
		if( !mmdModel.Create( model ) ) {
			Debug.LogError( "CreateMMDModel: Failed " + model.gameObject.name );
			return null;
		}

		_mmdModelList.Add( mmdModel );
		return mmdModel;
	}
	
	public void DestroyMMDModel( MMDModel mmdModel )
	{
		for( int i = 0; i < _mmdModelList.Count; ++i ) {
			if( _mmdModelList[i] == mmdModel ) {
				mmdModel.Destroy();
				_mmdModelList.Remove( mmdModel );
				return;
			}
		}
	}

	public RigidBody CreateRigidBody( MMD4MecanimRigidBody rigidBody )
	{
		RigidBody r = new RigidBody();
		if( !r.Create( rigidBody ) ) {
			return null;
		}
		
		_rigidBodyList.Add( r );
		return r;
	}
	
	public void DestroyRigidBody( RigidBody rigidBody )
	{
		for( int i = 0; i < _rigidBodyList.Count; ++i ) {
			if( _rigidBodyList[i] == rigidBody ) {
				rigidBody.Destroy();
				_rigidBodyList.Remove( rigidBody );
				return;
			}
		}
	}

	//--------------------------------------------------------------------------------------------------------------------------------

	static void _InitializeEngine()
	{
		MMD4MecanimBulletPhysicsInitialize();
	}

	static void _FinalizeEngine()
	{
		MMD4MecanimBulletPhysicsFinalize();
	}

	static IntPtr _DebugLog( int clanupFlag )
	{
		return MMD4MecanimBulletPhysicsDebugLog( clanupFlag );
	}

	//--------------------------------------------------------------------------------------------------------------------------------

	static IntPtr _CreateWorld( IntPtr iValues, int iValueLength, IntPtr fValues, int fValueLength )
	{
		return MMD4MecanimBulletPhysicsCreateWorld( iValues, iValueLength, fValues, fValueLength );
	}

	static void _ConfigWorld( IntPtr worldPtr, IntPtr iValues, int iValueLength, IntPtr fValues, int fValueLength )
	{
		MMD4MecanimBulletPhysicsConfigWorld( worldPtr, iValues, iValueLength, fValues, fValueLength );
	}

	static void _DestroyWorld( IntPtr worldPtr )
	{
		MMD4MecanimBulletPhysicsDestroyWorld( worldPtr );
	}

	static void _UpdateWorld( IntPtr worldPtr, float deltaTime, IntPtr iValues, int iValueLength, IntPtr fValues, int fValueLength )
	{
		MMD4MecanimBulletPhysicsUpdateWorld( worldPtr, deltaTime, iValues, iValueLength, fValues, fValueLength );
	}

	//--------------------------------------------------------------------------------------------------------------------------------

	static IntPtr _CreateRigidBody( IntPtr iValues, int iValueLength, IntPtr fValues, int fValueLength )
	{
		return MMD4MecanimBulletPhysicsCreateRigidBody( iValues, iValueLength, fValues, fValueLength );
	}

	static void _ConfigRigidBody( IntPtr rigidBodyPtr, IntPtr iValues, int iValueLength, IntPtr fValues, int fValueLength )
	{
		MMD4MecanimBulletPhysicsConfigRigidBody( rigidBodyPtr, iValues, iValueLength, fValues, fValueLength );
	}

	static void _DestroyRigidBody( IntPtr rigidBodyPtr )
	{
		MMD4MecanimBulletPhysicsDestroyRigidBody( rigidBodyPtr );
	}
	
	static void _JoinWorldRigidBody( IntPtr worldPtr, IntPtr rigidBodyPtr )
	{
		MMD4MecanimBulletPhysicsJoinWorldRigidBody( worldPtr, rigidBodyPtr );
	}
	
	static void _LeaveWorldRigidBody( IntPtr rigidBodyPtr )
	{
		MMD4MecanimBulletPhysicsLeaveWorldRigidBody( rigidBodyPtr );
	}
	
	static void _ResetWorldRigidBody( IntPtr rigidBodyPtr )
	{
		MMD4MecanimBulletPhysicsResetWorldRigidBody( rigidBodyPtr );
	}
	
	static void _UpdateRigidBody( IntPtr rigidBodyPtr, int updateFlags, IntPtr iValues, int iValueLength, IntPtr fValues, int fValueLength )
	{
		MMD4MecanimBulletPhysicsUpdateRigidBody( rigidBodyPtr, updateFlags, iValues, iValueLength, fValues, fValueLength );
	}
	
	static int _LateUpdateRigidBody( IntPtr rigidBodyPtr, IntPtr iValues, int iValueLength, IntPtr fValues, int fValueLength )
	{
		return MMD4MecanimBulletPhysicsLateUpdateRigidBody( rigidBodyPtr, iValues, iValueLength, fValues, fValueLength );
	}

	//--------------------------------------------------------------------------------------------------------------------------------

	static IntPtr _CreateMMDModel(
		IntPtr mmdModelBytes, int mmdModelLength,
		IntPtr mmdIndexBytes, int mmdIndexLength,
		IntPtr mmdVertexBytes, int mmdVertexLength,
		IntPtr iMeshValues, int meshLength,
		IntPtr iValues, int iValueLength, IntPtr fValues, int fValueLength )
	{
		return MMD4MecanimBulletPhysicsCreateMMDModel(
			mmdModelBytes, mmdModelLength, mmdIndexBytes, mmdIndexLength, mmdVertexBytes, mmdVertexLength, iMeshValues, meshLength,
			iValues, iValueLength, fValues, fValueLength );
	}

	static int _UploadMeshMMDModel(
		IntPtr mmdModelPtr, int meshID,
		IntPtr vertices, IntPtr normals, IntPtr boneWeights, int vertexLength,
		IntPtr bindposes, int boneLength )
	{
		return MMD4MecanimBulletPhysicsUploadMeshMMDModel(
			mmdModelPtr, meshID, vertices, normals, boneWeights, vertexLength,
			bindposes, boneLength );
	}

	static void _ConfigMMDModel( IntPtr mmdModelPtr, IntPtr iValues, int iValueLength, IntPtr fValues, int fValueLength )
	{
		MMD4MecanimBulletPhysicsConfigMMDModel( mmdModelPtr, iValues, iValueLength, fValues, fValueLength );
	}

	static void _DestroyMMDModel( IntPtr mmdModelPtr )
	{
		MMD4MecanimBulletPhysicsDestroyMMDModel( mmdModelPtr );
	}

	static void _JoinWorldMMDModel( IntPtr worldPtr, IntPtr mmdModelPtr )
	{
		MMD4MecanimBulletPhysicsJoinWorldMMDModel( worldPtr, mmdModelPtr );
	}

	static void _LeaveWorldMMDModel( IntPtr mmdModelPtr )
	{
		MMD4MecanimBulletPhysicsLeaveWorldMMDModel( mmdModelPtr );
	}

	static void _ResetWorldMMDModel( IntPtr mmdModelPtr )
	{
		MMD4MecanimBulletPhysicsResetWorldMMDModel( mmdModelPtr );
	}

	static int  _PreUpdateMMDModel(
		IntPtr mmdModelPtr, int updateFlags,
		IntPtr iBoneValues, int boneLength,
		IntPtr iRigidBodyValues, int rigidBodyLength,
		IntPtr ikWeights, int ikLength,
		IntPtr morphWeights, int morphLength )
	{
		return MMD4MecanimBulletPhysicsPreUpdateMMDModel(
			mmdModelPtr, updateFlags,
			iBoneValues, boneLength,
			iRigidBodyValues, rigidBodyLength,
			ikWeights, ikLength,
			morphWeights, morphLength );
	}

	static void _UpdateMMDModel(
		IntPtr mmdModelPtr, int updateFlags, IntPtr iValues, int iValueLength, IntPtr fValues, int fValueLength, IntPtr modelTransform,
		IntPtr iBoneValues, IntPtr boneTransforms, IntPtr boneLocalPositions, IntPtr boneLocalRotations, IntPtr boneUserPositions, IntPtr boneUserRotations, int boneLength,
		IntPtr iRigidBodyValues, int rigidBodyLength,
		IntPtr ikWeights, int ikLength,
		IntPtr morphWeights, int morphLength )
	{
		MMD4MecanimBulletPhysicsUpdateMMDModel(
			mmdModelPtr, updateFlags, iValues, iValueLength, fValues, fValueLength, modelTransform,
			iBoneValues, boneTransforms, boneLocalPositions, boneLocalRotations, boneUserPositions, boneUserRotations, boneLength,
			iRigidBodyValues, rigidBodyLength,
			ikWeights, ikLength,
			morphWeights, morphLength );
	}

	static int  _LateUpdateMMDModel(
		IntPtr mmdModelPtr, IntPtr iValues, int iValueLength, IntPtr fValues, int fValueLength,
		IntPtr iBoneValues, IntPtr bonePositions, IntPtr boneRotations, int boneLength,
		IntPtr iMeshValues, int meshLength,
		IntPtr morphWeigts, int morphLength )
	{
		return MMD4MecanimBulletPhysicsLateUpdateMMDModel(
			mmdModelPtr, iValues, iValueLength, fValues, fValueLength,
			iBoneValues, bonePositions, boneRotations, boneLength,
			iMeshValues, meshLength,
			morphWeigts, morphLength );
	}

	static int _LateUpdateMeshMMDModel( IntPtr mmdModelPtr, int meshID, IntPtr vertices, IntPtr normals, int vertexLength )
	{
		return MMD4MecanimBulletPhysicsLateUpdateMeshMMDModel( mmdModelPtr, meshID, vertices, normals, vertexLength );
	}

	static void _ConfigBoneMMDModel( IntPtr mmdModelPtr, int boneID, IntPtr iValues, int iValueLength, IntPtr fValues, int fValueLength )
	{
		MMD4MecanimBulletPhysicsConfigBoneMMDModel( mmdModelPtr, boneID, iValues, iValueLength, fValues, fValueLength );
	}

	static void _ConfigRigidBodyMMDModel( IntPtr mmdModelPtr, int rigidBodyID, IntPtr iValues, int iValueLength, IntPtr fValues, int fValueLength )
	{
		MMD4MecanimBulletPhysicsConfigRigidBodyMMDModel( mmdModelPtr, rigidBodyID, iValues, iValueLength, fValues, fValueLength );
	}

	static void _ConfigJointMMDModel( IntPtr mmdModelPtr, int jointID, IntPtr iValues, int iValueLength, IntPtr fValues, int fValueLength )
	{
		MMD4MecanimBulletPhysicsConfigJointMMDModel( mmdModelPtr, jointID, iValues, iValueLength, fValues, fValueLength );
	}

#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
	[DllImport ("MMD4MecanimBulletPhysics")]
	public static extern void MMD4MecanimBulletPhysicsInitialize();
	[DllImport ("MMD4MecanimBulletPhysics")]
	public static extern void MMD4MecanimBulletPhysicsFinalize();
	[DllImport ("MMD4MecanimBulletPhysics")]
	public static extern IntPtr MMD4MecanimBulletPhysicsDebugLog( int clanupFlag );

	[DllImport ("MMD4MecanimBulletPhysics")]
	public static extern IntPtr MMD4MecanimBulletPhysicsCreateWorld( IntPtr iValues, int iValueLength, IntPtr fValues, int fValueLength );
	[DllImport ("MMD4MecanimBulletPhysics")]
	public static extern void MMD4MecanimBulletPhysicsConfigWorld( IntPtr worldPtr, IntPtr iValues, int iValueLength, IntPtr fValues, int fValueLength );
	[DllImport ("MMD4MecanimBulletPhysics")]
	public static extern void MMD4MecanimBulletPhysicsDestroyWorld( IntPtr worldPtr );
	[DllImport ("MMD4MecanimBulletPhysics")]
	public static extern void MMD4MecanimBulletPhysicsUpdateWorld( IntPtr worldPtr, float deltaTime, IntPtr iValues, int iValueLength, IntPtr fValues, int fValueLength );

	[DllImport ("MMD4MecanimBulletPhysics")]
	public static extern IntPtr MMD4MecanimBulletPhysicsCreateRigidBody( IntPtr iValues, int iValueLength, IntPtr fValues, int fValueLength );
	[DllImport ("MMD4MecanimBulletPhysics")]
	public static extern void MMD4MecanimBulletPhysicsConfigRigidBody( IntPtr rigidBodyPtr, IntPtr iValues, int iValueLength, IntPtr fValues, int fValueLength );
	[DllImport ("MMD4MecanimBulletPhysics")]
	public static extern void MMD4MecanimBulletPhysicsDestroyRigidBody( IntPtr rigidBodyPtr );
	[DllImport ("MMD4MecanimBulletPhysics")]
	public static extern void MMD4MecanimBulletPhysicsJoinWorldRigidBody( IntPtr worldPtr, IntPtr rigidBodyPtr );
	[DllImport ("MMD4MecanimBulletPhysics")]
	public static extern void MMD4MecanimBulletPhysicsLeaveWorldRigidBody( IntPtr rigidBodyPtr );
	[DllImport ("MMD4MecanimBulletPhysics")]
	public static extern void MMD4MecanimBulletPhysicsResetWorldRigidBody( IntPtr rigidBodyPtr );
	[DllImport ("MMD4MecanimBulletPhysics")]
	public static extern void MMD4MecanimBulletPhysicsUpdateRigidBody( IntPtr rigidBodyPtr, int updateFlags, IntPtr iValues, int iValueLength, IntPtr fValues, int fValueLength );
	[DllImport ("MMD4MecanimBulletPhysics")]
	public static extern int MMD4MecanimBulletPhysicsLateUpdateRigidBody( IntPtr rigidBodyPtr, IntPtr iValues, int iValueLength, IntPtr fValues, int fValueLength );

	[DllImport ("MMD4MecanimBulletPhysics")]
	public static extern IntPtr MMD4MecanimBulletPhysicsCreateMMDModel(
		IntPtr mmdModelBytes, int mmdModelLength,
		IntPtr mmdIndexBytes, int mmdIndexLength,
		IntPtr mmdVertexBytes, int mmdVertexLength,
		IntPtr iMeshValues, int meshLength,
		IntPtr iValues, int iValueLength, IntPtr fValues, int fValueLength );
	[DllImport ("MMD4MecanimBulletPhysics")]
	public static extern int  MMD4MecanimBulletPhysicsUploadMeshMMDModel(
		IntPtr mmdModelPtr, int meshID,
		IntPtr vertices, IntPtr normals, IntPtr boneWeights, int vertexLength,
		IntPtr bindposes, int boneLength );
	[DllImport ("MMD4MecanimBulletPhysics")]
	public static extern void MMD4MecanimBulletPhysicsConfigMMDModel( IntPtr mmdModelPtr, IntPtr iValues, int iValueLength, IntPtr fValues, int fValueLength );
	[DllImport ("MMD4MecanimBulletPhysics")]
	public static extern void MMD4MecanimBulletPhysicsDestroyMMDModel( IntPtr mmdModelPtr );
	[DllImport ("MMD4MecanimBulletPhysics")]
	public static extern void MMD4MecanimBulletPhysicsJoinWorldMMDModel( IntPtr worldPtr, IntPtr mmdModelPtr );
	[DllImport ("MMD4MecanimBulletPhysics")]
	public static extern void MMD4MecanimBulletPhysicsLeaveWorldMMDModel( IntPtr mmdModelPtr );
	[DllImport ("MMD4MecanimBulletPhysics")]
	public static extern void MMD4MecanimBulletPhysicsResetWorldMMDModel( IntPtr mmdModelPtr );
	[DllImport ("MMD4MecanimBulletPhysics")]
	public static extern int  MMD4MecanimBulletPhysicsPreUpdateMMDModel(
		IntPtr mmdModelPtr, int updateFlags,
		IntPtr iBoneValues, int boneLength,
		IntPtr iRigidBodyValues, int rigidBodyLength,
		IntPtr ikWeights, int ikLength,
		IntPtr morphWeights, int morphLength );
	[DllImport ("MMD4MecanimBulletPhysics")]
	public static extern void MMD4MecanimBulletPhysicsUpdateMMDModel(
		IntPtr mmdModelPtr, int updateFlags, IntPtr iValues, int iValueLength, IntPtr fValues, int fValueLength, IntPtr modelTransform,
		IntPtr iBoneValues, IntPtr boneTransforms, IntPtr boneLocalPositions, IntPtr boneLocalRotations, IntPtr boneUserPositions, IntPtr boneUserRotations, int boneLength,
		IntPtr iRigidBodyValues, int rigidBodyLength,
		IntPtr ikWeights, int ikLength,
		IntPtr morphWeights, int morphLength );
	[DllImport ("MMD4MecanimBulletPhysics")]
	public static extern int  MMD4MecanimBulletPhysicsLateUpdateMMDModel(
		IntPtr mmdModelPtr, IntPtr iValues, int iValueLength, IntPtr fValues, int fValueLength,
		IntPtr iBoneValues, IntPtr bonePositions, IntPtr boneRotations, int boneLength,
		IntPtr iMeshValues, int meshLength,
		IntPtr morphWeigts, int morphLength );
	[DllImport ("MMD4MecanimBulletPhysics")]
	public static extern int MMD4MecanimBulletPhysicsLateUpdateMeshMMDModel( IntPtr mmdModelPtr, int meshID, IntPtr vertices, IntPtr normals, int vertexLength );
	[DllImport ("MMD4MecanimBulletPhysics")]
	public static extern void MMD4MecanimBulletPhysicsConfigBoneMMDModel( IntPtr mmdModelPtr, int boneID, IntPtr iValues, int iValueLength, IntPtr fValues, int fValueLength );
	[DllImport ("MMD4MecanimBulletPhysics")]
	public static extern void MMD4MecanimBulletPhysicsConfigRigidBodyMMDModel( IntPtr mmdModelPtr, int rigidBodyID, IntPtr iValues, int iValueLength, IntPtr fValues, int fValueLength );
	[DllImport ("MMD4MecanimBulletPhysics")]
	public static extern void MMD4MecanimBulletPhysicsConfigJointMMDModel( IntPtr mmdModelPtr, int jointID, IntPtr iValues, int iValueLength, IntPtr fValues, int fValueLength );
#else
	public static void MMD4MecanimBulletPhysicsInitialize() {}
	public static void MMD4MecanimBulletPhysicsFinalize() {}
	public static IntPtr MMD4MecanimBulletPhysicsDebugLog( int cleanupFlag ) { return IntPtr.Zero; }

	public static IntPtr MMD4MecanimBulletPhysicsCreateWorld( IntPtr iValues, int iValueLength, IntPtr fValues, int fValueLength ) { return IntPtr.Zero; }
	public static void MMD4MecanimBulletPhysicsConfigWorld( IntPtr worldPtr, IntPtr iValues, int iValueLength, IntPtr fValues, int fValueLength ) {}
	public static void MMD4MecanimBulletPhysicsDestroyWorld( IntPtr worldPtr ) {}
	public static void MMD4MecanimBulletPhysicsUpdateWorld( IntPtr worldPtr, float deltaTime, IntPtr iValues, int iValueLength, IntPtr fValues, int fValueLength ) {}

	public static IntPtr MMD4MecanimBulletPhysicsCreateRigidBody( IntPtr iValues, int iValueLength, IntPtr fValues, int fValueLength ) { return IntPtr.Zero; }
	public static void MMD4MecanimBulletPhysicsConfigRigidBody( IntPtr rigidBodyPtr, IntPtr iValues, int iValueLength, IntPtr fValues, int fValueLength ) {}
	public static void MMD4MecanimBulletPhysicsDestroyRigidBody( IntPtr rigidBodyPtr ) {}
	public static void MMD4MecanimBulletPhysicsJoinWorldRigidBody( IntPtr worldPtr, IntPtr rigidBodyPtr ) {}
	public static void MMD4MecanimBulletPhysicsLeaveWorldRigidBody( IntPtr rigidBodyPtr ) {}
	public static void MMD4MecanimBulletPhysicsResetWorldRigidBody( IntPtr rigidBodyPtr ) {}
	public static void MMD4MecanimBulletPhysicsUpdateRigidBody( IntPtr rigidBodyPtr, int updateFlags, IntPtr iValues, int iValueLength, IntPtr fValues, int fValueLength ) {}
	public static int MMD4MecanimBulletPhysicsLateUpdateRigidBody( IntPtr rigidBodyPtr, IntPtr iValues, int iValueLength, IntPtr fValues, int fValueLength ) { return 0; }

	public static IntPtr MMD4MecanimBulletPhysicsCreateMMDModel(
		IntPtr mmdModelBytes, int mmdModelLength,
		IntPtr mmdIndexBytes, int mmdIndexLength,
		IntPtr mmdVertexBytes, int mmdVertexLength,
		IntPtr iMeshValues, int meshLength,
		IntPtr iValues, int iValueLength, IntPtr fValues, int fValueLength ) { return IntPtr.Zero; }
	public static int  MMD4MecanimBulletPhysicsUploadMeshMMDModel(
		IntPtr mmdModelPtr, int meshID,
		IntPtr vertices, IntPtr normals, IntPtr boneWeights, int vertexLength,
		IntPtr bindposes, int boneLength ) { return 0; }
	public static void MMD4MecanimBulletPhysicsConfigMMDModel( IntPtr mmdModelPtr, IntPtr iValues, int iValueLength, IntPtr fValues, int fValueLength ) {}
	public static void MMD4MecanimBulletPhysicsDestroyMMDModel( IntPtr mmdModelPtr ) {}
	public static void MMD4MecanimBulletPhysicsJoinWorldMMDModel( IntPtr worldPtr, IntPtr mmdModelPtr ) {}
	public static void MMD4MecanimBulletPhysicsLeaveWorldMMDModel( IntPtr mmdModelPtr ) {}
	public static void MMD4MecanimBulletPhysicsResetWorldMMDModel( IntPtr mmdModelPtr ) {}
	public static int  MMD4MecanimBulletPhysicsPreUpdateMMDModel(
		IntPtr mmdModelPtr, int updateFlags,
		IntPtr iBoneValues, int boneLength,
		IntPtr iRigidBodyValues, int rigidBodyLength,
		IntPtr ikWeights, int ikLength,
		IntPtr morphWeights, int morphLength ) { return 0; }
	public static void MMD4MecanimBulletPhysicsUpdateMMDModel(
		IntPtr mmdModelPtr, int updateFlags, IntPtr iValues, int iValueLength, IntPtr fValues, int fValueLength, IntPtr modelTransform,
		IntPtr iBoneValues, IntPtr boneTransforms, IntPtr boneLocalPositions, IntPtr boneLocalRotations, IntPtr boneUserPositions, IntPtr boneUserRotations, int boneLength,
		IntPtr iRigidBodyValues, int rigidBodyLength,
		IntPtr ikWeights, int ikLength,
		IntPtr morphWeights, int morphLength ) {}
	public static int  MMD4MecanimBulletPhysicsLateUpdateMMDModel(
		IntPtr mmdModelPtr, IntPtr iValues, int iValueLength, IntPtr fValues, int fValueLength,
		IntPtr iBoneValues, IntPtr bonePositions, IntPtr boneRotations, int boneLength,
		IntPtr iMeshValues, int meshLength,
		IntPtr morphWeigts, int morphLength ) { return 0; }
	public static int  MMD4MecanimBulletPhysicsLateUpdateMeshMMDModel( IntPtr mmdModelPtr, int meshID, IntPtr vertices, IntPtr normals, int vertexLength ) { return 0; }
	public static void MMD4MecanimBulletPhysicsConfigBoneMMDModel( IntPtr mmdModelPtr, int boneID, IntPtr iValues, int iValueLength, IntPtr fValues, int fValueLength ) {}
	public static void MMD4MecanimBulletPhysicsConfigRigidBodyMMDModel( IntPtr mmdModelPtr, int rigidBodyID, IntPtr iValues, int iValueLength, IntPtr fValues, int fValueLength ) {}
	public static void MMD4MecanimBulletPhysicsConfigJointMMDModel( IntPtr mmdModelPtr, int jointID, IntPtr iValues, int iValueLength, IntPtr fValues, int fValueLength ) {}
#endif
}
