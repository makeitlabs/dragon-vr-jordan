using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JoystickHeadController : MonoBehaviour {

	private GameObject riderHead;
	private Vector3 gazeVector;

	// Use this for initialization
	void Start () {
		gazeVector.x = 0.0f;
		gazeVector.y = 0.0f;
		gazeVector.z = 0.0f;
		riderHead = GameObject.Find ("RiderHead");
	}
	
	// Update is called once per frame
	void Update () {
		gazeVector.y = 90.0f * Input.GetAxis ("RightJoystickHorizontal");
		gazeVector.x = 90.0f * Input.GetAxis ("RightJoystickVertical");
		// Debug.Log (gazeVector);
		riderHead.transform.localEulerAngles = gazeVector;
	}
}
