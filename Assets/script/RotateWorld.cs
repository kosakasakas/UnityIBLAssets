using UnityEngine;
using System.Collections;

public class RotateWorld : MonoBehaviour {

	public GameObject targetObject = null;
	private Vector3 _oldPosition;

	// Use this for initialization
	void Start () {
		if (targetObject != null) {
			_oldPosition = targetObject.transform.localPosition;
		}
	}
	
	// Update is called once per frame
	void Update () {
		if (targetObject != null) {
			Vector3 currentPosition = targetObject.transform.localPosition;
			this.transform.localPosition += (currentPosition - _oldPosition);
			_oldPosition = currentPosition;
		}

		this.transform.Rotate ( 0, ( Input.GetAxis ( "Horizontal" ) *  -0.5f ), 0 );
		//this.transform.Rotate ( 0, 0.5f, 0 );
	}
}
