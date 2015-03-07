using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public partial class MMD4MecanimEditorCommon
{
	public const int BulletPhysicsExecutionOrder = 10001;


	static bool _IsExtension( string name, string lowerExt, string upperExt )
	{
		if( name != null ) {
			int length = name.Length;
			int extLength = lowerExt.Length;
			if( length >= extLength ) {
				for( int i = 0; i < extLength; ++i ) {
					if( name[length - extLength + i] == lowerExt[i] ||
					    name[length - extLength + i] == upperExt[i] ) {
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

	public static bool IsExtensionAnimBytes( string name )
	{
		return _IsExtension( name, ".anim.bytes", ".ANIM.BYTES" );
	}

	public static bool IsExtensionDDS( string name )
	{
		return _IsExtension( name, ".dds", ".DDS" );
	}

	public static bool IsExtensionVMD( string name )
	{
		return _IsExtension( name, ".vmd", ".VMD" );
	}
	
	public static bool IsExtensionFBX( string name )
	{
		return _IsExtension( name, ".fbx", ".FBX" );
	}

	public static bool IsExtensionXML( string name )
	{
		return _IsExtension( name, ".xml", ".XML" );
	}

	public static bool IsExtensionPMDorPMX( string name )
	{
		if( name != null ) {
			int length = name.Length;
			if( length >= 4 ) {
				if( name[length - 4] == '.'
				   && (name[length - 3] == 'p' || name[length - 3] == 'P')
				   && (name[length - 2] == 'm' || name[length - 2] == 'M') ) {
					return (name[length - 1] == 'd' || name[length - 1] == 'D')
						|| (name[length - 1] == 'x' || name[length - 1] == 'X');
				}
			}
		}
		
		return false;
	}

	public static string GetPathWithoutExtension( string assetPath, string ext )
	{
		if( ext != null && ext.Length > 0 ) {
			return GetPathWithoutExtension( assetPath, ext.Length );
		} else {
			return assetPath;
		}
	}

	public static string GetPathWithoutExtension( string assetPath )
	{
		return GetPathWithoutExtension( assetPath, 4 );
	}
	
	public static string GetPathWithoutExtension( string fbxAssetPath, int periodPos )
	{
		if( !string.IsNullOrEmpty( fbxAssetPath ) ) {
			int length = fbxAssetPath.Length;
			if( length >= periodPos && fbxAssetPath[length - periodPos] == '.' ) {
				return fbxAssetPath.Substring( 0, length - periodPos );
			} else {
				return System.IO.Path.Combine(
					System.IO.Path.GetDirectoryName( fbxAssetPath ),
					System.IO.Path.GetFileNameWithoutExtension( fbxAssetPath ) );
			}
		}
		
		return null;
	}

	public static System.DateTime GetLastWriteTime( string path )
	{
		try {
			return System.IO.File.GetLastWriteTime( path );
		} catch( System.Exception ) {
			return new System.DateTime();
		}
	}

	public static void LookLikeInspector()
	{
		System.Type t = typeof(EditorGUIUtility);
		var method = t.GetMethod("LookLikeInspector");
		if( method != null ) { // Unity 4.2.2(Legacy)
			method.Invoke( null, null );
		}
	}

	public static void LookLikeControls()
	{
		System.Type t = typeof(EditorGUIUtility);
		var method = t.GetMethod("LookLikeControls");
		if( method != null ) { // Unity 4.2.2(Legacy)
			method.Invoke( null, null );
		}
	}

	#if MMD4MECANIM_DEBUG
	// Don' touch globalScale in Debug.
	#else
	// Support flexible globalScale in FBX import settings.
	public static float GetModelImportScale( ModelImporter modelImporter )
	{
		if( modelImporter != null ) {
			return modelImporter.globalScale;
		}
		
		return 0.01f;
	}
	#endif

	public static void UpdateImportSettings( string path )
	{
		AssetDatabase.WriteImportSettingsIfDirty( path );
		AssetDatabase.ImportAsset( path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport );
	}

	public static void ValidateScriptExecutionOrder()
	{
		if( !EditorApplication.isPlaying ) {
			GameObject go = EditorUtility.CreateGameObjectWithHideFlags(
				"MMD4MecanimBulletPhysicsHidden",
				HideFlags.HideAndDontSave,
				typeof(MMD4MecanimBulletPhysics) );
			
			MMD4MecanimBulletPhysics bulletPhysics = go.GetComponent< MMD4MecanimBulletPhysics >();
			if( bulletPhysics != null ) {
				MonoScript script = MonoScript.FromMonoBehaviour( bulletPhysics );
				int executionOrder = MonoImporter.GetExecutionOrder( script );
				if( executionOrder != BulletPhysicsExecutionOrder ) {
					MonoImporter.SetExecutionOrder( script, BulletPhysicsExecutionOrder );
				}
			}
			
			GameObject.DestroyImmediate( go );
		}
	}

	public class TextureAccessor
	{
		public int width;
		public int height;
		public Color32[] pixels;
		public bool isTransparency;
		
		public TextureAccessor()
		{
		}
		
		public TextureAccessor( Texture texture )
		{
			Lock( texture );
		}
		
		public bool Lock( Texture texture )
		{
			this.width = 0;
			this.height = 0;
			this.pixels = null;
			this.isTransparency = false;
			
			if( texture == null ) {
				Debug.LogWarning( "Texture is null." );
				return false;
			}
			
			this.width = texture.width;
			this.height = texture.height;
			
			string textureAssetPath = AssetDatabase.GetAssetPath( texture );
			if( string.IsNullOrEmpty( textureAssetPath ) ) {
				Debug.LogWarning( "Texture is null." );
				return false;
			}

			if( IsExtensionDDS( textureAssetPath ) ) {
				this.isTransparency = _HasTextureTransparency( texture );
				return true;
			}
			
			TextureImporter textureImporter = TextureImporter.GetAtPath( textureAssetPath ) as TextureImporter;
			if( textureImporter == null ) {
				Debug.LogWarning( "Texture is null." );
				return false;
			}
	
			bool isReadable = textureImporter.isReadable;
			if( !isReadable ) {
				textureImporter.isReadable = true;
				MMD4MecanimEditorCommon.UpdateImportSettings( textureAssetPath );
			}
			
			this.isTransparency = _HasTextureTransparency( texture );
			if( this.isTransparency ) { // Adding alphaIsTransparency
				System.Type t = typeof(TextureImporter);
				var property = t.GetProperty("alphaIsTransparency");
				if( property != null ) { // Unity 4.2.0f4 or Later
					if( (bool)property.GetValue( textureImporter, null ) != true ) {
						property.SetValue( textureImporter, (bool)true, null );
						if( !isReadable ) {
							MMD4MecanimEditorCommon.UpdateImportSettings( textureAssetPath );
						}
					}
				}
			}
	
			if( !isReadable ) {
				textureImporter.isReadable = isReadable;
				MMD4MecanimEditorCommon.UpdateImportSettings( textureAssetPath );
			}
			return true;
		}

		private bool _HasTextureTransparency( Texture texture )
		{
			if( texture == null ) {
				Debug.LogError("_HasTextureTransparency:Unkonwn Flow.");
				return false;
			}
			
			Texture2D tex2d = texture as Texture2D;
			if( tex2d == null ) {
				Debug.LogError("_HasTextureTransparency:Unkonwn Flow.");
				return false;
			}

			try {
				this.pixels = tex2d.GetPixels32();
			} catch( System.Exception e ) {
				Debug.LogError( e.ToString() );
				Debug.LogError( AssetDatabase.GetAssetPath( texture ) );
				return true; // Lock is missing.(Counts as Transparency)
			}
			if( this.pixels == null ) {
				return true; // Lock is missing.(Counts as Transparency)
			}
			
			for( int i = 0; i < this.pixels.Length; ++i ) {
				if( this.pixels[i].a != 255 ) {
					return true;
				}
			}
			
			return false;
		}
	}
	
	public class AlphaUVChecker
	{
		struct Point
		{
			public int x, y;
			public Point( int x_, int y_ ) { x = x_; y = y_; }
		}
		
		static void _InitLineMinMax( List<Point> lineMinMax, int yMin, int yMax )
		{
			lineMinMax.Clear();
			int count = yMax - yMin + 1;
			for( int i = 0; i < count; ++i ) {
				int y = (1 << (sizeof(int) * 8 - 1));
				int x = y - 1;
				lineMinMax.Add( new Point( x, y ) );
			}
		}
		
		static void _ComputeLineMinMax( List<Point> lineMinMax, int yMin, Point pt1, Point pt2 )
		{
			uint yCount = (uint)lineMinMax.Count;
			int dx = Mathf.Abs(pt1.x - pt2.x);
			int dy = Mathf.Abs(pt1.y - pt2.y);
			int	s = 0, step = 0;
			uint idx = 0;
			
			if( dx > dy ) {
				if( pt1.x > pt2.x ) {
					step = (pt1.y > pt2.y)? 1: -1;
					s = pt1.x;
					pt1.x = pt2.x;
					pt2.x = s;
					pt1.y = pt2.y;
				}else{
					step = (pt1.y < pt2.y)? 1: -1;
				}
				
				idx = (uint)(pt1.y - yMin);
				if( idx < yCount ) {
					Point pt;
					pt.x = Mathf.Min( lineMinMax[(int)idx].x, pt1.x );
					pt.y = Mathf.Max( lineMinMax[(int)idx].y, pt1.x );
					lineMinMax[(int)idx] = pt;
				}
				
				s = dx >> 1;
				while( ++pt1.x <= pt2.x ) {
					if( (s -= dy) < 0 ) {
						s += dx;
						pt1.y += step;
					}
					
					idx = (uint)(pt1.y - yMin);
					if( idx < yCount ) {
						Point pt;
						pt.x = Mathf.Min( lineMinMax[(int)idx].x, pt1.x );
						pt.y = Mathf.Max( lineMinMax[(int)idx].y, pt1.x );
						lineMinMax[(int)idx] = pt;
					}
				}
			} else {
				if( pt1.y > pt2.y ) {
					step = (pt1.x > pt2.x) ? 1 : -1;
					s = pt1.y;
					pt1.y = pt2.y;
					pt2.y = s;
					pt1.x = pt2.x;
				}else{
					step = (pt1.x < pt2.x) ? 1 : -1;
				}
				
				idx = (uint)(pt1.y - yMin);
				if( idx < yCount ) {
					Point pt;
					pt.x = Mathf.Min( lineMinMax[(int)idx].x, pt1.x );
					pt.y = Mathf.Max( lineMinMax[(int)idx].y, pt1.x );
					lineMinMax[(int)idx] = pt;
				}
				
				s = dy >> 1;
				while( ++pt1.y <= pt2.y ) {
					if((s -= dx) < 0){
						s += dy;
						pt1.x += step;
					}
					
					idx = (uint)(pt1.y - yMin);
					if( idx < yCount ) {
						Point pt;
						pt.x = Mathf.Min( lineMinMax[(int)idx].x, pt1.x );
						pt.y = Mathf.Max( lineMinMax[(int)idx].y, pt1.x );
						lineMinMax[(int)idx] = pt;
					}
				}
			}
		}
		
		static void _Swap( ref Point xy0, ref Point xy1 )
		{
			Point t = xy0;
			xy0 = xy1;
			xy1 = t;
		}
		
		static void _SortTriangleByHeight( ref Point xy0, ref Point xy1, ref Point xy2 )
		{
			if( xy0.y > xy1.y ) {
				_Swap( ref xy0, ref xy1 );
			}
			if( xy0.y > xy2.y ) {
				_Swap( ref xy0, ref xy2 );
			}
			if( xy1.y > xy2.y ) {
				_Swap( ref xy1, ref xy2 );
			}
		}

		public bool[] pixels;
		public int width;
		public int height;
		float f_width;
		float f_height;
		List<Point> lineMinMax = new List<Point>();

		public void Create( int width, int height )
		{
			this.width = width;
			this.height = height;
			this.f_width = (float)width;
			this.f_height = (float)height;
			this.pixels = new bool[width * height];
		}
		
		Point _ToPoint( Vector2 uv )
		{
			return new Point( (int)(uv.x * this.f_width), (int)(uv.y * this.f_height) );
		}
		
		public void SetTriangle( Vector2 uv0, Vector2 uv1, Vector2 uv2 )
		{
			if( this.width == 0 || this.height == 0 ) {
				return;
			}
			
			Point vertex0 = _ToPoint( uv0 );
			Point vertex1 = _ToPoint( uv1 );
			Point vertex2 = _ToPoint( uv2 );
			_SortTriangleByHeight( ref vertex0, ref vertex1, ref vertex2 );
			_SetPixel( vertex0.x, vertex0.y );
			_SetPixel( vertex1.x, vertex1.y );
			_SetPixel( vertex2.x, vertex2.y );
			
			_InitLineMinMax( this.lineMinMax, vertex0.y, vertex2.y );
		
			int yMin = vertex0.y;
			_ComputeLineMinMax( this.lineMinMax, yMin, vertex0, vertex1 );
			_ComputeLineMinMax( this.lineMinMax, yMin, vertex1, vertex2 );
			_ComputeLineMinMax( this.lineMinMax, yMin, vertex0, vertex2 );
			
			for( int y = vertex0.y; y <= vertex2.y; ++y ) {
				int xMin = this.lineMinMax[y - yMin].x;
				int xMax = this.lineMinMax[y - yMin].y;
				for( int x = xMin; x < xMax; ++x ) {
					_SetPixel( x, y );
				}
			}
		}
		
		static int _Wrap( int x, int w )
		{
			if( x < 0 ) {
				x = -x;
			}
			if( x >= w ) {
				int d = x / w;
				int p = x % w;
				if( (d % 2) == 0 ) {
					x = p;
				} else {
					x = w - 1 - p;
				}
			}
			return x;
		}
		
		void _SetPixel( int x, int y )
		{
			x = _Wrap( x, this.width );
			y = _Wrap( y, this.height );
			if( (uint)x < (uint)this.width && (uint)y < (uint)this.height ) {
				this.pixels[x + y * this.width] = true;
			}
		}

		public bool HasTransparency( Color32[] colors )
		{
			if( this.pixels == null ) {
				return true;
			}

			for( int i = 0; i < this.pixels.Length; ++i ) {
				if( this.pixels[i] && colors[i].a != 255 ) {
					return true;
				}
			}

			return false;
		}

		public void Dump( Color32[] colors, string path )
		{
			if( this.pixels == null || colors == null ) {
				return;
			}

			Texture2D texture = new Texture2D( this.width, this.height, TextureFormat.ARGB32, false );

			Color32[] modColors = (Color32[])colors.Clone();
			for( int i = 0; i < modColors.Length; ++i ) {
				if( this.pixels[i] ) {
					if( colors[i].a != 255 ) {
						modColors[i] = new Color32( 255, 0, 0, 255 );
					} else {
						modColors[i] = new Color32( 0, 255, 0, 255 );
					}
				} else {
					modColors[i] = new Color32( 0, 0, 255, 255 );
				}
			}

			texture.SetPixels32( modColors );
			texture.Apply();

			System.IO.File.WriteAllBytes( path, texture.EncodeToPNG() );
		}
	}
	
	public class MMDMesh
	{
		MeshRenderer[] _meshRenderers;
		SkinnedMeshRenderer[] _skinnedMeshRenderers;
		
		public MMDMesh( GameObject gameObject )
		{
			if( gameObject != null ) {
				_meshRenderers = MMD4MecanimCommon.GetMeshRenderers( gameObject );
				_skinnedMeshRenderers = MMD4MecanimCommon.GetSkinnedMeshRenderers( gameObject );
			}
		}
		
		static bool _CheckTransparency( Mesh mesh, int subMeshIndex, Material material, TextureAccessor textureAccessor )
		{
			if( mesh == null || subMeshIndex >= mesh.subMeshCount || textureAccessor == null ) {
				Debug.LogError("");
				return true;
			}
			if( !textureAccessor.isTransparency ) {
				return false;
			}
			
			AlphaUVChecker alphaUVChecker = new AlphaUVChecker();
			alphaUVChecker.Create( textureAccessor.width, textureAccessor.height );
			
			Vector2[] uv = mesh.uv;
			int[] triangles = mesh.GetTriangles( subMeshIndex );
			if( uv == null || uv.Length == 0 || triangles == null || triangles.Length == 0 ) {
				Debug.LogError("");
				return false;
			}
			
			for( int i = 0; i + 2 < triangles.Length; i += 3 ) {
				alphaUVChecker.SetTriangle( uv[triangles[i + 0]], uv[triangles[i + 1]], uv[triangles[i + 2]] );
			}

			bool r = alphaUVChecker.HasTransparency( textureAccessor.pixels );
			#if MMD4MECANIM_DEBUG
			//Debug.Log("HasTransparency:" + r + " material:" + material.name + ".png" );
			//alphaUVChecker.Dump( textureAccessor.pixels, material.name + ".png" );
			#endif
			return r;
		}

		public bool CheckTransparency( int materialID, TextureAccessor textureAcccessor )
		{
			bool materialAnything = false;
			if( _meshRenderers != null ) {
				foreach( MeshRenderer meshRenderer in _meshRenderers ) {
					Material[] materials = meshRenderer.sharedMaterials;
					if( materials != null ) {
						for( int i = 0; i < materials.Length; ++i ) {
							Material material = materials[i];
							if( !string.IsNullOrEmpty( material.name ) && (uint)(material.name[0] - '0') <= 9 &&
							   MMD4MecanimCommon.ToInt( material.name ) == materialID ) {
								MeshFilter meshFilter = meshRenderer.gameObject.GetComponent< MeshFilter >();
								if( meshFilter != null && meshFilter.sharedMesh != null ) {
									materialAnything = true;
									if( _CheckTransparency( meshFilter.sharedMesh, i, material, textureAcccessor ) ) {
										return true;
									}
								}
							}
						}
					}
				}
			}

			if( _skinnedMeshRenderers != null ) {
				foreach( SkinnedMeshRenderer skinnedMeshRenderer in _skinnedMeshRenderers ) {
					Material[] materials = skinnedMeshRenderer.sharedMaterials;
					if( materials != null ) {
						for( int i = 0; i < materials.Length; ++i ) {
							Material material = materials[i];
							if( !string.IsNullOrEmpty( material.name ) && (uint)(material.name[0] - '0') <= 9 &&
								MMD4MecanimCommon.ToInt( material.name ) == materialID ) {
								materialAnything = true;
								if( _CheckTransparency( skinnedMeshRenderer.sharedMesh, i, material, textureAcccessor ) ) {
									return true;
								}
							}
						}
					}
				}
			}

			if( materialAnything ) {
				return false;
			}

			//Debug.LogWarning("Not found materialID:" + materialID);
			return true; // Safety
		}
	}

	public class StreamBuilder
	{
		public List<int> intValues = new List<int>();
		public List<float> floatValues = new List<float>();
		public List<byte> byteValues = new List<byte>();

		public int intPos
		{
			get {
				return this.intValues.Count;
			}
		}
		
		public int floatPos
		{
			get {
				return this.floatValues.Count;
			}
		}
		
		public int bytePos
		{
			get {
				return this.byteValues.Count;
			}
		}

		public int AddInt( int v )
		{
			int r = this.intValues.Count;
			this.intValues.Add( v );
			return r;
		}

		public int AddFloat( float v )
		{
			int r = this.floatValues.Count;
			this.floatValues.Add( v );
			return r;
		}

		public int AddByte( byte v )
		{
			int r = this.byteValues.Count;
			this.byteValues.Add( v );
			return r;
		}

		public void SetInt( int pos, int v )
		{
			this.intValues[pos] = v;
		}

		public void SetFloat( int pos, float v )
		{
			this.floatValues[pos] = v;
		}

		public void SetByte( int pos, byte v )
		{
			this.byteValues[pos] = v;
		}

		public int AddVector2( Vector2 v )
		{
			int r = this.floatPos;
			this.floatValues.Add( v.x );
			this.floatValues.Add( v.y );
			return r;
		}

		public int AddVector3( Vector3 v )
		{
			int r = this.floatPos;
			this.floatValues.Add( v.x );
			this.floatValues.Add( v.y );
			this.floatValues.Add( v.z );
			return r;
		}

		public void SetVector2( int pos, Vector2 v )
		{
			this.floatValues[pos + 0] = v.x;
			this.floatValues[pos + 1] = v.y;
		}

		public void SetVector3( int pos, Vector3 v )
		{
			this.floatValues[pos + 0] = v.x;
			this.floatValues[pos + 1] = v.y;
			this.floatValues[pos + 2] = v.z;
		}

		public void WriteToFile( string path )
		{
			int intLength = this.intValues.Count;
			int floatLength = this.floatValues.Count;
			int byteLength = this.byteValues.Count;

			byte[] fileBytes = new byte[intLength * 4 + floatLength * 4 + byteLength];
			int filePos = 0;
			if( intLength > 0 ) {
				System.Buffer.BlockCopy( this.intValues.ToArray(), 0, fileBytes, filePos, intLength * 4 );
				filePos += intLength * 4;
			}
			if( floatLength > 0 ) {
				System.Buffer.BlockCopy( this.floatValues.ToArray(), 0, fileBytes, filePos, floatLength * 4 );
				filePos += floatLength * 4;
			}
			if( byteLength > 0 ) {
				System.Buffer.BlockCopy( this.byteValues.ToArray(), 0, fileBytes, filePos, byteLength );
				filePos += byteLength;
			}

			FileStream indexStream = File.Open( path, FileMode.Create, FileAccess.Write, FileShare.None );
			indexStream.Write( fileBytes, 0, filePos );
			indexStream.Close();
		}
	}
}
