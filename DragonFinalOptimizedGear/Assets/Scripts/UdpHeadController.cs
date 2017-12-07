using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System;

// working from example here:
// https://stackoverflow.com/questions/25625725/unity3d-and-udpclient

public class UdpHeadController : MonoBehaviour {

	Thread udpReceiveThread;
	UdpClient udp;
	public int udpPort = 3333;

	private GameObject riderHead;
	private Vector3 gazeVector;

	private float yaw;
	private float pitch;
	private float roll;

	private float yawZero= 0.0f;
	private float pitchZero = 0.0f;
	private float rollZero = 0.0f;

	// Use this for initialization
	void Start () {
		udpReceiveThread = new Thread (new ThreadStart (UdpReceive));
		udpReceiveThread.IsBackground = true;
		udpReceiveThread.Start();
		gazeVector.x = 0.0f;
		gazeVector.y = 0.0f;
		gazeVector.z = 0.0f;
		riderHead = GameObject.Find ("RiderHead");
	}
	
	// Update is called once per frame
	void Update () {
		if (Input.GetButton ("ResetView")) {
			yawZero = yaw;
			pitchZero = pitch;
			rollZero = roll;
		}
		gazeVector.z = roll - rollZero;
		gazeVector.y = yaw - yawZero;
		gazeVector.x = -(pitch - pitchZero);
		// Debug.Log (gazeVector);
		riderHead.transform.localEulerAngles = gazeVector;
	}

	// Unity Application Quit Function
	void OnApplicationQuit() {
		if (udpReceiveThread.IsAlive) {
			udpReceiveThread.Abort();
		}
		udp.Close();
	}

	private void UdpReceive() {
		udp = new UdpClient (udpPort);
		// udp.Client.Blocking = false;
		while (true) {
			try {
				IPEndPoint anyIP = new IPEndPoint (IPAddress.Any, 0);
				byte[] data = udp.Receive (ref anyIP);

				// encode UTF8-coded bytes to text format
				string text = Encoding.UTF8.GetString (data);

				// show received message
				// Debug.Log(">> " + text);				

				string [] values = text.Split('\t');
				yaw = float.Parse(values[0], System.Globalization.CultureInfo.InvariantCulture);
				pitch = float.Parse(values[1], System.Globalization.CultureInfo.InvariantCulture);
				roll = float.Parse(values[2], System.Globalization.CultureInfo.InvariantCulture);
			
				// Debug.Log(yaw.ToString());
			
			} catch (Exception err) {
				Debug.Log(err.ToString ());
			}
		}
	}
}
