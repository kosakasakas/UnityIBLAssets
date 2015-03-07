//#define _SHADER_TEST

using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using MMD4Mecanim;
using MMD4MecanimProperty = MMD4MecanimImporter.PMX2FBXConfig.MMD4MecanimProperty;

public partial class MMD4MecanimImporter : ScriptableObject
{
	public const float ShaderRevision = 0.0f;

	public class MaterialBuilder
	{
		public GameObject		fbxAsset;
		public MMDModel			mmdModel;
		public MMD4MecanimProperty mmd4MecanimProperty;
		
		public string			modelDirectoryName;
		public string			texturesDirectoryName;
		public string			systemDirectoryName;

		public List< Texture >	textureList = new List<Texture>();
		public List< Texture >	systemToonTextureList = new List<Texture>();
		public List< Material >	materialList = new List<Material>();
		
		public static readonly string[] textureExtentions = new string[] {
			".bmp", ".png", ".jpg", ".tga"
		};
		
		public static readonly string[] systemToonTextureFileName = new string[] {
			"toon0.bmp",
			"toon01.bmp",
			"toon02.bmp",
			"toon03.bmp",
			"toon04.bmp",
			"toon05.bmp",
			"toon06.bmp",
			"toon07.bmp",
			"toon08.bmp",
			"toon09.bmp",
			"toon10.bmp",
		};

		//----------------------------------------------------------------------------------------------------------------

		private static bool _HasTextureTransparency( Texture texture )
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
			
			Color32[] pixels = tex2d.GetPixels32();
			for( int i = 0; i < pixels.Length; ++i ) {
				if( pixels[i].a != 255 ) {
					return true;
				}
			}
			
			return false;
		}
		
		public static bool HasTextureTransparency( Texture texture )
		{
			if( texture == null ) {
				Debug.LogWarning( "Texture is null." );
				return false;
			}
			
			string textureAssetPath = AssetDatabase.GetAssetPath( texture );
			TextureImporter textureImporter = TextureImporter.GetAtPath( textureAssetPath ) as TextureImporter;
			if( textureImporter == null ) {
				//Debug.LogWarning("TextureImporter is null."); // Memo: Unsupported .dds
				return false;
			}

			bool isReadable = textureImporter.isReadable;
			textureImporter.isReadable = true;
			MMD4MecanimEditorCommon.UpdateImportSettings( textureAssetPath );

			bool r = _HasTextureTransparency( texture );
			if( r ) { // Adding alphaIsTransparency
				System.Type t = typeof(TextureImporter);
				var property = t.GetProperty("alphaIsTransparency");
				if( property != null ) { // Unity 4.2.0f4 or Later
					if( (bool)property.GetValue( textureImporter, null ) != true ) {
						property.SetValue( textureImporter, (bool)true, null );
					}
				}
			}

			textureImporter.isReadable = isReadable;
			MMD4MecanimEditorCommon.UpdateImportSettings( textureAssetPath );
			return r;
		}

		private static TextureImporter _GetTextureImporter( Texture texture )
		{
			if( texture == null ) {
				return null;
			}
			
			string textureAssetPath = AssetDatabase.GetAssetPath( texture );
			if( string.IsNullOrEmpty( textureAssetPath ) ) {
				return null;
			}
			
			if( System.IO.Path.GetExtension( textureAssetPath ).ToLower() == ".dds" ) {
				return null;
			}
			
			TextureImporter textureImporter = TextureImporter.GetAtPath( textureAssetPath ) as TextureImporter;
			if( textureImporter == null ) {
				//Debug.LogWarning("TextureImporter is null."); // Memo: Unsupported .dds
				return null;
			}

			return textureImporter;
		}

		public static void SetTextureWrapMode( Texture texture, TextureWrapMode textureWrapMode )
		{
			if( texture == null ) {
				return;
			}

			TextureImporter textureImporter = _GetTextureImporter( texture );
			if( textureImporter == null ) {
				//Debug.LogWarning("TextureImporter is null.");
				return;
			}

			if( textureImporter.wrapMode != textureWrapMode ) {
				textureImporter.wrapMode = textureWrapMode;
				MMD4MecanimEditorCommon.UpdateImportSettings( AssetDatabase.GetAssetPath( texture ) );
			}
		}

		public static Texture ReloadTexture( Texture texture )
		{
			if( texture != null ) {
				string assetPath = AssetDatabase.GetAssetPath( texture );
				texture = AssetDatabase.LoadAssetAtPath( assetPath, typeof(Texture) ) as Texture;
				if( texture == null ) {
					Debug.LogWarning("ReloadTexture() Failed. " + assetPath);
				}
				return texture;
			}

			return null;
		}

		public static bool SetTextureCubemap( Texture texture, TextureImporterGenerateCubemap cubemap )
		{
			if( texture == null ) {
				return true;
			}

			TextureImporter textureImporter = _GetTextureImporter( texture );
			if( textureImporter == null ) {
				//Debug.LogWarning("TextureImporter is null."); // Memo: Unsupported .dds
				return false;
			}
			
			if( textureImporter.generateCubemap != cubemap ) {
				textureImporter.generateCubemap = cubemap;
				MMD4MecanimEditorCommon.UpdateImportSettings( AssetDatabase.GetAssetPath( texture ) );
			}
			return true;
		}

		public static void SetTextureAlphaIsTransparency( Texture texture, bool alphaIsTransparency )
		{
			TextureImporter textureImporter = _GetTextureImporter( texture );
			if( textureImporter == null ) {
				//Debug.LogWarning("TextureImporter is null."); // Memo: Unsupported .dds
				return;
			}

			{
				System.Type t = typeof(TextureImporter);
				var property = t.GetProperty("alphaIsTransparency");
				if( property != null ) { // Unity 4.2.0f4 or Later
					if( (bool)property.GetValue( textureImporter, null ) != alphaIsTransparency ) {
						property.SetValue( textureImporter, alphaIsTransparency, null );
						MMD4MecanimEditorCommon.UpdateImportSettings( AssetDatabase.GetAssetPath( texture ) );
					}
				}
			}
		}

		public static void SetTextureMipmapEnabled( Texture texture, bool mipmapEnabled )
		{
			TextureImporter textureImporter = _GetTextureImporter( texture );
			if( textureImporter == null ) {
				//Debug.LogWarning("TextureImporter is null."); // Memo: Unsupported .dds
				return;
			}

			if( textureImporter.mipmapEnabled != mipmapEnabled ) {
				textureImporter.mipmapEnabled = mipmapEnabled;
				MMD4MecanimEditorCommon.UpdateImportSettings( AssetDatabase.GetAssetPath( texture ) );
			}
		}

		public static void SetTextureFilterMode( Texture texture, FilterMode filterMode )
		{
			TextureImporter textureImporter = _GetTextureImporter( texture );
			if( textureImporter == null ) {
				//Debug.LogWarning("TextureImporter is null."); // Memo: Unsupported .dds
				return;
			}

			if( textureImporter.filterMode != filterMode ) {
				textureImporter.filterMode = filterMode;
				MMD4MecanimEditorCommon.UpdateImportSettings( AssetDatabase.GetAssetPath( texture ) );
			}
		}

		public static void SetTextureType( Texture texture, TextureImporterType textureType )
		{
			TextureImporter textureImporter = _GetTextureImporter( texture );
			if( textureImporter == null ) {
				//Debug.LogWarning("TextureImporter is null."); // Memo: Unsupported .dds
				return;
			}
			
			if( textureImporter.textureType != textureType ) {
				textureImporter.textureType = textureType;
				MMD4MecanimEditorCommon.UpdateImportSettings( AssetDatabase.GetAssetPath( texture ) );
			}
		}

		public static void SetTextureFormat( Texture texture, TextureImporterFormat textureFormat )
		{
			TextureImporter textureImporter = _GetTextureImporter( texture );
			if( textureImporter == null ) {
				//Debug.LogWarning("TextureImporter is null."); // Memo: Unsupported .dds
				return;
			}
			
			if( textureImporter.textureFormat != textureFormat ) {
				textureImporter.textureFormat = textureFormat;
				MMD4MecanimEditorCommon.UpdateImportSettings( AssetDatabase.GetAssetPath( texture ) );
			}
		}

		//----------------------------------------------------------------------------------------------------------------
		
		private MMD4MecanimEditorCommon.TextureAccessor _GetTextureAccessor( int textureID, MMD4MecanimProperty mmd4MecanimProperty )
		{
			if( mmd4MecanimProperty.transparency == PMX2FBXConfig.Transparency.Disable ) {
				return null;
			}

			if( (uint)textureID < (uint)this.textureList.Count && this.textureList[textureID ] != null ) {
				MMD4MecanimCommon.TextureFileSign textureFileSign = MMD4MecanimCommon.GetTextureFileSign( AssetDatabase.GetAssetPath( this.textureList[textureID ] ) );
				if( textureFileSign == MMD4MecanimCommon.TextureFileSign.None || /* for File not foud / File can't open */
					textureFileSign == MMD4MecanimCommon.TextureFileSign.BmpWithAlpha ||
					textureFileSign == MMD4MecanimCommon.TextureFileSign.TargaWithAlpha ||
					textureFileSign == MMD4MecanimCommon.TextureFileSign.PngWithAlpha ) {
					return new MMD4MecanimEditorCommon.TextureAccessor( this.textureList[textureID ] ); /* Check Alpha and set alphaIsTransparency */
				}
			}
			
			return null;
		}
		
		private bool _IsMaterialTransparency( MMDModel.Material xmlMaterial, MMD4MecanimProperty mmd4MecanimProperty )
		{
			if( mmd4MecanimProperty.transparency == PMX2FBXConfig.Transparency.Disable ) {
				return false;
			}
			if( xmlMaterial.diffuse.a < 1.0f - Mathf.Epsilon ) {
				return true;
			}
			
			return false;
		}
		
		private Shader _GetShader( MMDModel.Material xmlMaterial, MMD4MecanimProperty mmd4MecanimProperty, bool isTransparency )
		{
			System.Text.StringBuilder shaderNameBuilder = new System.Text.StringBuilder();
#if MMD4MECANIM_DEBUG
			if( mmd4MecanimProperty.isDebugShader ) {
				shaderNameBuilder.Append( "MMD4Mecanim/MMDLit-Test" );
				shaderNameBuilder.Append( "-Deferred" );

				shaderNameBuilder.Append( "-Transparent" );
				if( mmd4MecanimProperty.isDrawEdge ) {
					if( xmlMaterial.isDrawEdge ) {
						shaderNameBuilder.Append( "-Edge" );
					}
				}

				return Shader.Find( shaderNameBuilder.ToString() );
			}
#endif
			if( mmd4MecanimProperty.isDeferred ) {
				shaderNameBuilder.Append( "MMD4Mecanim/Deferred/MMDLit" );
			} else {
				shaderNameBuilder.Append( "MMD4Mecanim/MMDLit" );
			}

			if( !xmlMaterial.isDrawSelfShadowMap ) {
				shaderNameBuilder.Append( "-NoShadowCasting" );
			}
			if( xmlMaterial.isDrawBothFaces ) {
				shaderNameBuilder.Append( "-BothFaces" );
			}
			if( mmd4MecanimProperty.transparency != PMX2FBXConfig.Transparency.Disable ) {
				if( isTransparency ) {
					shaderNameBuilder.Append( "-Transparent" );
				}
			}
			if( mmd4MecanimProperty.isDrawEdge ) {
				if( xmlMaterial.isDrawEdge ) {
					shaderNameBuilder.Append( "-Edge" );
				}
			}
			return Shader.Find( shaderNameBuilder.ToString() );
		}
		
		private static string _PathCombine( string pathA, string pathB )
		{
			//return Path.Combine( pathA, pathB );
			if( string.IsNullOrEmpty( pathA ) ) {
				return pathB;
			}
			if( string.IsNullOrEmpty( pathB ) ) {
				return pathA;
			}
			
			System.Text.StringBuilder str = new System.Text.StringBuilder();
			for( int i = 0; i < pathA.Length; ++i ) {
				if( pathA[i] == '\\' ) {
					str.Append( '/' );
				} else if( pathA[i] == '.' ) {
					if( i + 1 < pathA.Length && ( pathA[i + 1] == '/' || pathA[i + 1] == '\\' ) ) {
						++i;
					} else {
						str.Append( pathA[i] );
					}
				} else {
					str.Append( pathA[i] );
				}
			}
			
			if( pathA[pathA.Length - 1] != '/' && pathA[pathA.Length - 1] != '\\' ) {
				str.Append( '/' );
			}

			for( int i = 0; i < pathB.Length; ++i ) {
				if( pathB[i] == '\\' ) {
					str.Append( '/' );
				} else if( pathB[i] == '.' ) {
					if( i + 1 < pathB.Length && ( pathB[i + 1] == '/' || pathB[i + 1] == '\\' ) ) {
						++i;
					} else {
						str.Append( pathB[i] );
					}
				} else {
					str.Append( pathB[i] );
				}
			}

			return str.ToString();
		}

		private Texture _LoadTexture( string directoryName, string fileName )
		{
			if( string.IsNullOrEmpty( fileName ) ) {
				return null;
			}
			
			string textureAssetPath = _PathCombine( directoryName, fileName );
			//Debug.Log( "Try to open:" + textureAssetPath );
			Texture texture = AssetDatabase.LoadAssetAtPath( textureAssetPath, typeof(Texture) ) as Texture;
			if( texture != null ) {
				return texture;
			}
			
			string extension = System.IO.Path.GetExtension( fileName ).ToLower();
			if( extension == ".spa" || extension == ".sph" ) {
				for( int i = 0; i < textureExtentions.Length; ++i ) {
					string newTextureAssetPath = textureAssetPath + textureExtentions[i];
					texture = AssetDatabase.LoadAssetAtPath( newTextureAssetPath, typeof(Texture) ) as Texture;
					if( texture != null ) {
						return texture;
					}
				}

				MMD4MecanimCommon.TextureFileSign textureFileSign = MMD4MecanimCommon.GetTextureFileSign( textureAssetPath );
				if( textureFileSign != MMD4MecanimCommon.TextureFileSign.None ) {
					string newTextureAssetPath = textureAssetPath;
					switch( textureFileSign ) {
					case MMD4MecanimCommon.TextureFileSign.Bmp:
					case MMD4MecanimCommon.TextureFileSign.BmpWithAlpha:
						newTextureAssetPath += ".bmp";
						break;
					case MMD4MecanimCommon.TextureFileSign.Png:
					case MMD4MecanimCommon.TextureFileSign.PngWithAlpha:
						newTextureAssetPath += ".png";
						break;
					case MMD4MecanimCommon.TextureFileSign.Jpeg:
						newTextureAssetPath += ".jpg";
						break;
					case MMD4MecanimCommon.TextureFileSign.Targa:
					case MMD4MecanimCommon.TextureFileSign.TargaWithAlpha:
						newTextureAssetPath += ".tga";
						break;
					default:
						Debug.LogWarning( "_LoadTexture: Unknown .sph/.spa file format. " + textureAssetPath );
						return null;
					}
					
					if( !System.IO.File.Exists( newTextureAssetPath ) ) {
						if( directoryName == this.texturesDirectoryName ) {
							System.IO.File.Move( textureAssetPath, newTextureAssetPath );
						} else {
							System.IO.File.Copy( textureAssetPath, newTextureAssetPath );
						}
						
						AssetDatabase.ImportAsset( newTextureAssetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport );
					} else {
						Debug.LogWarning( "_LoadTexture: Unknown flow." );
					}
					
					texture = AssetDatabase.LoadAssetAtPath( newTextureAssetPath, typeof(Texture) ) as Texture;
					if( texture != null ) {
						return texture;
					}
				}
			}

			return null;
		}

		private static string _PrefixFileName( string fileName )
		{
			if( fileName != null ) {
				int endPos = fileName.Length - 1;
				for( ; endPos >= 0; --endPos ) {
					char c = fileName[endPos];
					if( c != ' ' && c != '\r' && c != '\n' && c != '\t' ) {
						break;
					}
				}
				
				return fileName.Substring( 0, endPos + 1 );
			}
			
			return fileName;
		}

		private Texture _LoadTexture( string fileName )
		{
			fileName = _PrefixFileName( fileName );

			Texture texture = _LoadTexture( this.texturesDirectoryName, fileName );
			if( texture != null ) {
				return texture;
			}
			texture = _LoadTexture( this.modelDirectoryName, fileName );
			if( texture != null ) {
				return texture;
			}
			texture = _LoadTexture( this.systemDirectoryName, fileName );
			if( texture != null ) {
				return texture;
			}

			Debug.Log( "_LoadTexture: File not found. " + fileName );
			return null;
		}

		PMX2FBXConfig.MaterialProperty _FindMaterialProperty( Material material )
		{
			if( this.mmd4MecanimProperty != null ) {
				return this.mmd4MecanimProperty.FindMaterialProperty( material );
			}

			return null;
		}

		bool _IsMaterialPropertyProcessing( Material material )
		{
			var materialProperty = _FindMaterialProperty( material );
			if( materialProperty != null ) {
				return materialProperty.isProcessing;
			}

			return true;
		}

		public void Build( bool overwriteMaterials )
		{
			if( this.fbxAsset == null ) {
				Debug.LogError( "Not found FBX." );
				return;
			}
			
			if( this.mmdModel == null ) {
				Debug.LogError( "Not found model.xml" );
				return;
			}
			
			this.modelDirectoryName = System.IO.Path.GetDirectoryName( AssetDatabase.GetAssetPath( this.fbxAsset ) ) + "/";
			this.texturesDirectoryName = this.modelDirectoryName + "Textures/";
			string shaderDirectoryName = System.IO.Path.GetDirectoryName( AssetDatabase.GetAssetPath( Shader.Find( "MMD4Mecanim/MMDLit" ) ) );
			this.systemDirectoryName = System.IO.Path.Combine( System.IO.Path.GetDirectoryName( shaderDirectoryName ), "Textures" ) + "/";
			
			if( this.mmdModel.textureList != null ) {
				int textureListLength = this.mmdModel.textureList.Length;
				bool[] usedTextureIDList = new bool[textureListLength];
				if( this.mmdModel.materialList != null ) {
					for( int i = 0; i < this.mmdModel.materialList.Length; ++i ) {
						int textureID = this.mmdModel.materialList[i].textureID;
						int toonTextureID = this.mmdModel.materialList[i].toonTextureID;
						int additionalTextureID = this.mmdModel.materialList[i].additionalTextureID;
						if( (uint)textureID < (uint)textureListLength ) {
							usedTextureIDList[textureID] = true;
						}
						if( (uint)toonTextureID < (uint)textureListLength ) {
							usedTextureIDList[toonTextureID] = true;
						}
						if( (uint)additionalTextureID < (uint)textureListLength ) {
							usedTextureIDList[additionalTextureID] = true;
						}
					}
				}
				
				for( int i = 0; i < this.mmdModel.textureList.Length; ++i ) {
					if( usedTextureIDList[i] ) {
						string textureFileName = this.mmdModel.textureList[i].fileName;
						Texture texture = _LoadTexture( textureFileName );
						if( texture != null ) {
							this.textureList.Add( texture );
						} else {
							Debug.LogWarning( "Not found texture. " + i + ":" + textureFileName );
							this.textureList.Add( null );
						}
					} else {
						this.textureList.Add( null );
					}
				}
			}
			
			for( int i = 0; i < systemToonTextureFileName.Length; ++i ) {
				Texture texture = _LoadTexture( this.systemDirectoryName, systemToonTextureFileName[i] );
				if( texture != null ) {
					this.systemToonTextureList.Add( texture );
				} else {
					Debug.LogWarning( "Not found system toon texture. " + i + ":" + systemToonTextureFileName[i] );
					this.systemToonTextureList.Add( null );
				}
			}

			Color baseDiffuse = new Color( 1.0f, 1.0f, 1.0f, 1.0f );
			float edgeScale = 1.0f;
			float shadowLum = 1.25f;
			float selfShadow = 0.0f;
			float addLightToonCen = 0.0f;
			float addLightToonMin = 0.0f;
			PMX2FBXConfig.AutoLuminous autoLuminous = PMX2FBXConfig.AutoLuminous.Disable;
			float autoLuminousPower = 0.0f;
			if( this.mmd4MecanimProperty != null ) {
				baseDiffuse = this.mmd4MecanimProperty.baseDiffuse;
				edgeScale = this.mmd4MecanimProperty.edgeScale;
				shadowLum = this.mmd4MecanimProperty.shadowLum;
				selfShadow = this.mmd4MecanimProperty.isSelfShadow ? 1.0f : 0.0f;
				addLightToonCen = this.mmd4MecanimProperty.addLightToonCen;
				addLightToonMin = this.mmd4MecanimProperty.addLightToonMin;
				autoLuminous = this.mmd4MecanimProperty.autoLuminous;
				autoLuminousPower = this.mmd4MecanimProperty.autoLuminousPower;
			}
			
			List<MMD4MecanimEditorCommon.TextureAccessor> textureAccessorList = new List<MMD4MecanimEditorCommon.TextureAccessor>();
			for( int i = 0; i < this.textureList.Count; ++i ) {
				textureAccessorList.Add( _GetTextureAccessor( i, this.mmd4MecanimProperty ) );
			}
			
			this.materialList = MMD4MecanimImporter._GetFBXMaterialList( this.fbxAsset );
			MMD4MecanimEditorCommon.MMDMesh mmdMesh = new MMD4MecanimEditorCommon.MMDMesh( this.fbxAsset );
			if( this.materialList != null ) {
				bool[] materialIsTransparency = new bool[this.materialList.Count];
				for( int i = 0; i < this.materialList.Count; ++i ) {
					if( this.mmdModel.materialList == null || i >= this.mmdModel.materialList.Length ) {
						Debug.LogError( "Out of range." + i + "/" + this.mmdModel.materialList.Length );
						continue;
					}
					
					materialIsTransparency[i] = _IsMaterialTransparency( this.mmdModel.materialList[i], this.mmd4MecanimProperty );
					if( !materialIsTransparency[i] ) {
						int textureID = this.mmdModel.materialList[i].textureID;
						if( (uint)textureID < (uint)this.textureList.Count && textureAccessorList[textureID] != null ) {
							if( textureAccessorList[textureID].isTransparency ) {
								if( mmdMesh.CheckTransparency( i, textureAccessorList[textureID] ) ) {
									materialIsTransparency[i] = true;
								}
							}
						}
					}
				}

				for( int i = 0; i < this.materialList.Count; ++i ) {
					if( this.materialList[i] == null ) {
						Debug.LogWarning( "Not found material." + i );
						continue;
					}
					if( this.mmdModel.materialList == null || i >= this.mmdModel.materialList.Length ) {
						Debug.LogError( "Out of range." + i + "/" + this.mmdModel.materialList.Length );
						continue;
					}

					bool failureAnything = false;
					Material material = this.materialList[i];
					MMDModel.Material xmlMaterial = this.mmdModel.materialList[i];

					float shaderRevision = -1.0f;
					if( material != null && material.HasProperty("_Revision") ) {
						shaderRevision = material.GetFloat("_Revision");
					}

					bool initializedShader = false;
					int shaderType = PMX2FBXConfig.GetShaderType( material );
					bool isProcessing = _IsMaterialPropertyProcessing( material );
					if( (overwriteMaterials && isProcessing) || shaderType == PMX2FBXConfig.ShaderTypeUnknown ) {
						initializedShader = true;
						shaderType = (int)PMX2FBXConfig.ShaderType.MMD4Mecanim;
						material.shader = _GetShader( xmlMaterial, this.mmd4MecanimProperty, materialIsTransparency[i] );
					}

					if( shaderType == (int)PMX2FBXConfig.ShaderType.MMD4Mecanim ) {
						if( initializedShader ) {
							material.SetColor("_Color", xmlMaterial.diffuse * baseDiffuse);
							material.SetColor("_Specular", xmlMaterial.specular * MMD4MecanimCommon.MMDLit_globalLighting);
							material.SetFloat("_Shininess", xmlMaterial.shiness);
							material.SetColor("_Ambient", xmlMaterial.ambient);
							material.SetFloat("_ShadowLum", shadowLum);

							material.SetFloat("_AddLightToonCen", addLightToonCen);
							material.SetFloat("_AddLightToonMin", addLightToonMin);
						}

						if( initializedShader || shaderRevision < ShaderRevision - 0.5f ) {
							material.SetColor("_EdgeColor", xmlMaterial.edgeColor);
							material.SetFloat("_EdgeScale", edgeScale * MMD4MecanimCommon.MMDLit_edgeScale);
							material.SetFloat("_EdgeSize", xmlMaterial.edgeSize * edgeScale * MMD4MecanimCommon.MMDLit_edgeScale);
							
							if( xmlMaterial.isDrawSelfShadow ) {
								material.SetFloat("_SelfShadow", selfShadow);
							} else {
								material.SetFloat("_SelfShadow", 0.0f);
							}
							
							Color globalAmbient = RenderSettings.ambientLight;
							Color ambient = xmlMaterial.ambient;
							Color diffuse = xmlMaterial.diffuse;
							Color tempAmbient = MMD4MecanimCommon.MMDLit_GetTempAmbient(globalAmbient, ambient);
							Color tempAmbientL = MMD4MecanimCommon.MMDLit_GetTempAmbientL(ambient);
							Color tempDiffuse = MMD4MecanimCommon.MMDLit_GetTempDiffuse(globalAmbient, ambient, diffuse);
							tempDiffuse.a = diffuse.a;
							material.SetColor("_TempAmbient", tempAmbient);
							material.SetColor("_TempAmbientL", tempAmbientL);
							material.SetColor("_TempDiffuse", tempDiffuse);
						}

						if( initializedShader ) {
							Texture texture = (xmlMaterial.textureID >= 0) ? this.textureList[xmlMaterial.textureID] : null;
							SetTextureWrapMode( texture, TextureWrapMode.Repeat );
							material.mainTexture = texture;
							material.mainTextureScale = new Vector2(1, 1);
							material.mainTextureOffset = new Vector2(0, 0);
						}

						if( initializedShader ) {
							// AutoLuminous(Use by Emissive)
							if( autoLuminous == PMX2FBXConfig.AutoLuminous.Emissive ) {
								float shiness = xmlMaterial.shiness;
								if( shiness > 100.0f ) {
									Color color = MMD4MecanimCommon.ComputeAutoLuminousEmissiveColor(
										xmlMaterial.diffuse,
										xmlMaterial.ambient,
										shiness,
										autoLuminousPower );
									material.SetColor( "_Emissive", color );
									material.SetFloat( "_ALPower", autoLuminousPower );
								} else {
									material.SetColor( "_Emissive", new Color(0,0,0,0) );
									material.SetFloat( "_ALPower", autoLuminousPower );
								}
							} else {
								material.SetColor( "_Emissive", new Color(0,0,0,0) );
								material.SetFloat( "_ALPower", 0.0f );
							}
						}

						if( initializedShader || shaderRevision < ShaderRevision - 0.5f ) {
							Texture additionalTexture = (xmlMaterial.additionalTextureID >= 0) ? this.textureList[xmlMaterial.additionalTextureID] : null;
							if( xmlMaterial.sphereMode != MMDModel.SphereMode.None ) {
								if( additionalTexture != null ) {
									SetTextureWrapMode( additionalTexture, TextureWrapMode.Clamp );
									if( SetTextureCubemap( additionalTexture, TextureImporterGenerateCubemap.Spheremap ) ) {
										additionalTexture = ReloadTexture( additionalTexture );
										material.SetTexture("_SphereCube", additionalTexture);
									} else {
										Debug.LogWarning( "SetTextureCubemap: Failed. " + additionalTexture.name );
										failureAnything = true;
									}
								} else {
									material.SetTexture("_SphereCube", null);
								}
							} else {
								material.SetTexture("_SphereCube", null);
							}
							material.SetTextureScale("_SphereCube", new Vector2(1, 1));
							material.SetTextureOffset("_SphereCube", new Vector2(0, 0));
							
							if( xmlMaterial.sphereMode != MMDModel.SphereMode.None && additionalTexture != null ) {
								if( xmlMaterial.sphereMode == MMDModel.SphereMode.Multiply ) {
									material.SetFloat("_SphereMode", 1.0f);
									material.SetFloat("_SphereAdd", 0.0f);
									material.SetFloat("_SphereMul", 1.0f);
								} else if( xmlMaterial.sphereMode == MMDModel.SphereMode.Adding ) {
									material.SetFloat("_SphereMode", 2.0f);
									material.SetFloat("_SphereAdd", 1.0f);
									material.SetFloat("_SphereMul", 0.0f);
								} else if( xmlMaterial.sphereMode == MMDModel.SphereMode.SubTexture ) {
									// Not supported.
									Debug.LogWarning( "Material [" + i + ":" + xmlMaterial.nameJp + "] Not supported SubTexture." );
									material.SetFloat("_SphereMode", 3.0f);
									material.SetFloat("_SphereAdd", 0.0f);
									material.SetFloat("_SphereMul", 0.0f);
								} else {
									material.SetFloat("_SphereMode", 0.0f);
									material.SetFloat("_SphereAdd", 0.0f);
									material.SetFloat("_SphereMul", 0.0f);
								}
							} else {
								material.SetFloat("_SphereMode", 0.0f);
								material.SetFloat("_SphereAdd", 0.0f);
								material.SetFloat("_SphereMul", 0.0f);
							}
						}

						if( initializedShader ) {
							Texture toonTexture = (xmlMaterial.toonTextureID >= 0) ? this.textureList[xmlMaterial.toonTextureID] : null;
							if( toonTexture == null && xmlMaterial.toonID > 0 ) {
								//Debug.Log( xmlMaterial.toonID );
								toonTexture = this.systemToonTextureList[xmlMaterial.toonID];
							}
							if( toonTexture != null ) {
								SetTextureWrapMode( toonTexture, TextureWrapMode.Clamp );
							}
							material.SetTexture("_ToonTex", toonTexture);
							material.SetTextureScale("_ToonTex", new Vector2(1, 1));
							material.SetTextureOffset("_ToonTex", new Vector2(0, 0));
						}

						if( !failureAnything ) {
							material.SetFloat("_Revision", ShaderRevision);
						}
					}

					//material.renderQueue = 2502 + i; // memo: No Effects.
					material.renderQueue = -1;
				}
			}
		}
	}
	
	public class IndexBuilder
	{
		public GameObject	fbxAsset;
		public MMDModel		mmdModel;

		public const int MeshCountBitShift = MMD4MecanimInternal.AuxData.IndexData.MeshCountBitShift;

		public void _Write( System.IO.MemoryStream memoryStream, uint value )
		{
			byte[] bytes = new byte[4] {
				(byte)((value) & 0xff),
				(byte)((value >> 8) & 0xff),
				(byte)((value >> 16) & 0xff),
				(byte)((value >> 24) & 0xff),
			};
			
			memoryStream.Write( bytes, 0, 4 );
		}
		
		private SkinnedMeshRenderer[] _cache_skinnedMeshRenderers = null;

		public void Build()
		{
			if( this.fbxAsset == null ) {
				Debug.LogError( "Not found FBX." );
				return;
			}
			if( this.mmdModel == null ) {
				Debug.LogError( "Not found model.xml" );
				return;
			}
			
			string indexAssetPath = MMD4MecanimImporter.GetIndexDataPath( AssetDatabase.GetAssetPath( this.fbxAsset ) );
			
			uint numVertex = this.mmdModel.globalSettings.numVertex;
			List<uint>[] indexTable = new List<uint>[numVertex];
			for( uint i = 0; i < numVertex; ++i ) {
				indexTable[i] = new List<uint>();
			}
			
			/*
				meshIndex = (uint)indexTable[morphVertexIndex] >> 24;
				vertexIndex = (uint)indexTable[morphVertexIndex] & 0x00ffffff;
			*/
			
			uint meshLength = 0;
			uint colorLength = 0;

			_cache_skinnedMeshRenderers = MMD4MecanimCommon.GetSkinnedMeshRenderers( this.fbxAsset );
			if( _cache_skinnedMeshRenderers != null ) {
				meshLength = (uint)_cache_skinnedMeshRenderers.Length;
				for( int meshIndex = 0; meshIndex < meshLength; ++meshIndex ) {
					Color32[] colors = _cache_skinnedMeshRenderers[meshIndex].sharedMesh.colors32;
					if( colors != null ) {
						colorLength += (uint)colors.Length;
						for( uint i = 0; i < (uint)colors.Length; ++i ) {
							uint index = (uint)colors[i].r | ((uint)colors[i].g << 8) | ((uint)colors[i].b << 16);
							if( index < numVertex ) {
								unchecked {
									indexTable[index].Add( ((uint)meshIndex << MeshCountBitShift) | i );
								}
							}
						}
					}
				}
			}

			System.IO.MemoryStream memoryStream = new System.IO.MemoryStream();
			unchecked {
				_Write( memoryStream, numVertex );
				_Write( memoryStream, (meshLength << MeshCountBitShift) | colorLength );
			}
			uint offset = 2 + numVertex + 1;
			for( uint i = 0; i < numVertex; ++i ) {
				_Write( memoryStream, offset );
				offset += (uint)indexTable[i].Count;
			}
			_Write( memoryStream, offset );
			for( uint i = 0; i < numVertex; ++i ) {
				for( int j = 0; j < indexTable[i].Count; ++j ) {
					_Write( memoryStream, indexTable[i][j] );
				}
			}
			memoryStream.Flush();
			
			FileStream indexStream = File.Open( indexAssetPath, FileMode.Create, FileAccess.Write, FileShare.None );
			byte[] memoryStreamArray = memoryStream.ToArray();
			indexStream.Write( memoryStreamArray, 0, (int)memoryStream.Length );
			indexStream.Close();

			AssetDatabase.ImportAsset( indexAssetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport );
		}
	}

	public class VertexBuilder
	{
		public GameObject	fbxAsset;
		public float		importScale;
		public MMDModel		mmdModel;

		public const int MeshCountBitShift = MMD4MecanimInternal.AuxData.VertexData.MeshCountBitShift;

		private SkinnedMeshRenderer[] _cache_skinnedMeshRenderers = null;

		private static int ToOriginalBoneIndex( Transform[] bones, int boneIndex )
		{
			unchecked {
				if( bones != null && (uint)boneIndex < (uint)bones.Length ) {
					string name = bones[boneIndex].name;
					if( !string.IsNullOrEmpty( name ) ) {
						return MMD4MecanimCommon.ToInt( name );
					}
				}
			}

			Debug.LogWarning("");
			return -1;
		}

		static Vector3 _ToUnityPos( Vector3 pos, float vertexScale )
		{
			// Z-Back to Z-Front(MMD/Unity as LeftHand)
			pos[0] = -pos[0] * vertexScale;
			pos[1] =  pos[1] * vertexScale;
			pos[2] = -pos[2] * vertexScale;
			return pos;
		}

		public void Build()
		{
			if( this.fbxAsset == null ) {
				Debug.LogError( "Not found FBX." );
				return;
			}
			if( this.mmdModel == null ) {
				Debug.LogError( "Not found model.xml" );
				return;
			}
		
			_cache_skinnedMeshRenderers = MMD4MecanimCommon.GetSkinnedMeshRenderers( this.fbxAsset );
			if( _cache_skinnedMeshRenderers == null ) {
				Debug.LogError( "Not found SkinnedMeshRenderers" );
				return;
			}

			string fbxAssetPath = AssetDatabase.GetAssetPath( this.fbxAsset );
			string vertexAssetPath = MMD4MecanimImporter.GetVertexDataPath( fbxAssetPath );
			string extraAssetPath = MMD4MecanimImporter.GetExtraDataPath( fbxAssetPath );

			MMD4MecanimData.ExtraData extraData = MMD4MecanimData.BuildExtraData( AssetDatabase.LoadAssetAtPath( extraAssetPath, typeof(TextAsset) ) as TextAsset );
			if( extraData == null || extraData.vertexDataList == null ) {
				Debug.LogError( "Not found extra.bytes" );
				return;
			}

			if( this.importScale == 0.0f ) {
				Debug.LogWarning( "FBX not found." );
				this.importScale = extraData.importScale;
			}

			unchecked {
				int vertexCount = extraData.vertexCount;
				int meshCount = _cache_skinnedMeshRenderers.Length;
				int meshVertexCount = 0;
				List<int> meshAttributes = new List<int>();
				List<int> meshBoneCounts = new List<int>();
				List<byte> meshBoneAttributes = new List<byte>();
				List<int> meshBoneIndices = new List<int>();
				List<int> meshVertexCounts = new List<int>();
				List<byte> meshVertexAttributes = new List<byte>();
				List<Vector3> meshVertexSDEFValues = new List<Vector3>();

				float vertexScale = extraData.vertexScale * this.importScale;

				for( int meshIndex = 0; meshIndex < _cache_skinnedMeshRenderers.Length; ++meshIndex ) {
					Transform[] bones = _cache_skinnedMeshRenderers[meshIndex].bones;
					Matrix4x4[] bindposes = _cache_skinnedMeshRenderers[meshIndex].sharedMesh.bindposes;
					Color32[] colors = _cache_skinnedMeshRenderers[meshIndex].sharedMesh.colors32;

					BoneWeight[] boneWeights = _cache_skinnedMeshRenderers[meshIndex].sharedMesh.boneWeights;
					if( bones == null || bones.Length == 0 ||
					    bindposes == null || bindposes.Length == 0 ||
					    colors == null || colors.Length == 0 ||
					    boneWeights == null || boneWeights.Length == 0 ) {
						Debug.LogWarning( "Null SkinnedMeshRenderer. Skip processing." );
						meshAttributes.Add( 0 );
						meshBoneCounts.Add( 0 );
						meshVertexCounts.Add( 0 );
						continue;
					}
					if( boneWeights.Length != colors.Length ) {
						Debug.LogError( "Unknown." );
						return;
					}

					int baseBoneIndex = meshBoneAttributes.Count;
					int baseVertexIndex = meshVertexAttributes.Count;

					int boneCount = Mathf.Min( bones.Length, bindposes.Length );
					for( int i = 0; i < boneCount; ++i ) {
						byte boneAttribute = 0;
						Matrix4x4 bindpose = bindposes[i];
						if( bindpose.GetColumn( 0 ) == new Vector4(1,0,0,0) &&
						    bindpose.GetColumn( 1 ) == new Vector4(0,1,0,0) &&
						    bindpose.GetColumn( 2 ) == new Vector4(0,0,1,0) &&
						    bindpose.GetColumn( 3 ).w == 1 ) {
							boneAttribute |= (byte)MMD4MecanimInternal.AuxData.VertexData.BoneFlags.OptimizeBindPoses;
						}
						meshBoneAttributes.Add( boneAttribute );
						meshBoneIndices.Add( ToOriginalBoneIndex( bones, i ) ); // Added.(Support boneIndices)
					}

					bool isAnySDEF = false;
					bool isAnyQDEF = false;
					for( int i = 0; i < colors.Length; ++i ) {
						uint vertexIndex = (uint)colors[i].r | ((uint)colors[i].g << 8) | ((uint)colors[i].b << 16);

						if( vertexIndex < vertexCount ) {
							MMD4MecanimData.VertexData vertexData = extraData.vertexDataList[vertexIndex];
							bool isSDEF = (vertexData.boneTransform == MMD4MecanimData.BoneTransform.SDEF);
							bool isQDEF = (vertexData.boneTransform == MMD4MecanimData.BoneTransform.QDEF);
							isAnySDEF |= isSDEF;
							isAnyQDEF |= isQDEF;
							byte vertexAttribute = 0;
							vertexAttribute |= (isSDEF ? (byte)MMD4MecanimInternal.AuxData.VertexData.VertexFlags.SDEF : (byte)0);
							vertexAttribute |= (isQDEF ? (byte)MMD4MecanimInternal.AuxData.VertexData.VertexFlags.QDEF : (byte)0);
							if( isSDEF ) {
								int originalBoneIndex0 = ToOriginalBoneIndex( bones, boneWeights[i].boneIndex0 );
								int originalBoneIndex1 = ToOriginalBoneIndex( bones, boneWeights[i].boneIndex1 );
								if( originalBoneIndex0 == vertexData.boneWeight.boneIndex0 &&
								    originalBoneIndex1 == vertexData.boneWeight.boneIndex1 ) {
									// Nothing.
								} else if(	originalBoneIndex0 == vertexData.boneWeight.boneIndex1 &&
								    		originalBoneIndex1 == vertexData.boneWeight.boneIndex0 ) {
									vertexAttribute |= (byte)MMD4MecanimInternal.AuxData.VertexData.VertexFlags.SDEFSwapIndex;
								} else {
									float weight0 = boneWeights[vertexIndex].weight0;
									float weight1 = boneWeights[vertexIndex].weight1;
									if( Mathf.Abs( weight0 - vertexData.boneWeight.weight0 ) <= Mathf.Epsilon &&
									    Mathf.Abs( weight1 - vertexData.boneWeight.weight1 ) <= Mathf.Epsilon ) {
										// Nothing.
									} else if(	Mathf.Abs( weight1 - vertexData.boneWeight.weight0 ) <= Mathf.Epsilon &&
									   			Mathf.Abs( weight0 - vertexData.boneWeight.weight1 ) <= Mathf.Epsilon ) {
										vertexAttribute |= (byte)MMD4MecanimInternal.AuxData.VertexData.VertexFlags.SDEFSwapIndex;
									} else {
										Debug.LogWarning("");
									}
								}

								byte flag = (byte)MMD4MecanimInternal.AuxData.VertexData.BoneFlags.SDEF;
								if( boneWeights[i].boneIndex0 >= 0 ) {
									meshBoneAttributes[baseBoneIndex + boneWeights[i].boneIndex0] |= flag;
								}
								if( boneWeights[i].boneIndex1 >= 0 ) {
									meshBoneAttributes[baseBoneIndex + boneWeights[i].boneIndex1] |= flag;
								}
							}
							if( isQDEF ) {
								byte flag = (byte)MMD4MecanimInternal.AuxData.VertexData.BoneFlags.QDEF;
								if( boneWeights[i].boneIndex0 >= 0 ) {
									meshBoneAttributes[baseBoneIndex + boneWeights[i].boneIndex0] |= flag;
								}
								if( boneWeights[i].boneIndex1 >= 0 ) {
									meshBoneAttributes[baseBoneIndex + boneWeights[i].boneIndex1] |= flag;
								}
								if( boneWeights[i].boneIndex2 >= 0 ) {
									meshBoneAttributes[baseBoneIndex + boneWeights[i].boneIndex2] |= flag;
								}
								if( boneWeights[i].boneIndex3 >= 0 ) {
									meshBoneAttributes[baseBoneIndex + boneWeights[i].boneIndex3] |= flag;
								}
							}

							meshVertexAttributes.Add( vertexAttribute );

							if( isSDEF ) {
								meshVertexSDEFValues.Add( _ToUnityPos( vertexData.sdefC, vertexScale ) );

								float weight0 = boneWeights[i].weight0;
								float weight1 = boneWeights[i].weight1;
								if( (vertexAttribute & (byte)MMD4MecanimInternal.AuxData.VertexData.VertexFlags.SDEFSwapIndex) != 0 ) {
									weight0 = boneWeights[i].weight1;
									weight1 = boneWeights[i].weight0;
								}

								// Preprocess for SDEF.
								Vector3 rI = vertexData.sdefR0 * weight0 + vertexData.sdefR1 * weight1; // Interpolated round
								Vector3 r0 = (vertexData.sdefR0 - rI) + vertexData.sdefC; // Compute relative round and add Center
								Vector3 r1 = (vertexData.sdefR1 - rI) + vertexData.sdefC; // Compute relative round and add Center
								meshVertexSDEFValues.Add( _ToUnityPos( r0, vertexScale ) );
								meshVertexSDEFValues.Add( _ToUnityPos( r1, vertexScale ) );

								//for Debug
								//meshVertexSDEFValues.Add( _ToUnityPos( vertexData.sdefR0, vertexScale ) );
								//meshVertexSDEFValues.Add( _ToUnityPos( vertexData.sdefR1, vertexScale ) );
							}
						} else {
							Debug.LogError("vertexIndex is overflow.");
							return;
						}
					}

					int meshAttribute = 0;
					meshAttribute |= (isAnySDEF ? (int)MMD4MecanimInternal.AuxData.VertexData.MeshFlags.SDEF : 0);
					meshAttribute |= (isAnyQDEF ? (int)MMD4MecanimInternal.AuxData.VertexData.MeshFlags.QDEF : 0);

					meshVertexCount += colors.Length;

					meshAttributes.Add( meshAttribute );
					meshBoneCounts.Add( boneCount );
					meshVertexCounts.Add( colors.Length );
					if( meshAttribute == 0 ) {
						meshBoneAttributes.RemoveRange( baseBoneIndex, meshBoneAttributes.Count - baseBoneIndex );
						meshBoneIndices.RemoveRange( baseBoneIndex, meshBoneIndices.Count - baseBoneIndex ); // Added.(Support boneIndices)
						meshVertexAttributes.RemoveRange( baseVertexIndex, meshVertexAttributes.Count - baseVertexIndex );
					}
				}

				MMD4MecanimEditorCommon.StreamBuilder streamBuilder = new MMD4MecanimEditorCommon.StreamBuilder();

				streamBuilder.AddInt( vertexCount );
				streamBuilder.AddInt( (meshCount << MeshCountBitShift) | meshVertexCount );			// meshCount | meshVertexCount
				streamBuilder.AddInt( ( meshAttributes.Count + 1 ) * 2 + meshBoneIndices.Count );	// intCount
				streamBuilder.AddInt( 2 + meshVertexSDEFValues.Count * 3 );							// floatCount
				streamBuilder.AddInt( meshBoneAttributes.Count + meshVertexAttributes.Count );		// byteCount

				int bytePos = 0; // position for byte[]
				for( int meshIndex = 0; meshIndex < meshAttributes.Count; ++meshIndex ) {
					int meshAttribute = meshAttributes[meshIndex];
					streamBuilder.AddInt( bytePos | meshAttribute );
					if( meshAttribute != 0 ) {
						bytePos += meshBoneCounts[meshIndex];
					}
				}
				streamBuilder.AddInt( bytePos );
				for( int meshIndex = 0; meshIndex < meshAttributes.Count; ++meshIndex ) {
					int meshAttribute = meshAttributes[meshIndex];
					streamBuilder.AddInt( bytePos );
					if( meshAttribute != 0 ) {
						bytePos += meshVertexCounts[meshIndex];
					}
				}
				streamBuilder.AddInt( bytePos );

				for( int i = 0; i < meshBoneIndices.Count; ++i ) {
					streamBuilder.AddInt( meshBoneIndices[i] ); // Added.(Support boneIndices)
				}

				streamBuilder.AddFloat( extraData.vertexScale );
				streamBuilder.AddFloat( this.importScale );

				for( int i = 0; i < meshVertexSDEFValues.Count; ++i ) {
					streamBuilder.AddVector3( meshVertexSDEFValues[i] );
				}
				for( int i = 0; i < meshBoneAttributes.Count; ++i ) {
					streamBuilder.AddByte( meshBoneAttributes[i] );
				}
				for( int i = 0; i < meshVertexAttributes.Count; ++i ) {
					streamBuilder.AddByte( meshVertexAttributes[i] );
				}

				streamBuilder.WriteToFile( vertexAssetPath );

				AssetDatabase.ImportAsset( vertexAssetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport );
			}
		}
	}

	private static int _ComputeFBXMaterialList(
		Material[] materials,
		string fbxAssetDirectoryName,
		Dictionary<int, Material> materialDict )
	{
		int materialCount = 0;
		if( materials != null ) {
			foreach( Material material in materials ) {
				if( material == null ) {
					continue;
				}
				string materialPath = AssetDatabase.GetAssetPath( material );
				if( string.IsNullOrEmpty( materialPath ) ) {
					Debug.LogWarning( "Material is invalid. Skip this material." + material.name );
					continue;
				}
				if( fbxAssetDirectoryName == Path.GetDirectoryName( Path.GetDirectoryName( materialPath ) ) ) {
					string materialName = Path.GetFileNameWithoutExtension( materialPath );
					int materialValue = MMD4MecanimCommon.ToInt( materialName );
					materialDict[materialValue] = material;
					materialCount = ((materialValue + 1) > materialCount) ? (materialValue + 1) : materialCount;
				}
			}
		}

		return materialCount;
	}

	public static List<Material> _GetFBXMaterialList( GameObject fbxAsset )
	{
		if( fbxAsset == null ) {
			return null;
		}

		string fbxAssetPath = AssetDatabase.GetAssetPath( fbxAsset );
		string fbxAssetDirectoryName = Path.GetDirectoryName( fbxAssetPath );

		Dictionary<int, Material> materialDict = new Dictionary<int, Material>();
		
		int materialCount = 0;
		MeshRenderer[] meshRenderers = MMD4MecanimCommon.GetMeshRenderers( fbxAsset );
		if( meshRenderers != null ) {
			foreach( MeshRenderer meshRenderer in meshRenderers ) {
				int tempMaterialCount = _ComputeFBXMaterialList( meshRenderer.sharedMaterials, fbxAssetDirectoryName, materialDict );
				if( materialCount < tempMaterialCount ) {
					materialCount = tempMaterialCount;
				}
			}
		}

		SkinnedMeshRenderer[] skinnedMeshRenderers = MMD4MecanimCommon.GetSkinnedMeshRenderers( fbxAsset );
		if( skinnedMeshRenderers != null ) {
			foreach( SkinnedMeshRenderer skinnedMeshRenderer in skinnedMeshRenderers ) {
				int tempMaterialCount = _ComputeFBXMaterialList( skinnedMeshRenderer.sharedMaterials, fbxAssetDirectoryName, materialDict );
				if( materialCount < tempMaterialCount ) {
					materialCount = tempMaterialCount;
				}
			}
		}

		List<Material> materialList = new List<Material>();
		for( int i = 0; i < materialCount; ++i ) {
			Material material = null;
			if( materialDict.TryGetValue( i, out material ) ) {
				materialList.Add( material );
			} else {
				materialList.Add( null );
			}
		}
		
		return materialList;
	}

	private void _OnInspectorGUI_Material()
	{
		if( this.pmx2fbxConfig == null || this.pmx2fbxConfig.mmd4MecanimProperty == null ) {
			return;
		}

		if( this.materialList == null ) {
			this.materialList = _GetFBXMaterialList( this.fbxAsset );
			if( this.materialList != null ) {
				this.pmx2fbxConfig.PrepareMaterialPropertyList( this.materialList.ToArray() );
			}
		}

		GUILayout.Label( "FBX", EditorStyles.boldLabel );
		GUILayout.BeginHorizontal();
		GUILayout.Space( 26.0f );
		_OnInspectorGUI_ShowFBXField();
		GUILayout.EndHorizontal();
		
		EditorGUILayout.Separator();

		var mmd4MecanimProperty = this.pmx2fbxConfig.mmd4MecanimProperty;

		GUILayout.Label( "Shader Properties", EditorStyles.boldLabel );
		GUILayout.BeginVertical();
		#if MMD4MECANIM_DEBUG
		mmd4MecanimProperty.isDebugShader = EditorGUILayout.Toggle( "Debug", mmd4MecanimProperty.isDebugShader );
		#endif
		mmd4MecanimProperty.isDeferred = EditorGUILayout.Toggle( "Deferred", mmd4MecanimProperty.isDeferred );
		mmd4MecanimProperty.baseDiffuse = EditorGUILayout.ColorField( "BaseDiffuse", mmd4MecanimProperty.baseDiffuse );
		mmd4MecanimProperty.transparency = (PMX2FBXConfig.Transparency)EditorGUILayout.EnumPopup( "Transparency", (System.Enum)mmd4MecanimProperty.transparency );
		mmd4MecanimProperty.shadowLum = EditorGUILayout.FloatField( "ShadowLum", mmd4MecanimProperty.shadowLum );
		mmd4MecanimProperty.isSelfShadow = EditorGUILayout.Toggle( "SelfShadow", mmd4MecanimProperty.isSelfShadow );
		mmd4MecanimProperty.isDrawEdge = EditorGUILayout.Toggle( "DrawEdge", mmd4MecanimProperty.isDrawEdge );
		mmd4MecanimProperty.edgeScale = EditorGUILayout.FloatField( "EdgeScale", mmd4MecanimProperty.edgeScale );
		mmd4MecanimProperty.addLightToonCen = EditorGUILayout.FloatField( "AddLightToonCen", mmd4MecanimProperty.addLightToonCen );
		mmd4MecanimProperty.addLightToonMin = EditorGUILayout.FloatField( "AddLightToonMin", mmd4MecanimProperty.addLightToonMin );
		mmd4MecanimProperty.autoLuminous = (PMX2FBXConfig.AutoLuminous)EditorGUILayout.EnumPopup( "AutoLuminous", (System.Enum)mmd4MecanimProperty.autoLuminous );
		mmd4MecanimProperty.autoLuminousPower = EditorGUILayout.FloatField( "AutoLuminousPower", mmd4MecanimProperty.autoLuminousPower );
		GUILayout.EndVertical();

		EditorGUILayout.Separator();

		if( this._editorAdvancedMode ) {
			GUILayout.Label( "Materials", EditorStyles.boldLabel );

			{
				GUILayout.BeginHorizontal();
				if( this.materialList != null ) {
					GUILayout.BeginVertical();
					foreach( var material in this.materialList ) {
						GUILayout.BeginHorizontal();
						PMX2FBXConfig.MaterialProperty materialProperty = this.pmx2fbxConfig.FindMaterialProperty( material );
						if( materialProperty != null ) {
							materialProperty.isProcessing = GUILayout.Toggle( materialProperty.isProcessing, "" );
						} else {
							GUILayout.Toggle( false, "" );
						}
						bool backupEnabled = GUI.enabled;
						GUI.enabled = materialProperty.isProcessing;
						EditorGUILayout.ObjectField( material, typeof(Material), false );
						int shaderType = PMX2FBXConfig.GetShaderType( material );
						if( shaderType == -1 ) {
							shaderType = (int)PMX2FBXConfig.ShaderType.MMD4Mecanim;
						}
						EditorGUILayout.EnumPopup( (PMX2FBXConfig.ShaderType)shaderType );
						GUI.enabled = backupEnabled;
						GUILayout.EndHorizontal();
					}
					GUILayout.EndVertical();
				}
				GUILayout.EndHorizontal();
			}

			EditorGUILayout.Separator();
		}

		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if( GUILayout.Button("Process") ) {
			SavePMX2FBXConfig();
			Process();
		}
		GUILayout.EndHorizontal();
	}
	
	public void Process()
	{
		_ProcessMaterial(
			this.fbxAsset,
			this.mmdModel,
			this.pmx2fbxConfig,
			true ); // Enable overwriteMaterials
		Debug.Log( "Processed." );
	}
	
	bool _initializeMaterialAtLeastOnce;
	bool _initializeMaterialAfterPMX2FBX;

	[System.FlagsAttribute]
	enum MaterialState
	{
		Nothing			= 0,
		Uninitialized	= 0x01,
		Previous		= 0x02,
		Normally		= 0x04,
	}

	static MaterialState _CheckMaterialState( Material material )
	{
		MaterialState materialState = MaterialState.Nothing;
		if( material != null ) {
			if( material.shader != null &&
			    material.shader.name != null &&
			    material.shader.name.StartsWith("MMD4Mecanim") ) { // Use MMD4Mecanim shader.
				if( material.HasProperty("_Revision") ) {
					if( material.GetFloat("_Revision") < ShaderRevision - 0.5f ) {
						materialState |= MaterialState.Previous;
					} else {
						materialState |= MaterialState.Normally;
					}
				} else {
					materialState |= MaterialState.Previous;
				}
			} else if( material.mainTexture != null ) { // Use other shader.
				materialState |= MaterialState.Normally;
			} else {
				materialState |= MaterialState.Uninitialized;
			}
		}
		return materialState;
	}

	static MaterialState _CheckMaterialStates( Material[] materials )
	{
		MaterialState materialState = MaterialState.Nothing;
		if( materials != null ) {
			foreach( Material material in materials ) {
				materialState |= _CheckMaterialState( material );
			}
		}
		return materialState;
	}

	bool _CheckFBXMaterialMaterial( MeshRenderer[] meshRenderers, SkinnedMeshRenderer[] skinnedMeshRenderers )
	{
		if( this.fbxAsset == null || this.fbxAssetPath == null || this.mmdModel == null || this.pmx2fbxConfig == null ) {
			return false;
		}

		bool initializeMaterial = false;
		if( !_initializeMaterialAtLeastOnce || _initializeMaterialAfterPMX2FBX ) {
			if( _initializeMaterialAfterPMX2FBX ) {
				_initializeMaterialAfterPMX2FBX = false;
				if( !File.Exists( GetIndexDataPath( this.fbxAssetPath ) ) ) {
					initializeMaterial = true;
				}
			}
			if( !initializeMaterial ) {
				MaterialState materialState = MaterialState.Nothing;
				if( meshRenderers != null ) {
					foreach( MeshRenderer meshRenderer in meshRenderers ) {
						materialState |= _CheckMaterialStates( meshRenderer.sharedMaterials );
					}
				}
				if( materialState == MaterialState.Nothing || materialState == MaterialState.Normally ) {
					if( skinnedMeshRenderers != null ) {
						foreach( SkinnedMeshRenderer skinnedMeshRenderer in skinnedMeshRenderers ) {
							materialState |= _CheckMaterialStates( skinnedMeshRenderer.sharedMaterials );
						}
					}
				}

				initializeMaterial = (materialState != MaterialState.Nothing) && (materialState != MaterialState.Normally);
			}
		}
		if( initializeMaterial ) {
			Debug.Log( "MMD4Mecanim:Initialize FBX Material:" + this.fbxAsset.name );
			_initializeMaterialAtLeastOnce = true;
			_ProcessMaterial( fbxAsset, mmdModel, pmx2fbxConfig, false ); // Not overwriteMaterials.
		}

		return initializeMaterial;
	}

	bool _CheckFBXMaterialIndex( SkinnedMeshRenderer[] skinnedMeshRenderers )
	{
		if( this.fbxAsset == null || this.mmdModel == null || this.pmx2fbxConfig == null ) {
			return false;
		}

		if( skinnedMeshRenderers != null && skinnedMeshRenderers.Length > 0 ) {
			bool initializeIndex = false;
			if( this.indexData != null ) {
				if( !MMD4MecanimInternal.AuxData.ValidateIndexData( this.indexData, skinnedMeshRenderers ) ) {
					this.indexData = null;
					initializeIndex = true;
				}
			} else {
				initializeIndex = true;
			}

			if( initializeIndex ) {
				Debug.Log( "MMD4Mecanim:Initialize FBX Index:" + this.fbxAsset.name );
				_ProcessIndex( fbxAsset, mmdModel );
				
				this.indexAsset = AssetDatabase.LoadAssetAtPath(
					GetIndexDataPath( this.fbxAssetPath ), typeof(TextAsset) ) as TextAsset;
				if( this.indexAsset != null ) {
					this.indexData = MMD4MecanimInternal.AuxData.BuildIndexData( this.indexAsset );
				}
			}

			return initializeIndex;
		}

		return false;
	}

	bool _CheckFBXMaterialVertex( SkinnedMeshRenderer[] skinnedMeshRenderers )
	{
		if( this.fbxAsset == null || this.mmdModel == null || this.pmx2fbxConfig == null ) {
			return false;
		}

		if( skinnedMeshRenderers != null && skinnedMeshRenderers.Length > 0 ) {
			bool initializeVertex = false;
			if( this.vertexData != null ) {
				if( !MMD4MecanimInternal.AuxData.ValidateVertexData( this.vertexData, skinnedMeshRenderers ) ) {
					this.vertexData = null;
					initializeVertex = true;
				}
			} else {
				initializeVertex = true;
			}
			
			if( !initializeVertex ) {
				if( this.mmdModel != null && this.mmdModel.globalSettings != null ) { // Use for FastCheck.
					if( this.vertexData != null ) {
						float vertexScale = (float)this.mmdModel.globalSettings.vertexScale;
						float importScale = this.fbxAssertImportScale;
						if( importScale == 0.0f ) {
							importScale = (float)this.mmdModel.globalSettings.importScale;
						}
						
						float importScaleSub = importScale - this.vertexData.importScale;
						float vertexScaleSub = vertexScale - this.vertexData.vertexScale;
						if( Mathf.Abs( importScaleSub ) > Mathf.Epsilon ||
						    Mathf.Abs( vertexScaleSub ) > Mathf.Epsilon ) { // Unstable.(Require detail check.)
							string extraAssetPath = MMD4MecanimImporter.GetExtraDataPath( AssetDatabase.GetAssetPath( this.fbxAsset ) );
							MMD4MecanimData.ExtraData extraData = MMD4MecanimData.BuildExtraData( AssetDatabase.LoadAssetAtPath( extraAssetPath, typeof(TextAsset) ) as TextAsset );
							if( extraData != null ) { // Detail check.
								vertexScaleSub = (float)extraData.vertexScale - this.vertexData.vertexScale;
								if( Mathf.Abs( importScaleSub ) > Mathf.Epsilon ||
								    Mathf.Abs( vertexScaleSub ) > Mathf.Epsilon ) {
									initializeVertex = true;
								}
							}
						}
					} else {
						initializeVertex = true;
					}
				}
			}
			if( initializeVertex ) {
				string extraAssetPath = MMD4MecanimImporter.GetExtraDataPath( AssetDatabase.GetAssetPath( this.fbxAsset ) );
				if( !System.IO.File.Exists( extraAssetPath ) ) {
					initializeVertex = false;
				} else {
					// Wait for Importing.
					if( AssetDatabase.LoadAssetAtPath( extraAssetPath, typeof(TextAsset) ) == null ) {
						#if MMD4MECANIM_DEBUG
						Debug.LogWarning("_CheckFBXMaterial: Force import extraAssetPath:" + extraAssetPath);
						#endif
						AssetDatabase.ImportAsset( extraAssetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport );
					}
				}
			}
			if( initializeVertex ) {
				Debug.Log( "MMD4Mecanim:Initialize FBX Vertex:" + this.fbxAsset.name );
				_ProcessVertex( fbxAsset, this.fbxAssertImportScale, mmdModel );
				
				this.vertexAsset = AssetDatabase.LoadAssetAtPath(
					GetVertexDataPath( this.fbxAssetPath ), typeof(TextAsset) ) as TextAsset;
				if( this.vertexAsset != null ) {
					this.vertexData = MMD4MecanimInternal.AuxData.BuildVertexData( this.vertexAsset );
				}
			}

			return initializeVertex;
		}
		
		return false;
	}

	public void _CheckFBXMaterial()
	{
		if( this.fbxAsset == null || this.mmdModel == null || this.pmx2fbxConfig == null ) {
			return;
		}

		MeshRenderer[] meshRenderers = MMD4MecanimCommon.GetMeshRenderers( fbxAsset.gameObject );
		SkinnedMeshRenderer[] skinnedMeshRenderers = MMD4MecanimCommon.GetSkinnedMeshRenderers( fbxAsset.gameObject );

		bool isCreateNewAssets = false;
		_CheckFBXMaterialMaterial( meshRenderers, skinnedMeshRenderers );
		isCreateNewAssets |= _CheckFBXMaterialIndex( skinnedMeshRenderers );
		isCreateNewAssets |= _CheckFBXMaterialVertex( skinnedMeshRenderers );
		if( isCreateNewAssets ) {
			AssetDatabase.Refresh(); // for Mac(NFD)
		}
	}

	public void _CheckFBXMaterialVertex()
	{
		if( this.fbxAsset == null || this.mmdModel == null || this.pmx2fbxConfig == null ) {
			return;
		}
		
		SkinnedMeshRenderer[] skinnedMeshRenderers = MMD4MecanimCommon.GetSkinnedMeshRenderers( fbxAsset.gameObject );
		_CheckFBXMaterialVertex( skinnedMeshRenderers );
	}

	private static void _ProcessMaterial(
		GameObject fbxAsset,
		MMDModel mmdModel,
		PMX2FBXConfig pmx2fbxConfig,
		bool overwriteMaterials )
	{
		if( fbxAsset == null ) {
			Debug.LogError( "FBXAsset is null." );
			return;
		}
		if( mmdModel == null ) {
			Debug.LogError( "MMDModel is null." );
			return;
		}
		if( pmx2fbxConfig == null ) {
			Debug.LogError( "pmx2fbxConfig is null." );
			return;
		}
		
		MaterialBuilder materialBuilder = new MaterialBuilder();
		materialBuilder.fbxAsset = fbxAsset;
		materialBuilder.mmdModel = mmdModel;
		materialBuilder.mmd4MecanimProperty = pmx2fbxConfig.mmd4MecanimProperty;
		materialBuilder.Build( overwriteMaterials );
	}

	private static void _ProcessIndex( GameObject fbxAsset, MMDModel mmdModel )
	{
		if( fbxAsset == null ) {
			Debug.LogError( "FBXAsset is null." );
			return;
		}
		if( mmdModel == null ) {
			Debug.LogError( "MMDModel is null." );
			return;
		}

		IndexBuilder indexBuilder = new IndexBuilder();
		indexBuilder.fbxAsset = fbxAsset;
		indexBuilder.mmdModel = mmdModel;
		indexBuilder.Build();
	}

	private static void _ProcessVertex( GameObject fbxAsset, float importScale, MMDModel mmdModel )
	{
		if( fbxAsset == null ) {
			Debug.LogError( "FBXAsset is null." );
			return;
		}
		if( mmdModel == null ) {
			Debug.LogError( "MMDModel is null." );
			return;
		}
		
		VertexBuilder vertexBuilder = new VertexBuilder();
		vertexBuilder.fbxAsset = fbxAsset;
		vertexBuilder.importScale = importScale;
		vertexBuilder.mmdModel = mmdModel;
		vertexBuilder.Build();
	}
}

