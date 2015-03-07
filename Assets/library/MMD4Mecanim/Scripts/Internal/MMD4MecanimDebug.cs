using UnityEngine;
using System.Collections;

namespace MMD4Mecanim
{
	public static class M4MDebug
	{
		[System.Diagnostics.Conditional("MMD4MECANIM_DEBUG")]
		public static void Log( string msg )
		{
			Debug.Log( msg );
		}

		[System.Diagnostics.Conditional("MMD4MECANIM_DEBUG")]
		public static void LogWarning( string msg )
		{
			Debug.LogWarning( msg );
		}

		[System.Diagnostics.Conditional("MMD4MECANIM_DEBUG")]
		public static void LogError( string msg )
		{
			Debug.LogError( msg );
		}

		[System.Diagnostics.Conditional("MMD4MECANIM_DEBUG")]
		public static void Assert( bool cmp )
		{
			if( !cmp ) {
				Debug.Break();
			}
		}
	}
}
