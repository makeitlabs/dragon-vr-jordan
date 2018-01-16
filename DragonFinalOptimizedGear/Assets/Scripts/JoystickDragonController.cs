#region License

#endregion

using System.Collections;
using UnityEngine;
using SocketIO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System;

public class JoystickDragonController : MonoBehaviour
{
	Thread udpReceiveThread;
	UdpClient udp;
	public int udpPort = 3334; 

	// private SocketIOComponent socket;

	private Animator animator;

	public float originalMoveSpeed = 10f;
	public float moveSpeed = 10f;

	public float maxDiveSpeed = 25f;

	public float turnSpeed = 8f;
	public float upSpeed = 2f;
	public float bankSpeed = 20f;
	public float evenSpeed = 20f;

	public float rollSpeed = 50f;

	public int flyingAnimation = 0;
	public int rightAnimation = 1;
	public int leftAnimation = 2;
	public int upAnimation = 3;
	public int downAnimation = 4;
	public int rollRightAnimation = 5;
	public int rollLeftAnimation = 6;
	public int evenDownAnimation = 7;
	public int evenUpAnimation = 8;
	public int evenLeftAnimation = 9;
	public int evenRightAnimation = 10;

	private Vector3 euler; 

	private bool upCoroutineStarted = false;
	private bool downCoroutineStarted = false;
	private bool leftCoroutineStarted = false;
	private bool rightCoroutineStarted = false;
	private bool rollCoroutineStarted = false;

	public bool isFlying = false; //change this to false when cardboard is active

	private float lastFlyX = 0.0f;
	private float lastFlyY = 0.0f;

	public float maxForwardSpeed = 100.0F;
	public float maxTurnSpeed = 25.0F;

	private Vector3 turnVector;

	public bool useJoystickReins = false;

	private float udpLeftRein = 0.0F;
	private float udpRightRein = 0.0F;
	private float udpPitch = 0.0F;


	AudioSource flapping;

	public void Start() 
	{
		euler = transform.eulerAngles;
		// GameObject go = GameObject.Find("SocketIO");
		// socket = go.GetComponent<SocketIOComponent>();
		animator = GetComponent<Animator> ();
		flapping = GetComponent<AudioSource> ();

		turnVector = transform.eulerAngles;

		udpReceiveThread = new Thread (new ThreadStart (UdpReceive));
		udpReceiveThread.IsBackground = true;
		udpReceiveThread.Start ();



		////////// MOVEMENT USING EULER AND COROUTINES  //////////
		//socket.On ("left", MoveLeftEuler);
		//socket.On ("right", MoveRightEuler);
		//socket.On ("up", MoveUpEuler);
		//socket.On ("down", MoveDownEuler);
		//socket.On ("evenup", EvenUpEuler);
		//socket.On ("evendown", EvenDownEuler);
		//socket.On ("evenleft", EvenLeftEuler);
		//socket.On ("evenright", EvenRightEuler);
		//socket.On ("roll", RollEuler);
		//socket.On ("reset", ResetGame);
		//socket.On ("move", MoveDragon);

	}



	// LEFTTTTT
	public void MoveLeftEuler(){
		
		// if roll in progress block other movements
		if (rollCoroutineStarted == false) {
			Debug.Log ("LEFT");

			//Trigger Left Animation
			animator.SetInteger("DragonState", leftAnimation);

			StartCoroutine ("LeftEuler");
		} else {
			Debug.Log ("ROLL IN PROGRESS - NO LEFT");
		}
	}
	
	private IEnumerator LeftEuler(){
		flapping.mute = true;
		//if left button is pressed while dragon is evening out, cancel the evening out and turn more left
		if (leftCoroutineStarted == true) {
			Debug.Log ("CANCEL LEFT EVENING");
			leftCoroutineStarted = false;
			StopCoroutine("EvenLeftEulerCoroutine");
		}
			
		float currentTime = Time.time;
		while (Time.time <= currentTime + 1f) {
			//turning angle
			euler.y = (euler.y - turnSpeed * Time.deltaTime);
			//banking angle
			if(euler.z < 40){
				euler.z = (euler.z + bankSpeed * Time.deltaTime);
			}
			transform.eulerAngles = euler;
			yield return null;
		}
	}

	public void EvenLeftEuler(){
		
		// if roll in progress block other movements

		if (rollCoroutineStarted == false){
			Debug.Log ("EVEN LEFT");

			animator.SetInteger ("DragonState", evenLeftAnimation);

			StartCoroutine ("EvenLeftEulerCoroutine");
		} else {
			Debug.Log ("NO EVEN LEFT");
		}
	}

	private IEnumerator EvenLeftEulerCoroutine(){
		Debug.Log ("COROUTINE - EVEN LEFT");
		leftCoroutineStarted = true;
		yield return new WaitForSeconds (0.2f);
		while (euler.z > 0) {
			euler.z = (euler.z - bankSpeed * Time.deltaTime);
			transform.eulerAngles = euler;
			yield return null;
		}
		transform.eulerAngles = euler;
		leftCoroutineStarted = false;

		//Trigger Default Flying Animation
		animator.SetInteger("DragonState", flyingAnimation);
		flapping.mute = false;
	}

	/// RIGHTTTT
	public void MoveRightEuler(){
		


		// if roll in progress block other movements
		if (rollCoroutineStarted == false) {
			Debug.Log ("RIGHT");

			//Trigger Right Animation
			animator.SetInteger("DragonState", rightAnimation);

			StartCoroutine ("RightEuler");
		} else {
			Debug.Log ("ROLL IN PROGRESS - NO RIGHT");
		}
	}
	
	private IEnumerator RightEuler(){
		flapping.mute = true;
		if (rightCoroutineStarted == true) {
			Debug.Log ("CANCEL RIGHT EVENING");
			rightCoroutineStarted = false;
			StopCoroutine("EvenRightEulerCoroutine");
		}
			
		float currentTime = Time.time;
		
		while (Time.time <= currentTime + 1f) {
			euler.y = (euler.y + turnSpeed * Time.deltaTime);
			if(euler.z > -40){
				euler.z = (euler.z - bankSpeed * Time.deltaTime);
			}
			transform.eulerAngles = euler;
			yield return null;
		}
			
	}

	public void EvenRightEuler(){
		
		// if roll in progress block other movements
		if (rollCoroutineStarted == false){
			Debug.Log ("EVEN RIGHT");
			animator.SetInteger ("DragonState", evenRightAnimation);
			StartCoroutine ("EvenRightEulerCoroutine");
		} else {
			Debug.Log ("NO EVEN RIGHT");
		}
	}

	private IEnumerator EvenRightEulerCoroutine(){
		Debug.Log ("COROUTINE - EVEN RIGHT");

		rightCoroutineStarted = true;
		yield return new WaitForSeconds (0.2f);
		while (euler.z < 0) {
			euler.z = (euler.z + bankSpeed * Time.deltaTime);
			transform.eulerAngles = euler;
			yield return null;
		}
			
		transform.eulerAngles = euler;
		rightCoroutineStarted = false;

		animator.SetInteger("DragonState", flyingAnimation);
		flapping.mute = false;
	}

	//// UPPPPPP
	public void MoveUpEuler(){

		// if roll in progress block other movements
		if (rollCoroutineStarted == false) {
			Debug.Log ("UP");
			animator.SetInteger ("DragonState", upAnimation);
			StartCoroutine ("UpEuler");
		} else {
			Debug.Log ("ROLL IN PROGRESS - NO UP");
		}
	}
	
	private IEnumerator UpEuler(){
		flapping.mute = true;
		// if the dragon is leveling out and asked to rise again, cancel the leveling out
		if (upCoroutineStarted == true) {
			Debug.Log ("CANCEL UP COROUTINE");
			upCoroutineStarted = false;
			StopCoroutine("EvenUpEulerCoroutine");
		}


		float currentTime = Time.time;
		
		while (Time.time <= currentTime + 1f) {
			if(euler.x > -70){
				euler.x = (euler.x-upSpeed*Time.deltaTime);
				transform.eulerAngles = euler;
			}
			yield return null;
		}

	}

	public void EvenUpEuler(){
		
		// if roll in progress block other movements
		if (rollCoroutineStarted == false){// && downCoroutineStarted == false) {
			Debug.Log ("EVEN UP");

			StartCoroutine ("EvenUpEulerCoroutine");
		} else {
			Debug.Log ("NO EVEN UP");
		}
	}

	
	private IEnumerator EvenUpEulerCoroutine(){
		Debug.Log ("COROUTINE - EVEN UP");
		animator.SetInteger ("DragonState", evenUpAnimation);
		//change bool so we know if to cancel it
		upCoroutineStarted = true;
		yield return new WaitForSeconds (0.2f);
		while (euler.x < 0) {
			
			euler.x = (euler.x + evenSpeed*Time.deltaTime);
			transform.eulerAngles = euler;
			yield return null;
		}

		transform.eulerAngles = euler;
		//set bool to false once over
		upCoroutineStarted = false;

		animator.SetInteger ("DragonState", flyingAnimation);
		flapping.mute = false;
	}

	///// DOWNNNN
	public void MoveDownEuler(){

		// if roll in progress block other movements
		if (rollCoroutineStarted == false) {
			Debug.Log ("DOWN");

			animator.SetInteger ("DragonState", downAnimation);

			StartCoroutine ("DownEuler");
		} else {
			Debug.Log ("ROLL IN PROGRESS - NO DOWN");
		}
	}
	
	private IEnumerator DownEuler(){
		flapping.mute = true;
		if (downCoroutineStarted == true) {
			Debug.Log ("DOWN COROUTINE CANCELLED");
			downCoroutineStarted = false;
			StopCoroutine("EvenDownEulerCoroutine");
		}


		float currentTime = Time.time;
		
		while (Time.time <= currentTime + 1f) {
			if(euler.x < 70){
				if(moveSpeed < maxDiveSpeed){
					moveSpeed += 0.08f;
				}

				euler.x = (euler.x+turnSpeed*Time.deltaTime);
				transform.eulerAngles = euler;
			}
			yield return null;
		}

	}

	public void EvenDownEuler(){

		// if roll in progress block other movements
		if (rollCoroutineStarted == false){
			Debug.Log ("EVEN DOWN");

			StartCoroutine ("EvenDownEulerCoroutine");
		} else {
			Debug.Log ("NO EVEN DOWN");
		}

	}

	private IEnumerator EvenDownEulerCoroutine(){
		Debug.Log ("COROUTINE - EVEN DOWN");
		animator.SetInteger ("DragonState", evenDownAnimation);
		downCoroutineStarted = true;
		yield return new WaitForSeconds (0.2f);
		while (euler.x > 0) {
			if(moveSpeed>originalMoveSpeed){
				moveSpeed -= 0.1f;
			}

			euler.x = (euler.x - evenSpeed*Time.deltaTime);
			transform.eulerAngles = euler;
			yield return null;
		}


		transform.eulerAngles = euler;
		moveSpeed = originalMoveSpeed;

		downCoroutineStarted = false;

		animator.SetInteger ("DragonState", flyingAnimation);
		flapping.mute = false;
	}

	public void RollEuler(){

		//only let second barrel roll start once one roll is over

		if (rollCoroutineStarted == false) {
			Debug.Log ("ROLL");
			StartCoroutine ("RollEulerCoroutine");
		} else {
			Debug.Log ("NO ROLL");
		}
	}

	public void ResetGame(){
		Application.LoadLevel (0);
	}
		
	private IEnumerator RollEulerCoroutine(){
		Debug.Log ("COROUTINE - ROLL");
		flapping.mute = true;

		// can check for and cancel even up or even down coroutine if you want here

		rollCoroutineStarted = true;

		// if turning left or moving straight do barrel roll to the left
		if (euler.z > 0 || euler.z == 0) {
			animator.SetInteger ("DragonState", rollLeftAnimation);
			while (euler.z <= 360f) {
				euler.z = (euler.z + rollSpeed * Time.deltaTime);

				transform.eulerAngles = euler;
				yield return null;
			}
		} else if (euler.z < 0) {  // if turning right z will be less than 0 and do barrel roll to theright
			animator.SetInteger ("DragonState", rollRightAnimation);
			while (euler.z >= -360f) {
				euler.z = (euler.z - rollSpeed * Time.deltaTime);

				transform.eulerAngles = euler;
				yield return null;
			}
		}
		euler.z = 0;
		transform.eulerAngles = euler;

		// if dragon was rising or diving while rolling, when roll stops reorient to x = 0
		if (euler.x > 0) {
			Debug.Log ("EVEN DOWN MAN");
			StartCoroutine ("EvenDownEulerCoroutine");

		} else if (euler.x < 0) {
			Debug.Log ("EVEN UP MAN");
			StartCoroutine ("EvenUpEulerCoroutine");
		} else {
			animator.SetInteger ("DragonState", flyingAnimation);
		}

		rollCoroutineStarted = false;
		flapping.mute = false;
	}

	public void MoveDragon(){
		isFlying = !isFlying;
	}
	
	//// END OF USING EULER ANGLES ////

	public void XXXUpdate(){
		// start/stop flying on button click

		// done this so you can project another comp on a big screen
		//if (Input.GetMouseButtonDown (0)) {
			//isFlying = !isFlying;
		//	socket.Emit ("fly");
		//}

		if (Input.GetButton ("FlyButton")) {
			isFlying = true;
		} else if (Input.GetButton ("NoFlyButton")) {
			isFlying = false;
		}
			
		float flyX = Input.GetAxis ("FlyAxisX");
		float flyY = Input.GetAxis ("FlyAxisY");

		float leftRein = 100.0F - (Input.GetAxis ("LeftJoystickVertical") * 100.0F);
		leftRein = Mathf.Round(Mathf.Clamp(leftRein, 0.0F, 100.0F));

		float rightRein = 100.0F - (Input.GetAxis ("RightJoystickVertical") * 100.0F);
		rightRein = Mathf.Round(Mathf.Clamp(rightRein, 0.0F, 100.0F));
		Debug.Log (leftRein + "," + rightRein);			

		if (flyX > 0) {
			MoveRightEuler ();
			lastFlyX = 1.0f;
		} else if (flyX < 0) {
			MoveLeftEuler ();
			lastFlyX = -1.0f;
		} else {
			if (lastFlyX > 0) {
				EvenRightEuler ();
			} else if (lastFlyX < 0) {
				EvenLeftEuler ();
			}
			lastFlyX = 0.0f;
		}	

		if (flyY > 0) {
			MoveUpEuler ();
			lastFlyY = 1.0f;
		} else if (flyY < 0) {
			MoveDownEuler ();
			lastFlyY = -1.0f;
		} else {
			if (lastFlyY > 0) {
				EvenUpEuler ();
			} else if (lastFlyY < 0) {
				EvenDownEuler ();
			}
			lastFlyY = 0.0f;
		}	
			
		if (isFlying) {
			transform.Translate (Vector3.forward * moveSpeed * Time.deltaTime);
		}
			

	}
	
	public void Update(){
		float leftRein = 0.0F;
		float rightRein = 0.0F;

		if (useJoystickReins) {
			leftRein = 1.0F - Input.GetAxis ("LeftJoystickVertical");
			leftRein = Mathf.Clamp (leftRein, 0.0F, 1.0F);

			rightRein = 1.0F - Input.GetAxis ("RightJoystickVertical");
			rightRein = Mathf.Clamp (rightRein, 0.0F, 1.0F);
			Debug.Log (leftRein + "," + rightRein);			
		} 
		else {
			// use the udp data
		
		}

		float forwardPercentage = 0.5F * (leftRein + rightRein);
		float turnPercentage = (leftRein - rightRein);

		turnVector.x = -udpPitch;
		turnVector.y = (turnVector.y + turnPercentage * maxTurnSpeed * Time.deltaTime);
		turnVector.z = 0.0F;
		transform.eulerAngles = turnVector;

		transform.Translate (Vector3.forward * forwardPercentage * maxForwardSpeed * Time.deltaTime);


	}

	void OnApplicationQuit() {
		if (udpReceiveThread.IsAlive) {
			udpReceiveThread.Abort();
		}
		udp.Close ();
	}


	private void UdpReceive() {
		udp = new UdpClient(udpPort);
		while (true) {
			try {
				IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
				byte [] data = udp.Receive(ref anyIP);
				// decode upd data here

				udpLeftRein = (float)data[3] / 100.0F; 
				udpRightRein = (float)data[4] / 100.0F; 

				//Int16 tempPitch = ((Int16)(data[7]) << 8) | ((Int16)(data[8]) & (Int16)(0x00ff));

				// Debug.Log(data[7].ToString() + " " + data[8].ToString());

				Int16 tempPitch = (Int16)data[7];
				//Debug.Log(tempPitch);
				tempPitch = (Int16)(tempPitch << 8);
				//Debug.Log(tempPitch);
				tempPitch = (Int16)(tempPitch | data[8]);
				//Debug.Log(tempPitch);
				//Debug.Log("===");

				if (tempPitch != -255) {
					udpPitch = (float)tempPitch;
				}
				else {
					udpPitch = 0.0F;
				}

				Debug.Log(udpPitch);
			
			}
			catch (Exception err) {
				Debug.Log (err.ToString ());
			}
		}
	}
}
