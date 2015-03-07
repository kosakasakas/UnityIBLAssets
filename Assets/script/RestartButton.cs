using UnityEngine;

public class RestartButton : MonoBehaviour {

	public void onClick () {
		Application.LoadLevel (Application.loadedLevelName);
	}
}
