using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports;
using UnityEngine.UI;
using System;
using System.Text;
using System.Text.RegularExpressions;

/*
 * ServoControl
 * 
 * Controls a Telemetrics PT-LP pan/tilt camera head using the serial protocol
 * established in the user manual. This application lets a user to enter a device address,
 * connect to the device, drive it around with a joystick-like interface, program basic A-B movements, 
 * and stop the head.
 * 
 */


public class ServoControl : MonoBehaviour
{
	public const int MAX_NEG_HEAD_VELOCITY = 0;
	// control voltage for max speed in one direction
	public const int NEU_HEAD_VELOCITY = 16383;
	// control voltage for no movement
	public const int MAX_POS_HEAD_VELOCITY = 32767;
	// control voltage for max speed in opposite direction

	// Artificial scale used to give the user more generic numbers to work with rahter than 0-32767.
	public const int MAX_NEG_VELOCITY_SCALE = -1000;
	public const int MAX_POS_VELOCITY_SCALE = 1000;

	// GUI elements
	public Slider tiltSlider;
	public Slider panSlider;
	public Text panPositionText;
	public Text tiltPositionText;
	public Text panVelocityText;
	public Text tiltVelocityText;
	public Text status;
	public InputField deviceAddressInput;
	public InputField endPanPositionInput;
	public InputField endTiltPositionInput;
	public InputField durationInput;

	// Slider values
	int lastTiltValue = 0;
	int lastPanValue = 0;

	// Position values received from device
	int lastReceivedPanPosition = 0;
	int lastReceivedTiltPosition = 0;

	// Target positions for move
	float startPanPosition;
	float startTiltPosition;
	float endPanPosition;
	float endTiltPosition;

	// Empiraclly determined values for average units/time for each axis.
	// Note: because of the hardware limitations of this system, exact velocities
	// can't be expected.
	float maxPanVelocity = 51.5f;
	float maxTiltVelocity = 94f;

	// Move status
	float moveDuration = 0f;
	bool moveRunning = false;
	bool moveRunningFirstLoop = true;
	float calibDuration = 5.0f;
	bool calibrating = false;
	bool calibrationFirstLoop = true;
	int calibrationsDone = 0;
	float lastTimePositionUpdated = 0f;
	float delayInSecondsForPositionUpdate = 1f;
	float counter = 0f;
	int[] lastPositions;

	// Serial port variables
	SerialPort sp;
	bool portOpened = false;
	string deviceAddress = "";
	string valRead = "";
	String[] splitVals;
	int[] falseReturn = { -1, -1 };

	// Main loop
	public void Update ()
	{
		if (portOpened) {

			// Get last position from head
			if ((Time.fixedTime - lastTimePositionUpdated) > delayInSecondsForPositionUpdate) {
				GetHeadPosition ();	
				lastTimePositionUpdated = Time.fixedTime;
			}
			
			/*
			 * Running a move
			 */
			if (moveRunning) {
				// Setup
				if (moveRunningFirstLoop) {
					counter = 0f;
					moveRunningFirstLoop = false;
				}
				if (counter < moveDuration) {
					counter += Time.deltaTime;
				} else {
					StopMovement ();
					moveRunningFirstLoop = true;
				}
			}

			/*
			 * Calibration
			 */
			else if (calibrating) {
				// Setup
				if (calibrationFirstLoop) {
					calibrationFirstLoop = false;
					// Get current position
					lastPositions = GetHeadPosition ();

					// Move at max speed for moveTime
					moveRunning = true;
					calibrationsDone += 1;
					// Alternate directions to (help) avoid hitting limit switches
					if (calibrationsDone % 2 == 0) {
						sp.Write ("P " + MAX_POS_HEAD_VELOCITY + " T " + MAX_POS_HEAD_VELOCITY + "\r");
					} else {
						sp.Write ("P " + MAX_NEG_HEAD_VELOCITY + " T " + MAX_NEG_HEAD_VELOCITY + "\r");
					}
					counter = 0f;

					// TODO move to known good position to avoid hitting limit switches
				}


				if (counter < calibDuration) {
					counter += Time.deltaTime;
				} else {
					StopMovement ();
					calibrating = false;
					calibrationFirstLoop = true;

					int[] newPositions = GetHeadPosition ();
					// calculate max velocities as position units (as defined by values given from head) / sec
					maxPanVelocity = (Math.Abs (lastPositions [0] - newPositions [0]) / calibDuration); 
					maxTiltVelocity = (Math.Abs (lastPositions [1] - newPositions [1]) / calibDuration);
					Debug.unityLogger.Log ("New max pan velocity: " + maxPanVelocity + " Max tilt velocity: " + maxTiltVelocity);
				}

			} 

			/*
			 * Joystick Mode
			 */
			else {
				/*
				 * Keyboard arrows work as input - up/down for tilt, left/right for pan
				 * if head is already moving in one direction, pressing the opposite direction
				 * key will cause the head to stop (hence the ?: conditional operators)
				 */
				if (Input.anyKeyDown) {
					if (Input.GetKey (KeyCode.UpArrow)) {
						tiltSlider.value = (tiltSlider.value < NEU_HEAD_VELOCITY) 
							? NEU_HEAD_VELOCITY : MAX_POS_HEAD_VELOCITY;
					}
					if (Input.GetKey (KeyCode.DownArrow)) {
						tiltSlider.value = (tiltSlider.value > NEU_HEAD_VELOCITY) 
							? NEU_HEAD_VELOCITY : MAX_NEG_HEAD_VELOCITY;
					}
					if (Input.GetKey (KeyCode.LeftArrow)) {
						panSlider.value = (panSlider.value < NEU_HEAD_VELOCITY)
							? NEU_HEAD_VELOCITY : MAX_POS_HEAD_VELOCITY;
					}
					if (Input.GetKey (KeyCode.RightArrow)) {
						panSlider.value = (panSlider.value > NEU_HEAD_VELOCITY)
							? NEU_HEAD_VELOCITY : MAX_NEG_HEAD_VELOCITY;
					}
				}

				// PAN update velocity if slider value has changed
				if ((int)panSlider.value != lastPanValue) {
					sp.Write ("P " + (int)panSlider.value + "\r");
					lastPanValue = (int)panSlider.value;
					panVelocityText.text = "" + (int)MapValues (panSlider.value, 
						MAX_NEG_HEAD_VELOCITY, MAX_POS_HEAD_VELOCITY, 
						MAX_NEG_VELOCITY_SCALE, MAX_POS_VELOCITY_SCALE);
				}

				// TILT update velocity if slider value has changed
				if ((int)tiltSlider.value != lastTiltValue) {
					sp.Write ("T " + (int)tiltSlider.value + "\r");
					lastTiltValue = (int)tiltSlider.value;
					tiltVelocityText.text = "" + (int)MapValues (tiltSlider.value, 
						MAX_NEG_HEAD_VELOCITY, MAX_POS_HEAD_VELOCITY, 
						MAX_NEG_VELOCITY_SCALE, MAX_POS_VELOCITY_SCALE);
				}
			}
		}
	}

	/*
	 * Connect()
	 * 
	 * Attempts to setup a serial connection with device using the address
	 * entered by the user into the input field.
	 * 
	 */
	public void Connect ()
	{
		UnityEngine.Debug.Log ("Connect");
		// If no address was entered
		if (deviceAddressInput.text == "") {
			UnityEditor.EditorUtility.DisplayDialog ("Device address needed", 
				"Please enter a device address and reconnect.", "Ok");
		} else {
			// Set device name to whatever the user entered
			// TODO add some validation for address addresses or give a list of connected devices
			deviceAddress = deviceAddressInput.text;
			// Parameters needed according to the user manual
			sp = new SerialPort (deviceAddress, 9600, Parity.None, 8, StopBits.One);
			try {
				sp.Open ();
				portOpened = true;
				Debug.Log ("Port Opened");
				sp.ReadTimeout = 500;
				sp.NewLine = "\r";
				StopMovement ();
				EnableCamera ();
				panVelocityText.text = "0";
				tiltVelocityText.text = "0";
			} catch (Exception) {
				UnityEditor.EditorUtility.DisplayDialog ("No connection", 
					"Unable to connect. Connect cables, check address, and retry.", "Ok");
			}	
		}
	}

	/*
	 * MoveToPosition()
	 * 
	 * Run a basic A-B movement, starting at the head's current position
	 * and working towards an end position over a given duration.
	 * 
	 */
	public void MoveToPosition ()
	{
		// Get input from GUI
		moveDuration = float.Parse (durationInput.text);
		endPanPosition = float.Parse (endPanPositionInput.text);
		endTiltPosition = float.Parse (endTiltPositionInput.text);

		UnityEngine.Debug.Log ("MoveToPosition positions - " +
			"pan: " + endPanPosition + " tilt: " + endTiltPosition);

		if (moveDuration > 0) {
			
			// Calculate velocity needed for move in terms of units/sec
			float panVelocity = (endPanPosition - lastReceivedPanPosition) / moveDuration;
			if (panVelocity < 0) {
				panVelocity = MapValues (Math.Abs (panVelocity), 0, maxPanVelocity, 
					MAX_NEG_HEAD_VELOCITY, NEU_HEAD_VELOCITY);
			} else if (panVelocity > 0) {
				panVelocity = MapValues (panVelocity, 0, maxPanVelocity, 
					NEU_HEAD_VELOCITY, MAX_POS_HEAD_VELOCITY);
			} else {
				panVelocity = NEU_HEAD_VELOCITY;
			}

			float tiltVelocity = (endTiltPosition - lastReceivedTiltPosition) / moveDuration;
			if (tiltVelocity < 0) {
				tiltVelocity = MapValues (Math.Abs (tiltVelocity), 
					0, maxTiltVelocity, 
					MAX_NEG_HEAD_VELOCITY, NEU_HEAD_VELOCITY);
			} else if (tiltVelocity > 0) {
				tiltVelocity = MapValues (tiltVelocity, 0, maxTiltVelocity, 
					NEU_HEAD_VELOCITY, MAX_POS_HEAD_VELOCITY);
			} else {
				tiltVelocity = NEU_HEAD_VELOCITY;
			}

			// TODO validate if velocities calulated are achievable before starting the move

			UnityEngine.Debug.Log ("MoveToPosition velocities - " + 
				"pan: " + (int)panVelocity + 
				" tilt: " + (int)tiltVelocity);
			EnableCamera ();
			sp.Write ("P " + (int)panVelocity + "T " + (int)tiltVelocity + "\r");
			moveRunning = true;

		} else {
			print ("Duration must be greater than 0");
		}
	}

	/*
	 * Calibrate()
	 * 
	 * Initiates a calibration control loop in Update()
	 */
	public void Calibrate ()
	{
		UnityEngine.Debug.Log ("Calibrate");
		calibrating = true;
	}

	/*
	 * StopMovement()
	 * 
	 * Sends a stop signal (R) to the head.
	 */
	public void StopMovement ()
	{
		UnityEngine.Debug.Log ("StopMovement");
		// Clear output buffer so command doesn't get delayed
		sp.DiscardOutBuffer ();
		// Write stop command
		sp.WriteLine ("R\r");
		// Put sliders at the neutral velocity position
		panSlider.value = 16383;
		tiltSlider.value = 16383;
		// Stop move loop in Update()
		moveRunning = false;
	}

	/*
	 * EnableCamera()
	 * 
	 * Takes control of the head with an L 1 command.
	 */
	public void EnableCamera ()
	{
		UnityEngine.Debug.Log ("EnableCamera");
		sp.Write ("L 1\r");
	}

	/*
	 * OnApplicationQuit()
	 * 
	 * Stops the head and closes the serial port.
	 */
	void OnApplicationQuit ()
	{
		UnityEngine.Debug.Log ("Quit");
		StopMovement ();
		sp.Close ();
	}

	/*
	 * GetHeadPosition()
	 * 
	 * Query the head for its current pan and tilt positions.
	 * 
	 */
	int[] GetHeadPosition ()
	{
		// Clear buffers
		sp.DiscardOutBuffer ();
		sp.DiscardInBuffer ();

		// This command will return pan and tilt's positions in one packet
		sp.WriteLine ("pt\r");
		valRead = sp.ReadTo ("\r");
		splitVals = valRead.Split (' ');

		if (IsValidPosition (splitVals)) {
			// Update global position variables
			lastReceivedPanPosition = Convert.ToInt32 (splitVals [0]);
			lastReceivedTiltPosition = Convert.ToInt32 (splitVals [1]);
			// Update GUI
			tiltPositionText.text = "" + lastReceivedTiltPosition;
			panPositionText.text = "" + lastReceivedPanPosition;
			return new int[]{ lastReceivedPanPosition, lastReceivedTiltPosition };
		} else {
			UnityEngine.Debug.LogWarning ("Invalid position read");
			return falseReturn;
		}
	}
		
	/*
	 * IsValidPosition()
	 * 
	 * Verify that position data received is of a valid format (2 values, only numbers).
	 * 
	 */ 
	bool IsValidPosition (string[] vals)
	{
		if (vals.Length >= 2 &&
			// Regex: composed of one or more digits
		    Regex.IsMatch (vals [0], @"^[0-9]+$") &&
		    Regex.IsMatch (vals [1], @"^[0-9]+$")) {
			return true;
		} else {
			return false;
		}
	}

	/*
	 * MapValues()
	 * 
	 * Map a value from one range to another.
	 * 
	 */ 
	public float MapValues (float input, float minInput, float maxInput, float minOutput, float maxOutput)
	{
		return minOutput + (maxOutput - minOutput) * ((input - minInput) / (maxInput - minInput));
	}
}
