using UnityEngine;
using System.Collections;

public class RotateCam : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		this.transform.Rotate ( ( Input.GetAxis ( "Vertical" ) *  -0.5f ), 0, 0 );
	}
}
