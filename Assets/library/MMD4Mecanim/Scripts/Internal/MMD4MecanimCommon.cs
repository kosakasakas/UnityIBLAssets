using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public static class MMD4MecanimCommon
{
	//------------------------------------------------------------------------------------------------------------------------

	public static readonly Color MMDLit_centerAmbient = new Color(0.5f, 0.5f, 0.5f);
	public static readonly Color MMDLit_centerAmbientInv = new Color(1.0f / 0.5f, 1.0f / 0.5f, 1.0f / 0.5f);
	public static readonly Color MMDLit_globalLighting = new Color(0.6f, 0.6f, 0.6f);
	public static readonly float MMDLit_edgeScale = 0.001f;

	public static Color MMDLit_GetTempAmbientL( Color ambient )
	{
		unchecked {
			Color tempColor = MMDLit_centerAmbient - ambient;
			tempColor.r = Mathf.Max( tempColor.r, 0.0f );
			tempColor.g = Mathf.Max( tempColor.g, 0.0f );
			tempColor.b = Mathf.Max( tempColor.b, 0.0f );
			tempColor.a = 0.0f;
			return tempColor * MMDLit_centerAmbientInv.r;
		}
	}

	public static Color MMDLit_GetTempAmbient( Color globalAmbient, Color ambient )
	{
		unchecked {
			Color tempColor = globalAmbient * (Color.white - MMDLit_GetTempAmbientL(ambient));
			tempColor.a = 0.0f;
			return tempColor;
		}
	}
	
	public static Color MMDLit_GetTempDiffuse( Color globalAmbient, Color ambient, Color diffuse )
	{
		unchecked {
			Color tempColor = ambient + diffuse * MMDLit_globalLighting.r;
			tempColor.r = Mathf.Min( tempColor.r, 1.0f );
			tempColor.g = Mathf.Min( tempColor.g, 1.0f );
			tempColor.b = Mathf.Min( tempColor.b, 1.0f );
			tempColor -= MMDLit_GetTempAmbient(globalAmbient, ambient);
			tempColor.r = Mathf.Max( tempColor.r, 0.0f );
			tempColor.g = Mathf.Max( tempColor.g, 0.0f );
			tempColor.b = Mathf.Max( tempColor.b, 0.0f );
			tempColor.a = 0.0f;
			return tempColor;
		}
	}

	//------------------------------------------------------------------------------------------------------------------------

	public static void WeakSetMaterialFloat( Material m, string name, float v )
	{
		if( m.GetFloat( name ) != v ) {
			m.SetFloat( name, v );
		}
	}

	public static void WeakSetMaterialVector( Material m, string name, Vector4 v )
	{
		if( m.GetVector( name ) != v ) {
			m.SetVector( name, v );
		}
	}

	public static void WeakSetMaterialColor( Material m, string name, Color v )
	{
		if( m.GetColor( name ) != v ) {
			m.SetColor( name, v );
		}
	}

	//------------------------------------------------------------------------------------------------------------------------

	public static Transform WeakAddChildTransform( Transform parentTransform, string name )
	{
		GameObject r = WeakAddChildGameObject( (parentTransform != null) ? parentTransform.gameObject : null, name );
		return (r != null) ? r.transform : null;
	}

	public static GameObject WeakAddChildGameObject( GameObject parentGameObject, string name )
	{
		if( parentGameObject != null && !string.IsNullOrEmpty(name) ) {
			foreach( Transform childTransform in parentGameObject.transform ) {
				if( childTransform.name == name ) {
					return childTransform.gameObject;
				}
			}
		}

		GameObject go = new GameObject( (name != null) ? name : "" );
		if( parentGameObject != null ) {
			go.transform.parent = parentGameObject.transform;
		}
		go.layer = parentGameObject.layer;
		go.transform.localPosition = Vector3.zero;
		go.transform.localRotation = Quaternion.identity;
		go.transform.localScale = Vector3.one;
		return go;
	}

	//------------------------------------------------------------------------------------------------------------------------

	public struct Version
	{
		public int major, minor, revision;

		public bool LaterThan( int major )
		{
			return this.major >= major;
		}

		public bool LaterThan( int major, int minor )
		{
			if( this.major < major ) {
				return false;
			} else if( this.major > major ) {
				return true;
			}

			return this.minor >= minor;
		}

		public bool LaterThan( int major, int minor, int revision )
		{
			if( this.major < major ) {
				return false;
			} else if( this.major > major ) {
				return true;
			}

			if( this.minor < minor ) {
				return false;
			} else if( this.minor > minor ) {
				return true;
			}

			return this.revision >= revision;
		}
	}
	
	public static Version GetUnityVersion()
	{
		Version version = new Version();
		string versionStr = Application.unityVersion;
		int pos0 = versionStr.IndexOf( "." );
		int pos1 = (pos0 >= 0) ? versionStr.IndexOf( ".", pos0 + 1 ) : -1;
		if( pos1 >= 0 ) {
			version.major = MMD4MecanimCommon.ToInt( versionStr, 0, pos0 );
			version.minor = MMD4MecanimCommon.ToInt( versionStr, pos0 + 1, pos1 - (pos0 + 1) );
			version.revision = MMD4MecanimCommon.ToInt( versionStr, pos1 + 1, versionStr.Length - (pos1 + 1) );
		} else if( pos0 >= 0 ) {
			version.major = MMD4MecanimCommon.ToInt( versionStr, 0, pos0 );
			version.minor = MMD4MecanimCommon.ToInt( versionStr, pos0 + 1, versionStr.Length - (pos0 + 1) );
		} else {
			version.major = MMD4MecanimCommon.ToInt( versionStr );
		}
		
		return version;
	}

	public static GameObject[] GetChildrenRecursivery( GameObject gameObject )
	{
		List<GameObject> children = new List<GameObject>();
		if( gameObject != null ) {
			_GetChildrenRecursivery( children, gameObject );
		}

		return children.ToArray();
	}

	private static void _GetChildrenRecursivery( List<GameObject> children, GameObject gameObject )
	{
		foreach( Transform child in gameObject.transform ) {
			children.Add( child.gameObject );
			_GetChildrenRecursivery( children, child.gameObject );
		}
	}

	public static void IgnoreCollisionRecursivery( GameObject gameObject, Collider targetCollider )
	{
		if( gameObject != null && targetCollider != null ) {
			if( gameObject != targetCollider.gameObject ) {
				Collider collider = gameObject.GetComponent< Collider >();
				if( collider != null ) {
					if( collider.enabled && targetCollider.enabled ) {
						Physics.IgnoreCollision( collider, targetCollider );
					}
				}
			}
			foreach( Transform child in gameObject.transform ) {
				IgnoreCollisionRecursivery( child.gameObject, targetCollider );
			}
		}
	}

	public static void IgnoreCollisionRecursivery( Collider collider, Collider targetCollider )
	{
		if( collider != null && targetCollider != null ) {
			IgnoreCollisionRecursivery( collider.gameObject, targetCollider );
		}
	}
	
	public static bool ContainsNameInParents( GameObject gameObject, string name )
	{
		if( gameObject == null ) {
			return false;
		}
		
		for(;;) {
			if( gameObject.name.Contains( name ) ) {
				return true;
			}
			if( gameObject.transform.parent == null ) {
				break;
			}

			gameObject = gameObject.transform.parent.gameObject;
		}
		
		return false;
	}
	
	//----------------------------------------------------------------------------------------------------------------
	
	public static int MurmurHash32( string name )
	{
		return MurmurHash32( name, 0xabadcafe );
	}
	
	public static int MurmurHash32( string name, uint seed )
	{
		byte[] bytes = System.Text.Encoding.UTF8.GetBytes(name);
		return MurmurHash32( bytes, 0, bytes.Length, seed );
	}

	public static int MurmurHash32( byte[] bytes, int pos, int len )
	{
		return MurmurHash32( bytes, pos, len, 0xabadcafe );
	}
	
	static uint mmh3_fmix32( uint h )
	{
		h ^= h >> 16;
		h *= 0x85ebca6b;
		h ^= h >> 13;
		h *= 0xc2b2ae35;
		h ^= h >> 16;
		return h;
	}
	
	public static int MurmurHash32( byte[] bytes, int pos, int len, uint seed )
	{
		unchecked {
			int nblocks = len / 4;
			
			uint h1 = (uint)seed;
			
			uint c1 = (uint)0xcc9e2d51;
			uint c2 = (uint)0x1b873593;
			
			// body
			for( int i = 0, n = nblocks * 4; i < n; i += 4 ) {
				uint k1 = ((uint)bytes[pos + i + 0])
						| ((uint)bytes[pos + i + 1] << 8)
						| ((uint)bytes[pos + i + 2] << 16)
						| ((uint)bytes[pos + i + 3] << 24);
				
				k1 *= c1;
				k1 = (k1 << 15) | (k1 >> (32 - 15));
				k1 *= c2;
				
				h1 ^= k1;
				h1 = (h1 << 13) | (h1 >> (32 - 13));
				h1 = h1 * 5 + 0xe6546b64;
			}
			
			// tail
			if( (len & 3) != 0 ) {
				uint k1 = 0;
				if( (len & 3) >= 3 ) {
					k1 ^= (uint)bytes[pos + nblocks * 4 + 2] << 16;
				}
				if( (len & 3) >= 2 ) {
					k1 ^= (uint)bytes[pos + nblocks * 4 + 1] << 8;
				}
				if( (len & 3) >= 1 ) {
					k1 ^= (uint)bytes[pos + nblocks * 4 + 0];
				}
			    k1 *= c1;
				k1 = (k1 << 15) | (k1 >> (32 - 15));
				k1 *= c2;
				h1 ^= k1;
			}
			
			h1 ^= (uint)len;
			h1 = mmh3_fmix32(h1);
			return (int)h1;
		}
	}
	
	//----------------------------------------------------------------------------------------------------------------

	public static GCHValue<T> MakeGCHValue<T>( ref T value )
	{
		return new GCHValue<T>( ref value );
	}

	public static GCHValues<T> MakeGCHValues<T>( T[] values )
	{
		return new GCHValues<T>( values );
	}

	//----------------------------------------------------------------------------------------------------------------

	public struct GCHValue<T>
	{
		GCHandle _gch_value;
		IntPtr _valuePtr;

		public GCHValue( ref T value )
		{
			_gch_value = GCHandle.Alloc(value, GCHandleType.Pinned);
			_valuePtr = _gch_value.AddrOfPinnedObject();
		}

		public static implicit operator IntPtr(GCHValue<T> v)
		{
			return v._valuePtr;
		}

		public void Free()
		{
			if( _valuePtr != IntPtr.Zero ) {
				_valuePtr = IntPtr.Zero;
				_gch_value.Free();
			}
		}
	}

	public struct GCHValues<T>
	{
		GCHandle _gch_values;
		IntPtr _valuesPtr;
		public int length;

		public GCHValues( T[] values )
		{
			if( values != null ) {
				_gch_values = GCHandle.Alloc(values, GCHandleType.Pinned);
				_valuesPtr = _gch_values.AddrOfPinnedObject();
				this.length = values.Length;
			} else {
				_gch_values = new GCHandle();
				_valuesPtr = IntPtr.Zero;
				this.length = 0;
			}
		}

		public static implicit operator IntPtr(GCHValues<T> v)
		{
			return v._valuesPtr;
		}

		public void Free()
		{
			if( _valuesPtr != IntPtr.Zero ) {
				_valuesPtr = IntPtr.Zero;
				_gch_values.Free();
			}
		}
	}

	//----------------------------------------------------------------------------------------------------------------

	public class PropertyWriter
	{
		private List<int>	_iValues = new List<int>();
		private List<float>	_fValues = new List<float>();

		public void Clear()
		{
			_iValues.Clear();
			_fValues.Clear();
		}

		public void Write( string propertyName, bool value )
		{
			_iValues.Add( MurmurHash32( propertyName ) );
			_iValues.Add( value ? 1 : 0 );
		}

		public void Write( string propertyName, int value )
		{
			_iValues.Add( MurmurHash32( propertyName ) );
			_iValues.Add( value );
		}
		
		public void Write( string propertyName, float value )
		{
			_iValues.Add( MurmurHash32( propertyName ) );
			_fValues.Add( value );
		}

		public void Write( string propertyName, Vector3 value )
		{
			_iValues.Add( MurmurHash32( propertyName ) );
			_fValues.Add( value.x );
			_fValues.Add( value.y );
			_fValues.Add( value.z );
		}

		public void Write( string propertyName, Quaternion value )
		{
			_iValues.Add( MurmurHash32( propertyName ) );
			_fValues.Add( value.x );
			_fValues.Add( value.y );
			_fValues.Add( value.z );
			_fValues.Add( value.w );
		}
		
		private int[] _lock_iValues;
		private float[] _lock_fValues;
		private GCHandle _gch_iValues;
		private GCHandle _gch_fValues;
		public IntPtr iValuesPtr;
		public IntPtr fValuesPtr;
		public int iValueLength { get { return (_lock_iValues != null) ? _lock_iValues.Length : 0; } }
		public int fValueLength { get { return (_lock_fValues != null) ? _lock_fValues.Length : 0; } }
		
		public void Lock()
		{
			_lock_iValues = _iValues.ToArray();
			_lock_fValues = _fValues.ToArray();
			_gch_iValues = GCHandle.Alloc(_lock_iValues, GCHandleType.Pinned);
			_gch_fValues = GCHandle.Alloc(_lock_fValues, GCHandleType.Pinned);
			iValuesPtr = _gch_iValues.AddrOfPinnedObject();
			fValuesPtr = _gch_fValues.AddrOfPinnedObject();
		}
		
		public void Unlock()
		{
			_gch_fValues.Free();
			_gch_iValues.Free();
			fValuesPtr = IntPtr.Zero;
			iValuesPtr = IntPtr.Zero;
		}
	};
	
	public static int ReadInt( byte[] bytes, int index )
	{
		unchecked {
			if( bytes != null && index * 4 + 3 < bytes.Length ) {
				return (int)(bytes[index * 4 + 0])
					| ((int)(bytes[index * 4 + 1]) << 8)
						| ((int)(bytes[index * 4 + 2]) << 16)
						| ((int)(bytes[index * 4 + 3]) << 24);
			}
			
			return 0;
		}
	}

	public class BinaryReader
	{
		private enum Header {
			HeaderIntValueListLength,
			HeaderFloatValueListLength,
			HeaderByteValueListLength,
			IntValueLengthInHeader,
			FloatValueLengthInHeader,
			ByteValueLengthInHeader,
			StructListLength,
			StructIntValueListLength,
			StructFloatValueListLength,
			StructByteValueListLength,
			IntValueListLength,
			FloatValueListLength,
			ByteValueListLength,
			NameLengthListLength,
			NameLength,
			Max,
		}
		
		private enum ReadMode {
			None,
			Header,
			StructList,
			Struct,
		}
		
		private byte[]		_fileBytes;
		
		private int 		_fourCC;

		private int			_structIntValueListPosition;	// for intPool
		private int			_structFloatValueListPosition;	// for floatPool
		private int			_structByteValueListPosition;	// for fileBytes
		
		private int			_intValueListPosition;			// for intPool
		private int			_floatValueListPosition;		// for floatPool
		private int			_byteValueListPosition;			// for bytePool
	
		private int			_nameLengthPosition;			// for intPool
		private int			_namePosition;					// for bytePool
		
		private int[]		_header;
		private int[]		_structList; // [FourCC + Size + Count] x StructListLength
		private int[]		_intPool;
		private float[]		_floatPool;
		private byte[]		_bytePool;
		private string[]	_nameList;
		private int			_bytePoolPosition;

		private ReadMode	_readMode;
		private bool		_isError;

		private int			_currentHeaderIntValueIndex;
		private int			_currentHeaderFloatValueIndex;
		private int			_currentHeaderByteValueIndex;

		private int			_currentStructListIndex;
		private int			_currentStructIndex;
		
		private int			_currentStructFourCC;
		private int			_currentStructFlags;
		private int			_currentStructLength;
		private int			_currentStructIntValueLength;
		private int			_currentStructFloatValueLength;
		private int			_currentStructByteValueLength;
		private int			_currentStructIntValueIndex;
		private int			_currentStructFloatValueIndex;
		private int			_currentStructByteValueIndex;

		private int			_currentIntPoolPosition;
		private int			_currentIntPoolRemain;
		private int			_currentFloatPoolPosition;
		private int			_currentFloatPoolRemain;
		private int			_currentBytePoolPosition;
		private int			_currentBytePoolRemain;
		
		public int structListLength		{ get { return (_header != null) ? _header[(int)Header.StructListLength] : 0; } }
		public int currentStructFourCC	{ get { return _currentStructFourCC; } }
		public int currentStructFlags	{ get { return _currentStructFlags; } }
		public int currentStructLength	{ get { return _currentStructLength; } }
		public int currentStructIndex	{ get { return _currentStructIndex; } }
		
		public static int MakeFourCC( string str )
		{
			return (int)str[0] | ((int)str[1] << 8) | ((int)str[2] << 16) | ((int)str[3] << 24);
		}
		
		public int GetFourCC()
		{
			return _fourCC;
		}
		
		//------------------------------------------------------------------------------------------------------------

		public BinaryReader( byte[] fileBytes )
		{
			_fileBytes = fileBytes;
		}
		
		public bool Preparse()
		{
			if( _fileBytes == null || _fileBytes.Length == 0 ) {
				Debug.LogError( "(BinaryReader) fileBytes is Nothing." );
				_isError = true;
				return false;
			}
//#if UNITY_WEBPLAYER
#if true
			int filePos = 0;
			_fourCC = MMD4MecanimCommon.ReadInt( _fileBytes, 0 );
			filePos += 4;

			_header = new int[(int)Header.Max];
			System.Buffer.BlockCopy( _fileBytes, filePos, _header, 0, _header.Length * 4 );
			filePos += _header.Length * 4;

			_structList = new int[_header[(int)Header.StructListLength] * 9];
			System.Buffer.BlockCopy( _fileBytes, filePos, _structList, 0, _structList.Length * 4 );
			filePos += _structList.Length * 4;

			int intPoolLength	= _header[(int)Header.HeaderIntValueListLength]
								+ _header[(int)Header.StructIntValueListLength]
								+ _header[(int)Header.IntValueListLength]
								+ _header[(int)Header.NameLengthListLength];
			
			_structIntValueListPosition	= _header[(int)Header.HeaderIntValueListLength];
			_intValueListPosition		= _structIntValueListPosition	+ _header[(int)Header.StructIntValueListLength];
			_nameLengthPosition			= _intValueListPosition			+ _header[(int)Header.IntValueListLength];
			
			_intPool = new int[intPoolLength];
			System.Buffer.BlockCopy( _fileBytes, filePos, _intPool, 0, intPoolLength * 4 );
			filePos += intPoolLength * 4;

			int floatPoolLength	= _header[(int)Header.HeaderFloatValueListLength]
								+ _header[(int)Header.StructFloatValueListLength]
								+ _header[(int)Header.FloatValueListLength];

			_structFloatValueListPosition	= _header[(int)Header.HeaderFloatValueListLength];
			_floatValueListPosition			= _structFloatValueListPosition + _header[(int)Header.StructFloatValueListLength];

			_floatPool = new float[floatPoolLength];
			System.Buffer.BlockCopy( _fileBytes, filePos, _floatPool, 0, floatPoolLength * 4 );
			filePos += floatPoolLength * 4;

			int bytePoolLength	= _header[(int)Header.HeaderByteValueListLength]
								+ _header[(int)Header.StructByteValueListLength]
								+ _header[(int)Header.ByteValueListLength]
								+ _header[(int)Header.NameLength];
			
			if( filePos + bytePoolLength > _fileBytes.Length ) {
				Debug.LogError( "(BinaryReader) Overflow." );
				_isError = true;
				return false;
			}

			_bytePool						= _fileBytes;
			_bytePoolPosition				= filePos;
			_structByteValueListPosition	= _bytePoolPosition				+ _header[(int)Header.HeaderByteValueListLength];
			_byteValueListPosition			= _structByteValueListPosition	+ _header[(int)Header.StructByteValueListLength];
			_namePosition					= _byteValueListPosition		+ _header[(int)Header.ByteValueListLength];

			return _PostfixPreparse();
#else
			int filePos = 0;
			int fileReadBytes = 0;
			
			fileReadBytes = 4;
			if( (filePos + fileReadBytes) > _fileBytes.Length ) {
				Debug.LogError( "(BinaryReader) Overflow." );
				_isError = true;
				return false;
			}
			
			_fourCC = (int)_fileBytes[0] | ((int)_fileBytes[1] << 8) | ((int)_fileBytes[2] << 16) | ((int)_fileBytes[3] << 24);
			filePos += fileReadBytes;
			
			fileReadBytes = (int)Header.Max * 4;
			if( filePos + fileReadBytes > _fileBytes.Length ) {
				Debug.LogError( "(BinaryReader) Overflow." );
				_isError = true;
				return false;
			}

			GCHandle gch = GCHandle.Alloc( _fileBytes, GCHandleType.Pinned );
			IntPtr addr = gch.AddrOfPinnedObject();
			_header = new int[(int)Header.Max];
			Marshal.Copy( new IntPtr( addr.ToInt64() + (long)filePos ), _header, 0, (int)Header.Max );
			filePos += fileReadBytes;
			
			fileReadBytes = _header[(int)Header.StructListLength] * 9 * 4;
			if( filePos + fileReadBytes > _fileBytes.Length ) {
				Debug.LogError( "(BinaryReader) Overflow." );
				gch.Free();
				_isError = true;
				return false;
			}
			
			_structList = new int[_header[(int)Header.StructListLength] * 9];
			Marshal.Copy( new IntPtr( addr.ToInt64() + (long)filePos ), _structList, 0, _header[(int)Header.StructListLength] * 9 );
			filePos += fileReadBytes;
			
			int intPoolLength	= _header[(int)Header.HeaderIntValueListLength]
								+ _header[(int)Header.StructIntValueListLength]
								+ _header[(int)Header.IntValueListLength]
								+ _header[(int)Header.NameLengthListLength];
			fileReadBytes = intPoolLength * 4;
			if( filePos + fileReadBytes > _fileBytes.Length ) {
				Debug.LogError( "(BinaryReader) Overflow." );
				gch.Free();
				_isError = true;
				return false;
			}
			
			_structIntValueListPosition	= _header[(int)Header.HeaderIntValueListLength];
			_intValueListPosition		= _structIntValueListPosition	+ _header[(int)Header.StructIntValueListLength];
			_nameLengthPosition			= _intValueListPosition			+ _header[(int)Header.IntValueListLength];
			
			_intPool = new int[intPoolLength];
			Marshal.Copy( new IntPtr( addr.ToInt64() + (long)filePos ), _intPool, 0, intPoolLength );
			filePos += fileReadBytes;

			int floatPoolLength	= _header[(int)Header.HeaderFloatValueListLength]
								+ _header[(int)Header.StructFloatValueListLength]
								+ _header[(int)Header.FloatValueListLength];
			fileReadBytes = floatPoolLength * 4;
			if( filePos + fileReadBytes > _fileBytes.Length ) {
				Debug.LogError( "(BinaryReader) Overflow." );
				gch.Free();
				_isError = true;
				return false;
			}

			_structFloatValueListPosition	= _header[(int)Header.HeaderFloatValueListLength];
			_floatValueListPosition			= _structFloatValueListPosition + _header[(int)Header.StructFloatValueListLength];
			
			_floatPool = new float[floatPoolLength];
			Marshal.Copy( new IntPtr( addr.ToInt64() + (long)filePos ), _floatPool, 0, floatPoolLength );
			filePos += fileReadBytes;
			
			int bytePoolLength	= _header[(int)Header.HeaderByteValueListLength]
								+ _header[(int)Header.StructByteValueListLength]
								+ _header[(int)Header.ByteValueListLength]
								+ _header[(int)Header.NameLength];
			fileReadBytes = bytePoolLength;
			if( filePos + fileReadBytes > _fileBytes.Length ) {
				Debug.LogError( "(BinaryReader) Overflow." );
				gch.Free();
				_isError = true;
				return false;
			}

			_bytePool						= _fileBytes;
			_bytePoolPosition				= filePos;
			_structByteValueListPosition	= _bytePoolPosition				+ _header[(int)Header.HeaderByteValueListLength];
			_byteValueListPosition			= _structByteValueListPosition	+ _header[(int)Header.StructByteValueListLength];
			_namePosition					= _byteValueListPosition		+ _header[(int)Header.ByteValueListLength];
			
			gch.Free();
			return _PostfixPreparse();
#endif
		}

		bool _PostfixPreparse()
		{
			if( _fileBytes == null || _intPool == null || _header == null ) {
				Debug.LogError( "(BinaryReader) null." );
				_isError = true;
				return false;
			}
			if( _nameLengthPosition + _header[(int)Header.NameLengthListLength] > _intPool.Length ) {
				Debug.LogError( "(BinaryReader) Overflow." );
				_isError = true;
				return false;
			}
			
			int nameLengthListLength = _header[(int)Header.NameLengthListLength];
			_nameList = new string[nameLengthListLength];
			int nameLengthPosition = _nameLengthPosition;
			int namePosition = _namePosition;
			for( int i = 0; i < nameLengthListLength; ++i ) {
				int nameLength = _intPool[nameLengthPosition];
				if( namePosition + nameLength > _fileBytes.Length ) {
					Debug.LogError( "(BinaryReader) Overflow." );
					_isError = true;
					return false;
				}
				_nameList[i] = System.Text.Encoding.UTF8.GetString( _fileBytes, namePosition, nameLength );
				++nameLengthPosition;
				namePosition += nameLength + 1;
			}
			
			return true;
		}
		
		//------------------------------------------------------------------------------------------------------------

		public void Rewind()
		{
			if( _isError ) {
				return;
			}

			_readMode						= ReadMode.None;
			_currentHeaderIntValueIndex		= 0;
			_currentHeaderFloatValueIndex	= 0;
			_currentHeaderByteValueIndex	= 0;
			_currentStructListIndex			= 0;
			_currentStructIndex				= 0;
			
			_currentStructFourCC			= 0;
			_currentStructFlags				= 0;
			_currentStructLength			= 0;
			_currentStructIntValueLength	= 0;
			_currentStructFloatValueLength	= 0;
			_currentStructByteValueLength	= 0;
			_currentStructIntValueIndex		= 0;
			_currentStructFloatValueIndex	= 0;
			_currentStructByteValueIndex	= 0;
			
			_currentIntPoolPosition			= 0;
			_currentIntPoolRemain			= 0;
			_currentFloatPoolPosition		= 0;
			_currentFloatPoolRemain			= 0;
			_currentBytePoolPosition		= 0;
			_currentBytePoolRemain			= 0;
		}

		public bool BeginHeader()
		{
			if( _isError ) {
				return false;
			}
			if( _readMode != ReadMode.None ) {
				Debug.LogError( "(BinaryReader) invalid flow." );
				return false;
			}
		
			_currentHeaderIntValueIndex		= 0;
			_currentHeaderFloatValueIndex	= 0;
			_currentHeaderByteValueIndex	= 0;
			_currentIntPoolPosition			= 0;
			_currentFloatPoolPosition		= 0;
			_currentBytePoolPosition		= 0;
			_currentIntPoolRemain			= _header[(int)Header.IntValueLengthInHeader];
			_currentFloatPoolRemain			= _header[(int)Header.FloatValueLengthInHeader];
			_currentBytePoolRemain			= _header[(int)Header.ByteValueLengthInHeader];
			_readMode						= ReadMode.Header;
			return true;
		}

		public int ReadHeaderInt()
		{
			if( _isError ) {
				return 0;
			}
			if( _readMode != ReadMode.Header ) {
				Debug.LogError( "(BinaryReader) invalid flow." );
				_isError = true;
				return 0;
			}
			if( _currentHeaderIntValueIndex >= _header[(int)Header.HeaderIntValueListLength] ) {
				return 0; // Not error.
			}
			
			int headerIntValuePosition = _currentHeaderIntValueIndex;
			if( _intPool == null || headerIntValuePosition >= _intPool.Length ) {
				Debug.LogError( "(BinaryReader) Overflow." );
				_isError = true;
				return 0;
			}
			
			int r = _intPool[headerIntValuePosition];
			++_currentHeaderIntValueIndex;
			return r;
		}

		public float ReadHeaderFloat()
		{
			if( _isError ) {
				return 0;
			}
			if( _readMode != ReadMode.Header ) {
				Debug.LogError( "(BinaryReader) invalid flow." );
				_isError = true;
				return 0;
			}
			if( _currentHeaderFloatValueIndex >= _header[(int)Header.HeaderFloatValueListLength] ) {
				return 0; // Not error.
			}
			
			int headerFloatValuePosition = _currentHeaderFloatValueIndex;
			if( _floatPool == null || headerFloatValuePosition >= _floatPool.Length ) {
				Debug.LogError( "(BinaryReader) Overflow." );
				_isError = true;
				return 0;
			}
			
			float r = _floatPool[headerFloatValuePosition];
			++_currentHeaderFloatValueIndex;
			return r;
		}
		
		public byte ReadHeaderByte()
		{
			if( _isError ) {
				return 0;
			}
			if( _readMode != ReadMode.Header ) {
				Debug.LogError( "(BinaryReader) invalid flow." );
				_isError = true;
				return 0;
			}
			if( _currentHeaderByteValueIndex >= _header[(int)Header.HeaderByteValueListLength] ) {
				return 0; // Not error.
			}
			
			int headerByteValuePosition = _bytePoolPosition + _currentHeaderByteValueIndex;
			if( _bytePool == null || headerByteValuePosition >= _bytePool.Length ) {
				Debug.LogError( "(BinaryReader) Overflow." );
				_isError = true;
				return 0;
			}
			
			byte r = _bytePool[headerByteValuePosition];
			++_currentHeaderByteValueIndex;
			return r;
		}
		
		public bool EndHeader()
		{
			if( _isError ) {
				return false;
			}
			if( _readMode != ReadMode.Header ) {
				Debug.LogError( "(BinaryReader) invalid flow." );
				return false;
			}

			_currentHeaderIntValueIndex		= 0;
			_currentHeaderFloatValueIndex	= 0;
			_currentHeaderByteValueIndex	= 0;
			_currentIntPoolPosition			= 0;
			_currentFloatPoolPosition		= 0;
			_currentBytePoolPosition		= 0;
			_currentIntPoolRemain			= 0;
			_currentFloatPoolRemain			= 0;
			_currentBytePoolRemain			= 0;
			_readMode						= ReadMode.None;
			return true;
		}
		
		public bool BeginStructList()
		{
			if( _isError ) {
				return false;
			}
			if( _readMode != ReadMode.None ) {
				Debug.LogError( "(BinaryReader) invalid flow." );
				return false;
			}
			
			if( _structList == null || _currentStructListIndex + 1 > _structList.Length ) {
				Debug.LogError( "(BinaryReader) Overflow." );
				return false;
			}
			
			_currentStructIndex				= 0;

			_currentStructFourCC			= _structList[_currentStructListIndex * 9 + 0];
			_currentStructFlags				= _structList[_currentStructListIndex * 9 + 1];
			_currentStructLength			= _structList[_currentStructListIndex * 9 + 2];
			_currentStructIntValueLength	= _structList[_currentStructListIndex * 9 + 3];
			_currentStructFloatValueLength	= _structList[_currentStructListIndex * 9 + 4];
			_currentStructByteValueLength	= _structList[_currentStructListIndex * 9 + 5];
			_currentIntPoolPosition			= _structList[_currentStructListIndex * 9 + 6];
			_currentFloatPoolPosition		= _structList[_currentStructListIndex * 9 + 7];
			_currentBytePoolPosition		= _structList[_currentStructListIndex * 9 + 8];

			_currentStructIntValueIndex		= 0; // Limited _currentStructIntValueLength
			_currentStructFloatValueIndex	= 0; // Limited _currentStructFloatValueLength
			_currentStructByteValueIndex	= 0; // Limited _currentStructByteValueLength
			_currentIntPoolRemain			= 0;
			_currentFloatPoolRemain			= 0;
			_currentBytePoolRemain			= 0;
			
			_readMode						= ReadMode.StructList;
			return true;
		}
		
		public bool BeginStruct()
		{
			if( _isError ) {
				return false;
			}
			if( _readMode != ReadMode.StructList ) {
				Debug.LogError( "(BinaryReader) invalid flow." );
				return false;
			}
			
			if( _currentStructIndex >= _currentStructLength ) {
				Debug.LogError( "(BinaryReader) Overflow." );
				return false;
			}

			if( _currentStructIntValueIndex + 3 > _currentStructIntValueLength ) {
				Debug.LogError( "(BinaryReader) Overflow." );
				_isError = true;
				return false;
			}
			if( _intPool == null || _structIntValueListPosition + 3 > _intPool.Length ) {
				Debug.LogError( "(BinaryReader) Overflow." );
				_isError = true;
				return false;
			}

			_currentIntPoolRemain			= _intPool[_structIntValueListPosition + 0];
			_currentFloatPoolRemain			= _intPool[_structIntValueListPosition + 1];
			_currentBytePoolRemain			= _intPool[_structIntValueListPosition + 2];
			_currentStructIntValueIndex		= 3;
			_structIntValueListPosition		+= 3;
			_readMode						= ReadMode.Struct;
			return true;
		}
		
		public int ReadStructInt()
		{
			if( _isError ) {
				return 0;
			}
			if( _readMode != ReadMode.Struct ) {
				Debug.LogError( "(BinaryReader) invalid flow." );
				return 0;
			}
			if( _currentStructIntValueIndex >= _currentStructIntValueLength ) {
				return 0; // Not error.
			}
			if( _intPool == null || _structIntValueListPosition >= _intPool.Length ) {
				Debug.LogError( "(BinaryReader) Overflow." );
				_isError = true;
				return 0;
			}
				
			int r = _intPool[_structIntValueListPosition];
			++_currentStructIntValueIndex;
			++_structIntValueListPosition;
			return r;
		}

		public float ReadStructFloat()
		{
			if( _isError ) {
				return 0;
			}
			if( _readMode != ReadMode.Struct ) {
				Debug.LogError( "(BinaryReader) invalid flow." );
				return 0;
			}
			if( _currentStructFloatValueIndex >= _currentStructFloatValueLength ) {
				return 0; // Not error.
			}
			if( _floatPool == null || _structFloatValueListPosition >= _floatPool.Length ) {
				Debug.LogError( "(BinaryReader) Overflow." );
				_isError = true;
				return 0;
			}
				
			float r = _floatPool[_structFloatValueListPosition];
			++_currentStructFloatValueIndex;
			++_structFloatValueListPosition;
			return r;
		}
		
		public Vector2 ReadStructVector2()
		{
			Vector2 r = Vector2.zero;
			r.x = ReadStructFloat();
			r.y = ReadStructFloat();
			return r;
		}

		public Vector3 ReadStructVector3()
		{
			Vector3 r = Vector3.zero;
			r.x = ReadStructFloat();
			r.y = ReadStructFloat();
			r.z = ReadStructFloat();
			return r;
		}

		public byte ReadStructByte()
		{
			if( _isError ) {
				return 0;
			}
			if( _readMode != ReadMode.Struct ) {
				Debug.LogError( "(BinaryReader) invalid flow." );
				return 0;
			}
			if( _currentStructByteValueIndex >= _currentStructByteValueLength ) {
				return 0; // Not error.
			}
			if( _bytePool == null || _structByteValueListPosition >= _bytePool.Length ) {
				Debug.LogError( "(BinaryReader) Overflow." );
				_isError = true;
				return 0;
			}
				
			byte r = _bytePool[_structByteValueListPosition];
			++_currentStructByteValueIndex;
			++_structByteValueListPosition;
			return r;
		}
		
		public bool EndStruct()
		{
			if( _readMode != ReadMode.Struct ) {
				Debug.LogError( "(BinaryReader) invalid flow." );
				return false;
			}
			
			if( _currentStructIntValueIndex < _currentStructIntValueLength ) {
				_structIntValueListPosition += (_currentStructIntValueLength - _currentStructIntValueIndex);
			}
			if( _currentStructFloatValueIndex < _currentStructFloatValueLength ) {
				_structFloatValueListPosition += (_currentStructFloatValueLength - _currentStructFloatValueIndex);
			}
			if( _currentStructByteValueIndex < _currentStructByteValueLength ) {
				_structByteValueListPosition += (_currentStructByteValueLength - _currentStructByteValueIndex);
			}

			_currentIntPoolPosition		+= _currentIntPoolRemain;
			_currentFloatPoolPosition	+= _currentFloatPoolRemain;
			_currentBytePoolPosition	+= _currentBytePoolRemain;
			
			++_currentStructIndex;
			_currentStructIntValueIndex		= 0;
			_currentStructFloatValueIndex	= 0;
			_currentStructByteValueIndex	= 0;
			_currentIntPoolRemain			= 0;
			_currentFloatPoolRemain			= 0;
			_currentBytePoolRemain			= 0;
			_readMode						= ReadMode.StructList;
			return true;
		}

		public bool EndStructList()
		{
			if( _readMode != ReadMode.StructList ) {
				Debug.LogError( "(BinaryReader) invalid flow." );
				return false;
			}

			if( _currentStructIndex < _currentStructLength ) {
				_structIntValueListPosition		+= _currentStructIntValueLength		* (_currentStructLength - _currentStructIndex);
				_structFloatValueListPosition	+= _currentStructFloatValueLength	* (_currentStructLength - _currentStructIndex);
				_structByteValueListPosition	+= _currentStructByteValueLength	* (_currentStructLength - _currentStructIndex);
			}
				
			++_currentStructListIndex;

			_currentStructIndex				= 0;

			_currentStructFourCC			= 0;
			_currentStructFlags				= 0;
			_currentStructLength			= 0;

			_currentStructIntValueLength	= 0;
			_currentStructFloatValueLength	= 0;
			_currentStructByteValueLength	= 0;
			_currentIntPoolPosition			= 0;
			_currentFloatPoolPosition		= 0;
			_currentBytePoolPosition		= 0;
	
			_currentStructIntValueIndex		= 0; // Limited _currentStructIntValueLength
			_currentStructFloatValueIndex	= 0; // Limited _currentStructFloatValueLength
			_currentStructByteValueIndex	= 0; // Limited _currentStructByteValueLength
			_currentIntPoolRemain			= 0;
			_currentFloatPoolRemain			= 0;
			_currentBytePoolRemain			= 0;
			
			_readMode = ReadMode.None;
			return true;
		}
		
		public int ReadInt()
		{
			if( _intPool == null || _currentIntPoolRemain == 0 ) {
				return 0;
			}
			int r = _intPool[_intValueListPosition + _currentIntPoolPosition];
			++_currentIntPoolPosition;
			--_currentIntPoolRemain;
			return r;
		}

		public float ReadFloat()
		{
			if( _floatPool == null || _currentFloatPoolRemain == 0 ) {
				return 0;
			}
			float r = _floatPool[_floatValueListPosition + _currentFloatPoolPosition];
			++_currentFloatPoolPosition;
			--_currentFloatPoolRemain;
			return r;
		}

		public Color ReadColor()
		{
			float r = ReadFloat();
			float g = ReadFloat();
			float b = ReadFloat();
			float a = ReadFloat();
			return new Color( r, g, b, a );
		}

		public Color ReadColorRGB()
		{
			float r = ReadFloat();
			float g = ReadFloat();
			float b = ReadFloat();
			return new Color( r, g, b, 1.0f );
		}

		public Vector3 ReadVector3()
		{
			float x = ReadFloat();
			float y = ReadFloat();
			float z = ReadFloat();
			return new Vector3( x, y, z );
		}

		public Quaternion ReadQuaternion()
		{
			float x = ReadFloat();
			float y = ReadFloat();
			float z = ReadFloat();
			float w = ReadFloat();
			return new Quaternion( x, y, z, w );
		}

		public byte ReadByte()
		{
			if( _fileBytes == null || _currentBytePoolRemain == 0 ) {
				return 0;
			}
			byte r = _fileBytes[_byteValueListPosition + _currentBytePoolPosition];
			++_currentBytePoolPosition;
			--_currentBytePoolRemain;
			return r;
		}
		
		public string GetName( int index )
		{
			if( _nameList != null && (uint)index < (uint)_nameList.Length ) {
				return _nameList[index];
			}

			return "";
		}
	}
	
	//----------------------------------------------------------------------------------------------------------------

	public enum TextureFileSign
	{
		None,
		Bmp,
		BmpWithAlpha,
		Png,
		PngWithAlpha,
		Jpeg,
		Targa,
		TargaWithAlpha,
	}
	
	private static UInt32 _Swap( UInt32 v )
	{
		return (v >> 24) | ((v >> 8) & 0xff00) | ((v << 8) & 0xff0000) | (v << 24);
	}
	
	private static UInt32 _MakeFourCC( char a, char b, char c, char d )
	{
		return (UInt32)(byte)a | ((UInt32)(byte)b << 8) | ((UInt32)(byte)c << 16) | ((UInt32)(byte)d << 24);
	}
	
	public static TextureFileSign GetTextureFileSign( string path )
	{
		try {
			using( System.IO.FileStream fileStream = new System.IO.FileStream( path, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read ) )
			{
				// BMP		... 14 + 56(V4/V5)
				// PNG		... 8
				// Targa	... 18
				// Jpeg		... 2
				
				byte[] bytes = new byte[(int)fileStream.Length];
				fileStream.Read( bytes, 0, (int)fileStream.Length );

				if( (int)fileStream.Length >= 8 ) {
					bool isPng = true;
					byte[] pngSign = new byte[8] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a };
					for( int i = 0; i < 8; ++i ) {
						if( bytes[i] != pngSign[i] ) {
							isPng = false;
							break;
						}
					}
					if( isPng ) {
						fileStream.Seek( 0, SeekOrigin.Begin );
						using( System.IO.BinaryReader binaryReader = new System.IO.BinaryReader( fileStream ) ) {
							binaryReader.ReadInt32();
							binaryReader.ReadInt32();
							for(;;) {
								try {
									UInt32 chunkLength = _Swap(binaryReader.ReadUInt32());
									UInt32 chunkType = binaryReader.ReadUInt32();
									if( chunkType == _MakeFourCC( 'I', 'H', 'D', 'R' ) ) {
										if( chunkLength < 13 ) {
											return TextureFileSign.Png; // Unknown.
										}
										binaryReader.ReadUInt32();
										binaryReader.ReadUInt32();
										binaryReader.ReadByte();
										byte colorType = binaryReader.ReadByte();
										if( colorType == 4 ||		// Gray Scale With Alpha
											colorType == 6 ) {		// True Color With Alpha
											return TextureFileSign.PngWithAlpha;
										}
										chunkLength -= 10;
									} else if( chunkType == _MakeFourCC( 't', 'R', 'N', 'S' ) ) {
										return TextureFileSign.PngWithAlpha;
									} else if( chunkType == _MakeFourCC( 'I', 'E', 'N', 'D' ) ) {
										return TextureFileSign.Png; // End of image.
									} else if( chunkType == _MakeFourCC( 'I', 'D', 'A', 'T' ) ) {
										return TextureFileSign.Png; // Begin of data.
									}
									// Seek(x4)
									for( UInt32 n = 0; n < chunkLength / 4; ++n ) {
										binaryReader.ReadUInt32();
									}
									// Seek
									for( UInt32 n = 0; n < chunkLength % 4; ++n ) {
										binaryReader.ReadByte();
									}
									binaryReader.ReadUInt32(); // CRC
								} catch( Exception ) {
									return TextureFileSign.PngWithAlpha; // Unknown.
								}
							}
						}
					}
				}

				if( (int)fileStream.Length >= 18 && bytes[0] == 'B' && bytes[1] == 'M' ) {
					uint infoSize = (uint)bytes[14] | ((uint)bytes[15] << 8) | ((uint)bytes[16] << 16) | ((uint)bytes[17] << 24);
					if( infoSize == 12 ) { // Core
						return TextureFileSign.Bmp;
					} else if( infoSize == 40 || infoSize == 52 || infoSize == 56 || infoSize == 60 || infoSize == 96 || infoSize == 108 || infoSize == 112 || infoSize == 120 || infoSize == 124 )	{
						if( infoSize >= 56 ) {
							uint alphaMask = (uint)bytes[66] | ((uint)bytes[67] << 8) | ((uint)bytes[68] << 16) | ((uint)bytes[69] << 73);
							if( alphaMask != 0 ) {
								return TextureFileSign.BmpWithAlpha;
							}
						}
						uint compressType = (uint)bytes[30] | ((uint)bytes[31] << 8) | ((uint)bytes[32] << 16) | ((uint)bytes[33] << 73);
						switch( compressType ) {
						case 0: case 1: case 2: case 3: case 4:
							return TextureFileSign.Bmp;
						case 5:
							return TextureFileSign.BmpWithAlpha;
						}
					}
				}
				
				if( (int)fileStream.Length >= 2 && bytes[0] == 0xff && bytes[1] == 0xd8 ) { // SOI
					return TextureFileSign.Jpeg;
				}
				
				if( (int)fileStream.Length >= 18 ) {
					byte cMapType = bytes[1];
					byte imageDesc = bytes[17];
					byte origin = (byte)((imageDesc & 0x30) >> 4);
					if( ( cMapType == 0 || cMapType == 1 ) && ( origin == 0 || origin == 2 ) ) {
						byte imageType = bytes[2];
						byte pixel = bytes[16];
						byte alphaBits = (byte)(imageDesc & 0x0f);
						if( ( imageType == 1 || imageType ==  9 ) && ( pixel == 1 || pixel == 2 || pixel == 4 || pixel == 8 ) ) {
							return (alphaBits > 0) ? TextureFileSign.TargaWithAlpha : TextureFileSign.Targa; // CMap or CMap(RLE)
						}
						if( ( imageType == 3 || imageType == 11 ) && ( pixel == 1 || pixel == 2 || pixel == 4 || pixel == 8 ) ) {
							return (alphaBits > 0) ? TextureFileSign.TargaWithAlpha : TextureFileSign.Targa; // Gray or Gray(RLE)
						}
						if( ( imageType == 2 || imageType == 10 ) && ( pixel == 16 || pixel == 24 || pixel == 32 ) ) {
							return (alphaBits > 0) ? TextureFileSign.TargaWithAlpha : TextureFileSign.Targa; // TC or TC(RLE)
						}
					}
				}
			}
			return TextureFileSign.None;
		} catch( Exception ) {
			return TextureFileSign.None;
		}
	}
	
	//----------------------------------------------------------------------------------------------------------------

	public static bool IsID( string str )
	{
		// Check format as Decimal + period.
		if( string.IsNullOrEmpty( str ) ) {
			return false;
		}

		unchecked {
			if( (uint)(str[0] - '0') > 9 ) {
				return false;
			}

			for( int i = 1; i != str.Length; ++i ) {
				if( str[i] == '.' ) {
					return true;
				} else if( (uint)(str[0] - '0') > 9 ) {
					return false;
				}
			}
		}

		return false; // Not terminated period.
	}

	public static int ToInt( string str )
	{
		unchecked {
			if( str == null ) {
				return 0;
			}
			
			return ToInt( str, 0, str.Length );
		}
	}

	public static int ToInt( string str, int pos, int len )
	{
		unchecked {
			if( str == null ) {
				return 0;
			}
			
			len += pos;
			if( len < str.Length ) {
				len = str.Length;
			}
			
			bool isMinus = false;
			if( pos < len && str[pos] == '-' ) {
				isMinus = true;
				++pos;
			}
			
			int value = 0;
			if( pos < len ) {
				uint v = (uint)(str[pos] - '0');
				if( v <= 9 ) {
					value = (int)v;
					++pos;
				} else {
					return 0;
				}
			}
			
			for( ; pos < len; ++pos ) {
				uint v = (uint)(str[pos] - '0');
				if( v <= 9 ) {
					value = value * 10 + (int)v;
				} else {
					break;
				}
			}
			
			return isMinus ? -value : value;
		}
	}

	public class CloneMeshWork
	{
		public Mesh			mesh;
		public string		name;
		public Vector3[]	vertices;
		public Vector3[]	normals;
		public Vector4[]	tangents;
		public Color32[]	colors32;
		public BoneWeight[]	boneWeights;
		public Bounds		bounds;
		public HideFlags	hideFlags;
		public Vector2[]	uv1;
		public Vector2[]	uv2;
		public Vector2[]	uv;
		public Matrix4x4[]	bindposes;
		public int[]		triangles;
	}

	public static CloneMeshWork CloneMesh( Mesh sharedMesh )
	{
		if( sharedMesh == null ) {
			return null;
		}

		CloneMeshWork cloneMeshWork = new CloneMeshWork();

		Mesh mesh = new Mesh();
		cloneMeshWork.mesh = mesh;

		cloneMeshWork.name = sharedMesh.name;
		cloneMeshWork.vertices = sharedMesh.vertices;
		cloneMeshWork.normals = sharedMesh.normals;
		cloneMeshWork.tangents = sharedMesh.tangents;
		cloneMeshWork.colors32 = sharedMesh.colors32;
		cloneMeshWork.boneWeights = sharedMesh.boneWeights;
		cloneMeshWork.bounds = sharedMesh.bounds;
		cloneMeshWork.hideFlags = sharedMesh.hideFlags;
		cloneMeshWork.uv1 = sharedMesh.uv2;
		cloneMeshWork.uv2 = sharedMesh.uv2;
		cloneMeshWork.uv = sharedMesh.uv;
		cloneMeshWork.bindposes = sharedMesh.bindposes;
		cloneMeshWork.triangles = sharedMesh.triangles;

		mesh.name = cloneMeshWork.name;
		mesh.vertices = cloneMeshWork.vertices;
		mesh.normals = cloneMeshWork.normals;
		mesh.tangents = cloneMeshWork.tangents;
		mesh.colors32 = cloneMeshWork.colors32;
		mesh.boneWeights = cloneMeshWork.boneWeights;
		mesh.bounds = cloneMeshWork.bounds;
		mesh.hideFlags = cloneMeshWork.hideFlags;
		mesh.uv2 = cloneMeshWork.uv1;
		mesh.uv2 = cloneMeshWork.uv2;
		mesh.uv = cloneMeshWork.uv;
		mesh.bindposes = cloneMeshWork.bindposes;
		mesh.triangles = cloneMeshWork.triangles;
		
		if( sharedMesh.subMeshCount > 0 ) {
			mesh.subMeshCount = sharedMesh.subMeshCount;
	        for( int sub = 0; sub < sharedMesh.subMeshCount; ++sub ) {
				mesh.SetTriangles( sharedMesh.GetIndices( sub ), sub );
			}
		}

		return cloneMeshWork;
	}
	
	public static Material CloneMaterial( Material sharedMaterial )
	{
		if( sharedMaterial == null ) {
			return sharedMaterial;
		}
	
		return new Material( sharedMaterial );
	}
	
	public static bool Approx( ref float src, float dest, float step )
	{
		if( src > dest ) {
			if( ( src -= step ) <= dest ) {
				src = dest;
				return true;
			}
			return false;
		} else if( src < dest ) {
			if( ( src += step ) >= dest ) {
				src = dest;
				return true;
			}
			return false;
		} else {
			return true;
		}
	}
	
	public static bool FindAnything<Type>( List<Type> elements, Type element )
		where Type : class
	{
		if( elements != null ) {
			for( int i = 0; i < elements.Count; ++i ) {
				if( element == elements[i] ) {
					return true;
				}
			}
		}

		return false;
	}
	
	public static bool IsAlphabet( char ch )
	{
		return ( (uint)(ch - 'A') <= 25 || (uint)(ch - 'a') <= 25 ) || (uint)(ch - '\uFF21') <= 25 || (uint)(ch - '\uFF41') <= 25;
	}
	
	public static char ToHalfLower( char ch )
	{
		if( (uint)(ch - 'A') <= 25 ) {
			return unchecked( (char)((uint)(ch - 'A') + (uint)'a') );
		}
		if( (uint)(ch - '\uFF21') <= 25 ) {
			return unchecked( (char)((uint)(ch - '\uFF21') + (uint)'a') );
		}
		if( (uint)(ch - '\uFF41') <= 25 ) {
			return unchecked( (char)((uint)(ch - '\uFF41') + (uint)'a') );
		}
		
		return ch;
	}

	public static float Reciplocal( float x )
	{
		return (x != 0.0f) ? (1.0f / x) : 0.0f;
	}

	public static Vector3 Reciplocal( Vector3 scale )
	{
		return new Vector3( Reciplocal( scale.x ), Reciplocal( scale.y ), Reciplocal( scale.z ) );
	}

	public static Vector3 Reciplocal( ref Vector3 scale )
	{
		return new Vector3( Reciplocal( scale.x ), Reciplocal( scale.y ), Reciplocal( scale.z ) );
	}

	public static void ConvertMatrixBulletPhysics( ref Matrix4x4 matrix )
	{
		matrix.m03 = -matrix.m03; // translate.x = -translate.x
		matrix.m10 = -matrix.m10; // right.y = -right.y
		matrix.m20 = -matrix.m20; // right.z = -right.z
		matrix.m01 = -matrix.m01; // up.x = -up.x
		matrix.m02 = -matrix.m02; // forward.x = -forward.x
	}

	public static Vector3 ComputeMatrixScale( ref Matrix4x4 matrix )
	{
		return new Vector3(
			new Vector3( matrix.m00, matrix.m10, matrix.m20 ).magnitude,
			new Vector3( matrix.m01, matrix.m11, matrix.m21 ).magnitude,
			new Vector3( matrix.m02, matrix.m12, matrix.m22 ).magnitude );
	}

	public static Vector3 ComputeMatrixReciplocalScale( ref Matrix4x4 matrix )
	{
		return new Vector3(
			Reciplocal( new Vector3( matrix.m00, matrix.m10, matrix.m20 ).magnitude ),
			Reciplocal( new Vector3( matrix.m01, matrix.m11, matrix.m21 ).magnitude ),
			Reciplocal( new Vector3( matrix.m02, matrix.m12, matrix.m22 ).magnitude ) );
	}

	public static void SetMatrixBasis( ref Matrix4x4 matrix, ref Vector3 right, ref Vector3 up, ref Vector3 forward )
	{
		matrix.m00 = right.x;
		matrix.m10 = right.y;
		matrix.m20 = right.z;
		matrix.m01 = up.x;
		matrix.m11 = up.y;
		matrix.m21 = up.z;
		matrix.m02 = forward.x;
		matrix.m12 = forward.y;
		matrix.m22 = forward.z;
	}

	public static void NormalizeMatrixBasis( ref Matrix4x4 matrix )
	{
		Vector3 right	= new Vector3( matrix.m00, matrix.m10, matrix.m20 );
		Vector3 up		= new Vector3( matrix.m01, matrix.m11, matrix.m21 );
		Vector3 forward	= new Vector3( matrix.m02, matrix.m12, matrix.m22 );
		right	*= Reciplocal( right.magnitude );
		up		*= Reciplocal( up.magnitude );
		forward	*= Reciplocal( forward.magnitude );
		SetMatrixBasis( ref matrix, ref right, ref up, ref forward );
	}

	public static void NormalizeMatrixBasis( ref Matrix4x4 matrix, ref Vector3 rScale )
	{
		Vector3 right	= new Vector3( matrix.m00, matrix.m10, matrix.m20 ) * rScale.x;
		Vector3 up		= new Vector3( matrix.m01, matrix.m11, matrix.m21 ) * rScale.y;
		Vector3 forward	= new Vector3( matrix.m02, matrix.m12, matrix.m22 ) * rScale.z;
		SetMatrixBasis( ref matrix, ref right, ref up, ref forward );
	}

	//------------------------------------------------------------------------------------------------

	public static bool IsNoEffects( ref float value, MMD4MecanimData.MorphMaterialOperation operation )
	{
		switch( operation ) {
		case MMD4MecanimData.MorphMaterialOperation.Adding:
			return value <= Mathf.Epsilon;
		case MMD4MecanimData.MorphMaterialOperation.Multiply:
			return Mathf.Abs(value - 1.0f) <= Mathf.Epsilon;
		default:
			return true;
		}
	}

	public static bool IsNoEffects( ref Color color, MMD4MecanimData.MorphMaterialOperation operation )
	{
		switch( operation ) {
		case MMD4MecanimData.MorphMaterialOperation.Adding:
			return color.r <= Mathf.Epsilon
				&& color.g <= Mathf.Epsilon
				&& color.b <= Mathf.Epsilon
				&& color.a <= Mathf.Epsilon;
		case MMD4MecanimData.MorphMaterialOperation.Multiply:
			return Mathf.Abs(color.r - 1.0f) <= Mathf.Epsilon
				&& Mathf.Abs(color.g - 1.0f) <= Mathf.Epsilon
	            && Mathf.Abs(color.b - 1.0f) <= Mathf.Epsilon
	            && Mathf.Abs(color.a - 1.0f) <= Mathf.Epsilon;
		default:
			return true;
		}
	}

	public static bool IsNoEffectsRGB( ref Color color, MMD4MecanimData.MorphMaterialOperation operation )
	{
		switch( operation ) {
		case MMD4MecanimData.MorphMaterialOperation.Adding:
			return color.r <= Mathf.Epsilon
				&& color.g <= Mathf.Epsilon
				&& color.b <= Mathf.Epsilon;
		case MMD4MecanimData.MorphMaterialOperation.Multiply:
			return Mathf.Abs(color.r - 1.0f) <= Mathf.Epsilon
				&& Mathf.Abs(color.g - 1.0f) <= Mathf.Epsilon
				&& Mathf.Abs(color.b - 1.0f) <= Mathf.Epsilon;
		default:
			return true;
		}
	}

	public static float NormalizeAsDegree( float v )
	{
		if( v < -180.0f ) {
			do {
				v += 360.0f;
			} while( v < -180.0f );
		} else if( v > 180.0f ) {
			do {
				v -= 360.0f;
			} while( v > 180.0f );
		}
		
		return v;
	}
	
	public static Vector3 NormalizeAsDegree( Vector3 degrees )
	{
		return new Vector3(
			NormalizeAsDegree( degrees.x ),
			NormalizeAsDegree( degrees.y ),
			NormalizeAsDegree( degrees.z ) );
	}
	
	public static bool FuzzyZero( float v )
	{
		return Math.Abs( v ) <= Mathf.Epsilon;
	}

	public static bool FuzzyZero( Vector3 v )
	{
		return Mathf.Abs( v.x ) <= Mathf.Epsilon
			&& Mathf.Abs( v.y ) <= Mathf.Epsilon
			&& Mathf.Abs( v.z ) <= Mathf.Epsilon;
	}

	public static Quaternion Inverse( Quaternion q )
	{
		return new Quaternion( -q.x, -q.y, -q.z, q.w );
	}

	public static bool FuzzyIdentity( Quaternion q )
	{
		return Mathf.Abs(q.x) <= Mathf.Epsilon
			&& Mathf.Abs(q.y) <= Mathf.Epsilon
			&& Mathf.Abs(q.z) <= Mathf.Epsilon
			&& Mathf.Abs(q.w - 1.0f) <= Mathf.Epsilon;
	}

	public static Quaternion FuzzyMul( Quaternion lhs, Quaternion rhs )
	{
		if( FuzzyIdentity( lhs ) ) {
			return rhs;
		} else if( FuzzyIdentity( rhs ) ) {
			return lhs;
		} else {
			return lhs * rhs;
		}
	}
	
	public static Quaternion FastMul( Quaternion lhs, Quaternion rhs )
	{
		if( lhs == Quaternion.identity ) {
			return rhs;
		} else if( rhs == Quaternion.identity ) {
			return lhs;
		} else {
			return lhs * rhs;
		}
	}

	public static void FuzzyAdd( ref float lhs, ref float rhs )
	{
		if( rhs > Mathf.Epsilon ) {
			lhs += rhs;
		}
	}

	public static void FuzzyAdd( ref Color lhs, ref Color rhs )
	{
		if( rhs.r > Mathf.Epsilon ) lhs.r += rhs.r;
		if( rhs.g > Mathf.Epsilon ) lhs.g += rhs.g;
		if( rhs.b > Mathf.Epsilon ) lhs.b += rhs.b;
		if( rhs.a > Mathf.Epsilon ) lhs.a += rhs.a;
	}

	public static void FuzzyMul( ref float lhs, ref float rhs )
	{
		if( Mathf.Abs( rhs - 1.0f ) > Mathf.Epsilon ) {
			lhs *= rhs;
		}
	}

	public static void FuzzyMul( ref Color lhs, ref Color rhs )
	{
		if( Mathf.Abs( rhs.r - 1.0f ) > Mathf.Epsilon ) lhs.r *= rhs.r;
		if( Mathf.Abs( rhs.g - 1.0f ) > Mathf.Epsilon ) lhs.g *= rhs.g;
		if( Mathf.Abs( rhs.b - 1.0f ) > Mathf.Epsilon ) lhs.b *= rhs.b;
		if( Mathf.Abs( rhs.a - 1.0f ) > Mathf.Epsilon ) lhs.a *= rhs.a;
	}

	public static void FuzzyAdd( ref float lhs, ref float rhs, float weight )
	{
		if( rhs > Mathf.Epsilon ) {
			lhs += rhs * weight;
		}
	}
	
	public static void FuzzyAdd( ref Color lhs, ref Color rhs, float weight )
	{
		if( rhs.r > Mathf.Epsilon ) lhs.r += rhs.r * weight;
		if( rhs.g > Mathf.Epsilon ) lhs.g += rhs.g * weight;
		if( rhs.b > Mathf.Epsilon ) lhs.b += rhs.b * weight;
		if( rhs.a > Mathf.Epsilon ) lhs.a += rhs.a * weight;
	}
	
	public static void FuzzyMul( ref float lhs, ref float rhs, float weight )
	{
		if( Mathf.Abs( rhs - 1.0f ) > Mathf.Epsilon ) {
			lhs *= rhs * weight + (1.0f - weight);
		}
	}
	
	public static void FuzzyMul( ref Color lhs, ref Color rhs, float weight )
	{
		if( Mathf.Abs( rhs.r - 1.0f ) > Mathf.Epsilon ) lhs.r *= rhs.r * weight + (1.0f - weight);
		if( Mathf.Abs( rhs.g - 1.0f ) > Mathf.Epsilon ) lhs.g *= rhs.g * weight + (1.0f - weight);
		if( Mathf.Abs( rhs.b - 1.0f ) > Mathf.Epsilon ) lhs.b *= rhs.b * weight + (1.0f - weight);
		if( Mathf.Abs( rhs.a - 1.0f ) > Mathf.Epsilon ) lhs.a *= rhs.a * weight + (1.0f - weight);
	}

	public static void OperationMaterial( ref MMD4MecanimData.MorphMaterialData currentMaterialData, ref MMD4MecanimData.MorphMaterialData operationMaterialData, float weight )
	{
		if( Mathf.Abs(weight - 1.0f) <= Mathf.Epsilon ) {
			switch( operationMaterialData.operation ) {
			case MMD4MecanimData.MorphMaterialOperation.Adding:
				FuzzyAdd( ref currentMaterialData.diffuse,		ref operationMaterialData.diffuse );
				FuzzyAdd( ref currentMaterialData.specular,		ref operationMaterialData.specular );
				FuzzyAdd( ref currentMaterialData.shininess,	ref operationMaterialData.shininess );
				FuzzyAdd( ref currentMaterialData.ambient,		ref operationMaterialData.ambient );
				FuzzyAdd( ref currentMaterialData.edgeColor,	ref operationMaterialData.edgeColor );
				FuzzyAdd( ref currentMaterialData.edgeSize,		ref operationMaterialData.edgeSize );
				break;
			case MMD4MecanimData.MorphMaterialOperation.Multiply:
				FuzzyMul( ref currentMaterialData.diffuse,		ref operationMaterialData.diffuse );
				FuzzyMul( ref currentMaterialData.specular,		ref operationMaterialData.specular );
				FuzzyMul( ref currentMaterialData.shininess,	ref operationMaterialData.shininess );
				FuzzyMul( ref currentMaterialData.ambient,		ref operationMaterialData.ambient );
				FuzzyMul( ref currentMaterialData.edgeColor,	ref operationMaterialData.edgeColor );
				FuzzyMul( ref currentMaterialData.edgeSize,		ref operationMaterialData.edgeSize );
				break;
			}
		} else {
			switch( operationMaterialData.operation ) {
			case MMD4MecanimData.MorphMaterialOperation.Adding:
				FuzzyAdd( ref currentMaterialData.diffuse,		ref operationMaterialData.diffuse,		weight );
				FuzzyAdd( ref currentMaterialData.specular,		ref operationMaterialData.specular,		weight );
				FuzzyAdd( ref currentMaterialData.shininess,	ref operationMaterialData.shininess,	weight );
				FuzzyAdd( ref currentMaterialData.ambient,		ref operationMaterialData.ambient,		weight );
				FuzzyAdd( ref currentMaterialData.edgeColor,	ref operationMaterialData.edgeColor,	weight );
				FuzzyAdd( ref currentMaterialData.edgeSize,		ref operationMaterialData.edgeSize,		weight );
				break;
			case MMD4MecanimData.MorphMaterialOperation.Multiply:
				FuzzyMul( ref currentMaterialData.diffuse,		ref operationMaterialData.diffuse,		weight );
				FuzzyMul( ref currentMaterialData.specular,		ref operationMaterialData.specular,		weight );
				FuzzyMul( ref currentMaterialData.shininess,	ref operationMaterialData.shininess,	weight );
				FuzzyMul( ref currentMaterialData.ambient,		ref operationMaterialData.ambient,		weight );
				FuzzyMul( ref currentMaterialData.edgeColor,	ref operationMaterialData.edgeColor,	weight );
				FuzzyMul( ref currentMaterialData.edgeSize,		ref operationMaterialData.edgeSize,		weight );
				break;
			}
		}
	}

	public static void BackupMaterial( ref MMD4MecanimData.MorphMaterialData materialData, Material material )
	{
		if( material != null && material.shader != null && material.shader.name.StartsWith("MMD4Mecanim") ) {
			materialData.materialID	= ToInt( material.name );
			materialData.diffuse	= material.GetColor("_Color");
			materialData.specular	= material.GetColor("_Specular"); // Memo: This value scaled MMDLit_globalLighting
			materialData.shininess	= material.GetFloat("_Shininess");
			materialData.ambient	= material.GetColor("_Ambient");
			materialData.edgeColor	= material.GetColor("_EdgeColor");
			materialData.edgeScale	= material.GetFloat("_EdgeScale");
			materialData.edgeSize	= material.GetFloat("_EdgeSize"); // Memo: This value scaled edgeScale
			materialData.alPower	= material.GetFloat("_ALPower");
			materialData.specular	*= (1.0f / MMDLit_globalLighting.r);
			if( materialData.edgeScale > 0.0f ) {
				materialData.edgeSize *= (1.0f / materialData.edgeScale);
			}
		}
	}

	public static void FeedbackMaterial(
		ref MMD4MecanimData.MorphMaterialData materialData,
		Material material,
		MMD4MecanimModel.MorphAutoLuminous morphAutoLuminous )
	{
		if( material != null && material.shader != null && material.shader.name.StartsWith("MMD4Mecanim") && morphAutoLuminous != null ) {
			material.SetColor("_Color",		materialData.diffuse);
			material.SetColor("_Specular",	materialData.specular * MMDLit_globalLighting.r);
			material.SetFloat("_Shininess",	materialData.shininess);
			material.SetColor("_Ambient",	materialData.ambient);
			material.SetColor("_EdgeColor",	materialData.edgeColor);
			material.SetFloat("_EdgeSize",	materialData.edgeSize * materialData.edgeScale);

			if( materialData.shininess > 100.0f ) { // AutoLuminous(Emissive)
				Color alColor = ComputeAutoLuminousEmissiveColor(
					materialData.diffuse,
					materialData.ambient,
					materialData.shininess,
					materialData.alPower,
					morphAutoLuminous );
				material.SetColor("_Emissive", alColor );
			} else {
				material.SetColor("_Emissive", new Color(0,0,0,0));
			}

			Color globalAmbient = RenderSettings.ambientLight;
			Color diffuse = materialData.diffuse;
			Color ambient = materialData.ambient;
			
			Color tempAmbient = MMD4MecanimCommon.MMDLit_GetTempAmbient(globalAmbient, ambient);
			Color tempAmbientL = MMD4MecanimCommon.MMDLit_GetTempAmbientL(ambient);
			Color tempDiffuse = MMD4MecanimCommon.MMDLit_GetTempDiffuse(globalAmbient, ambient, diffuse);
			tempDiffuse.a = diffuse.a;
			
			material.SetColor( "_TempAmbient", tempAmbient );
			material.SetColor( "_TempAmbientL", tempAmbientL );
			material.SetColor( "_TempDiffuse", tempDiffuse );
		}
	}

	public static Color ComputeAutoLuminousEmissiveColor(
		Color diffuse,
		Color ambient,
		float shininess,
		float alPower )
	{
		if( alPower > 0.0f ) {
			return (diffuse + ambient * 0.5f) * 0.5f * (shininess - 100.0f) * alPower * (1.0f / 7.0f);
		}
		return new Color(0,0,0,0);
	}

	public static Color ComputeAutoLuminousEmissiveColor(
		Color diffuse,
		Color ambient,
		float shininess,
		float alPower,
		MMD4MecanimModel.MorphAutoLuminous morphAutoLuminous )
	{
		Color c = ComputeAutoLuminousEmissiveColor( diffuse, ambient, shininess, alPower );
		if( morphAutoLuminous != null ) {
			return c * (1.0f + morphAutoLuminous.lightUp * 3.0f) * (1.0f - morphAutoLuminous.lightOff);
		}
		return c;
	}

	//------------------------------------------------------------------------------------------------------------------------------------------------

	static void _GetMeshRenderers( ArrayList meshRenderers, Transform parentTransform )
	{
		if( parentTransform.GetComponent<Animator>() ) {
			return;
		}
		MeshRenderer meshRenderer = parentTransform.GetComponent<MeshRenderer>();
		if( meshRenderer != null ) {
			meshRenderers.Add( meshRenderer );
		}
		foreach( Transform childTransform in parentTransform ) {
			_GetMeshRenderers( meshRenderers, childTransform );
		}
	}

	public static MeshRenderer[] GetMeshRenderers( GameObject parentGameObject )
	{
		if( parentGameObject != null ) {
			ArrayList arrayList = new ArrayList();
			foreach( Transform transform in parentGameObject.transform ) {
				if( transform.name == "U_Char" || transform.name.StartsWith( "U_Char_" ) ) {
					_GetMeshRenderers( arrayList, transform );
				}
			}
			if( arrayList.Count == 0 ) {
				MeshRenderer meshRenderer = parentGameObject.GetComponent<MeshRenderer>();
				if( meshRenderer != null ) {
					arrayList.Add( meshRenderer );
				}
			}
			if( arrayList.Count > 0 ) {
				MeshRenderer[] meshRenderers = new MeshRenderer[arrayList.Count];
				for( int i = 0; i < arrayList.Count; ++i ) {
					meshRenderers[i] = (MeshRenderer)arrayList[i];
				}
				
				return meshRenderers;
			}
		}

		return null;
	}

	static void _GetSkinnedMeshRenderers( ArrayList skinnedMeshRenderers, Transform parentTransform )
	{
		if( parentTransform.GetComponent<Animator>() ) {
			return; // Skip this oject.
		}
		SkinnedMeshRenderer skinnedMeshRenderer = parentTransform.GetComponent<SkinnedMeshRenderer>();
		if( skinnedMeshRenderer != null ) {
			skinnedMeshRenderers.Add( skinnedMeshRenderer );
		}
		foreach( Transform childTransform in parentTransform ) {
			_GetSkinnedMeshRenderers( skinnedMeshRenderers, childTransform );
		}
	}

	public static SkinnedMeshRenderer[] GetSkinnedMeshRenderers( GameObject parentGameObject )
	{
		if( parentGameObject != null ) {
			ArrayList arrayList = new ArrayList();
			foreach( Transform transform in parentGameObject.transform ) {
				if( transform.name == "U_Char" || transform.name.StartsWith( "U_Char_" ) ) {
					_GetSkinnedMeshRenderers( arrayList, transform );
				}
			}
			if( arrayList.Count > 0 ) {
				SkinnedMeshRenderer[] skinnedMeshRenderers = new SkinnedMeshRenderer[arrayList.Count];
				for( int i = 0; i < arrayList.Count; ++i ) {
					skinnedMeshRenderers[i] = (SkinnedMeshRenderer)arrayList[i];
				}
				return skinnedMeshRenderers;
			}
		}

		return null;
	}

	// memo: Optimized for ForceAllCheckModelInScene()
	static MeshRenderer _GetMeshRenderer( Transform parentTransform )
	{
		if( parentTransform.GetComponent<Animator>() ) {
			return null; // Skip this object.
		}
		MeshRenderer meshRenderer = parentTransform.GetComponent<MeshRenderer>();
		if( meshRenderer != null ) {
			return meshRenderer;
		}
		foreach( Transform childTransform in parentTransform ) {
			meshRenderer = _GetMeshRenderer( childTransform );
			if( meshRenderer != null ) {
				return meshRenderer;
			}
		}
		return null;
	}

	// memo: Optimized for ForceAllCheckModelInScene()
	public static MeshRenderer GetMeshRenderer( GameObject parentGameObject )
	{
		if( parentGameObject != null ) {
			foreach( Transform transform in parentGameObject.transform ) {
				if( transform.name == "U_Char" || transform.name.StartsWith( "U_Char_" ) ) {
					MeshRenderer meshRenderer = _GetMeshRenderer( transform );
					if( meshRenderer != null ) {
						return meshRenderer;
					}
				}
			}
			{
				MeshRenderer meshRenderer = parentGameObject.GetComponent<MeshRenderer>();
				if( meshRenderer != null ) {
					return meshRenderer;
				}
			}
		}

		return null;
	}

	// memo: Optimized for ForceAllCheckModelInScene()
	static SkinnedMeshRenderer _GetSkinnedMeshRenderer( Transform parentTransform )
	{
		if( parentTransform.GetComponent<Animator>() ) {
			return null; // Skip this oject.
		}
		SkinnedMeshRenderer skinnedMeshRenderer = parentTransform.GetComponent<SkinnedMeshRenderer>();
		if( skinnedMeshRenderer != null ) {
			return skinnedMeshRenderer;
		}
		foreach( Transform childTransform in parentTransform ) {
			skinnedMeshRenderer = _GetSkinnedMeshRenderer( childTransform );
			if( skinnedMeshRenderer != null ) {
				return skinnedMeshRenderer;
			}
		}
		return null;
	}
	
	// memo: Optimized for ForceAllCheckModelInScene()
	public static SkinnedMeshRenderer GetSkinnedMeshRenderer( GameObject parentGameObject )
	{
		if( parentGameObject != null ) {
			foreach( Transform transform in parentGameObject.transform ) {
				if( transform.name == "U_Char" || transform.name.StartsWith( "U_Char_" ) ) {
					SkinnedMeshRenderer skinnedMeshRenderer = _GetSkinnedMeshRenderer( transform );
					if( skinnedMeshRenderer != null ) {
						return skinnedMeshRenderer;
					}
				}
			}
		}
		
		return null;
	}

	//------------------------------------------------------------------------------------------------------------------------------------------------

	public static GameObject[] FindRootObjects()
	{
		return Array.FindAll( GameObject.FindObjectsOfType<GameObject>(), (item) => item.transform.parent == null );
	}

	//------------------------------------------------------------------------------------------------------------------------------------------------

	public static readonly string ExtensionAnimBytesLower = ".anim.bytes";
	public static readonly string ExtensionAnimBytesUpper = ".ANIM.BYTES";
	
	public static bool IsExtensionAnimBytes( string name )
	{
		if( name != null ) {
			int length = name.Length;
			int extLength = ExtensionAnimBytesLower.Length;
			if( length >= extLength ) {
				for( int i = 0; i < extLength; ++i ) {
					if( name[length - extLength + i] == ExtensionAnimBytesLower[i] ||
					   name[length - extLength + i] == ExtensionAnimBytesUpper[i] ) {
						continue;
					} else {
						return false;
					}
				}
				
				return true;
			}
		}
		
		return false;
	}

	public static bool IsDebugShader( Material material )
	{
		if( material != null ) {
			if( material.shader != null && material.shader.name != null &&
			   material.shader.name.StartsWith("MMD4Mecanim")&& material.shader.name.Contains("Test") ) {
				return true;
			}
		}
		
		return false;
	}

	public static bool IsDeferredShader( Material material )
	{
		if( material != null ) {
			if( material.shader != null && material.shader.name != null &&
			   material.shader.name.StartsWith("MMD4Mecanim")&& material.shader.name.Contains("Deferred") ) {
				return true;
			}
		}

		return false;
	}

	//------------------------------------------------------------------------------------------------------------------------------------------------

	public struct FastVector3
	{
		public Vector3	value;
		int				_isZero;

		public FastVector3( Vector3 value, bool isZero )
		{
			this.value = value;
			_isZero = isZero ? 1 : 0;
		}
		
		public FastVector3( Vector3 value )
		{
			this.value = value;
			_isZero = -1;
		}
		
		public FastVector3( ref Vector3 value )
		{
			this.value = value;
			_isZero = -1;
		}

		public bool isZero
		{
			get {
				if( _isZero == -1 ) {
					_isZero = (this.value == Vector3.zero) ? 1 : 0;
				}
				return _isZero != 0;
			}
		}

		public override bool Equals( System.Object obj )
		{
			if( obj is FastVector3 ) {
				return this.Equals( (FastVector3)obj );
			}
			return false;
		}
		
		public bool Equals( FastVector3 rhs )
		{
			return this.value == rhs.value;
		}

		public override int GetHashCode()
		{
			return this.value.GetHashCode();
		}

		public static FastVector3 zero { get { return new FastVector3( Vector3.zero, true ); } }
		public static implicit operator Vector3( FastVector3 v ) { return v.value; }
		public static implicit operator FastVector3( Vector3 v ) { return new FastVector3( ref v ); }
		public static bool operator==( FastVector3 lhs, Vector3 rhs ) { return lhs.value == rhs; }
		public static bool operator!=( FastVector3 lhs, Vector3 rhs ) { return lhs.value != rhs; }

		public static FastVector3 operator+( FastVector3 lhs, Vector3 rhs )
		{
			if( lhs.isZero ) {
				return new FastVector3( rhs );
			} else {
				return new FastVector3( lhs.value + rhs );
			}
		}

		public static FastVector3 operator+( Vector3 lhs, FastVector3 rhs )
		{
			if( rhs.isZero ) {
				return new FastVector3( lhs );
			} else {
				return new FastVector3( lhs + rhs.value );
			}
		}

		public static FastVector3 operator+( FastVector3 lhs, FastVector3 rhs )
		{
			if( lhs.isZero && rhs.isZero ) {
				return lhs;
			} else if( lhs.isZero ) {
				return rhs;
			} else if( rhs.isZero ) {
				return lhs;
			} else {
				return new FastVector3( lhs.value + rhs.value );
			}
		}
	}
	
	public struct FastQuaternion
	{
		public Quaternion	value;
		int					_isIdentity;
		
		public FastQuaternion( Quaternion value, bool isIdentity )
		{
			this.value = value;
			_isIdentity = isIdentity ? 1 : 0;
		}
		
		public FastQuaternion( Quaternion value )
		{
			this.value = value;
			_isIdentity = -1;
		}
		
		public FastQuaternion( ref Quaternion value )
		{
			this.value = value;
			_isIdentity = -1;
		}

		public bool isIdentity
		{
			get {
				if( _isIdentity == -1 ) {
					_isIdentity = (this.value == Quaternion.identity) ? 1 : 0;
				}
				return _isIdentity != 0;
			}
		}

		public override bool Equals( System.Object obj )
		{
			if( obj is FastQuaternion ) {
				return this.Equals( (FastQuaternion)obj );
			}
			return false;
		}

		public bool Equals( FastQuaternion rhs )
		{
			return this.value == rhs.value;
		}

		public override int GetHashCode()
		{
			return this.value.GetHashCode();
		}

		public static FastQuaternion identity { get { return new FastQuaternion( Quaternion.identity, true ); } }
		public static implicit operator Quaternion( FastQuaternion q ) { return q.value; }
		public static implicit operator FastQuaternion( Quaternion q ) { return new FastQuaternion( q ); }
		public static bool operator==( FastQuaternion lhs, Quaternion rhs ) { return lhs.value == rhs; }
		public static bool operator!=( FastQuaternion lhs, Quaternion rhs ) { return lhs.value != rhs; }

		public static FastQuaternion operator*( FastQuaternion lhs, Quaternion rhs )
		{
			if( lhs.isIdentity ) {
				return new FastQuaternion( rhs );
			} else {
				return new FastQuaternion( lhs.value * rhs );
			}
		}
		
		public static FastQuaternion operator*( Quaternion lhs, FastQuaternion rhs )
		{
			if( rhs.isIdentity ) {
				return new FastQuaternion( lhs );
			} else {
				return new FastQuaternion( lhs * rhs.value );
			}
		}

		public static FastQuaternion operator*( FastQuaternion lhs, FastQuaternion rhs )
		{
			if( lhs.isIdentity && rhs.isIdentity ) {
				return lhs;
			} else if( lhs.isIdentity ) {
				return rhs;
			} else if( rhs.isIdentity ) {
				return lhs;
			} else {
				return new FastQuaternion( lhs.value * rhs.value, false );
			}
		}
	}

	public static int SizeOf<T>()
	{
		return System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));
	}

	public static Type WeakAddComponent<Type>( GameObject go )
		where Type : Behaviour
	{
		if( go != null ) {
			Type t = go.GetComponent<Type>();
			if( t != default(Type) ) {
				return t;
			}
			return go.AddComponent<Type>();
		}

		return null;
	}
}
