using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports;
using UnityEngine.UI;
using System.Text;
using System;

/*
 * ServoControl
 * 
 * Controls a Telemetrics PT-LP pan/tilt camera head using the serial protocol
 * established in the user manual. This application lets a user to enter a device address,
 * connect to the device, drive it around, do basic A-B movement, and stop movement.
 * 
 */
using System.Text.RegularExpressions;

public class ServoControl : MonoBehaviour
{
	// GUI
	public Slider tiltSlider;
	public Slider panSlider;
	public Text panPositionText;
	public Text tiltPositionText;
	public Text panVelocityText;
	public Text tiltVelocityText;
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
	int startPanPosition;
	int startTiltPosition;
	int endPanPosition;
	int endTiltPosition;
	Boolean moveRunning = false;

	float maxPanVelocity = 1000f;
	// temp
	float maxTiltVelocity = 1000f;
	// temp

	float calibMoveDuration = 3.0f;
	bool calibrationInProgress = false;
	bool calibrationFirstLoop = true;

	// Time
	float lastTimePositionUpdated = 0;
	float delayInSecondsForPositionUpdate = 1f;

	SerialPort sp;
	Boolean portOpened = false;
	string deviceAddress = "";

	// TODO remove this. for dev purposes only.
	public void Start ()
	{
		deviceAddressInput.text = "/dev/tty.usbserial-FT0EGQ74";
		Connect ();
	}

	public float mapControlVoltageToPercent(float input) {
		float minVel = 0;
		float maxVel = 32767;
		float minVelOut = -1000;
		float maxVelOut = 1000;
		return minVelOut + (maxVelOut - minVelOut) * ((input - minVel) / (maxVel - minVel));
	}
	float counter = 0f;
	int[] lastPositions;
	// Main loop
	public void Update ()
	{
		if (portOpened) {
			// TODO periodically check if packets are being sent/received
			if (!moveRunning) {

				// TILT see if slider value has changed
				if ((int)tiltSlider.value != lastTiltValue) {
					print ("tiltSlider.value: " + tiltSlider.value);
					sp.Write ("T " + (int)tiltSlider.value + "\r");
					lastTiltValue = (int)tiltSlider.value;
					tiltVelocityText.text = "" + (int)mapControlVoltageToPercent (tiltSlider.value);
				}

				// PAN see if slider value has changed
				if ((int)panSlider.value != lastPanValue) {
					print ("panSlider.value: " + panSlider.value);
					sp.Write ("P " + (int)panSlider.value + "\r");
					lastPanValue = (int)panSlider.value;
					panVelocityText.text = "" + (int)mapControlVoltageToPercent (panSlider.value);
				}

				// Get position from device
				// if more than delay seconds have passed, update position
				if ((Time.fixedTime - lastTimePositionUpdated) > delayInSecondsForPositionUpdate) {
					GetHeadPosition ();	
					lastTimePositionUpdated = Time.fixedTime;
				}
			}
			if (calibrationInProgress) {
				if (calibrationFirstLoop) {
					calibrationFirstLoop = false;
					// Get current position
					lastPositions = GetHeadPosition ();
					print ("last positions: " + lastPositions [0] + " " + lastPositions [1]);

					// Move at max speed for moveTime
					moveRunning = true;
					sp.Write ("P " + 0 + " T " + 0 + "\r");

					counter = 0f;
				}
				// TODO move to known position to avoid hitting limit switches

				if(counter < calibMoveDuration) {
					counter += Time.deltaTime;
					print ("Time left: " + (calibMoveDuration - counter));
				}
				else {
					StopMovement ();
					calibrationInProgress = false;
					calibrationFirstLoop = true;

					int[] newPositions = GetHeadPosition ();
					print ("new positions: " + newPositions [0] + " " + newPositions [1]);
					// calculate max velocities
					maxPanVelocity = (Math.Abs (lastPositions [0] - newPositions [0]) / calibMoveDuration); // returned as "position units / sec"
					maxTiltVelocity = (Math.Abs (lastPositions [1] - newPositions [1]) /  calibMoveDuration); // returned as "position units / sec"
					print ("Max pan velocity: " + maxPanVelocity + " Max tilt velocity: " + maxTiltVelocity);
					// note: Units/sec is being calculated with a basic averaging, which is assuming that
					// speed changes linearly. that may not be true.
				}
				
			}
		}
	}

	/*
	 * Connect()
	 * 
	 * Attempts to setup a serial connection with device, using the address
	 * entered by the user into the input field.
	 * 
	 */
	public void Connect ()
	{
		// If no address was entered
		if (deviceAddressInput.text == "") {
			UnityEditor.EditorUtility.DisplayDialog ("Device address needed", "Please enter a device address and reconnect.", "Ok");
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
			} catch (Exception) {
				UnityEditor.EditorUtility.DisplayDialog ("No connection", "Unable to connect. Connect cables, check address, and retry.", "Ok");
			}	
		}
	}

	public void Calibrate ()
	{
		print ("CALIBRATE");
		calibrationInProgress = true;
	}

	/*
	 * MoveToPosition()
	 * 
	 * Run a basic A-B movement, starting at the head's current position
	 * and working towards an end position over a given duration.
	 * 
	 */
		public void MoveToPosition(int panPosition, int tiltPosition, float moveDuration)
		{
			StopMovement ();
	
			// Get input from GUI
			float duration = float.Parse(durationInput.text);
			endPanPosition = Convert.ToInt32 (panPosition);
			endTiltPosition = Convert.ToInt32 (endTiltPositionInput);
	
			// Calculate velocity needed for move in terms of units/sec
			int panVelocity = (int)((endPanPosition - lastReceivedPanPosition) / duration); // Map from 0-32767
			int tiltVelocity = (int)((endTiltPosition - lastReceivedTiltPosition) / duration); // Map from 0-32767
	
			// Map velocity to the cooresponding serial parameter
			if (panVelocity < 0) {
			panVelocity = Math.Abs((int)(16383 - (panVelocity / maxPanVelocity) * 16383));
			} else {
			panVelocity = Math.Abs((int)(16383 + (panVelocity / maxPanVelocity) * 16383));
			}
			if (tiltVelocity < 0) {
				tiltVelocity = Math.Abs((int)(16383 - (tiltVelocity / maxTiltVelocity) * 16383));
			} else {
				tiltVelocity = Math.Abs((int)(16383 - (tiltVelocity / maxTiltVelocity) * 16383));
			}
	
			sp.Write ("P " + panVelocity + "T " + tiltVelocity + "\r");
			// Wait for move to complete
			float lastPositionPoll = 0;
			float pollingInterval = 1;
			moveRunning = true;
			while (duration > 0) {
				// Poll for position every X m
				duration -= Time.deltaTime; // in seconds
			}
			moveRunning = false;
		}

	public void StopMovement ()
	{
		UnityEngine.Debug.Log ("Stop Movement");
		// Clear output buffer so command doesn't get delayed
		sp.DiscardOutBuffer ();
		// Write stop command
		sp.WriteLine ("R\r");
		// Put sliders at the neutral velocity position
		panSlider.value = 16383;
		tiltSlider.value = 16383;
		moveRunning = false;
	}

	public void EnableCamera ()
	{
		// Enable Camera
		UnityEngine.Debug.Log ("Enable Camera");
		sp.Write ("L 1\r");
	}

	void OnApplicationQuit ()
	{
		UnityEngine.Debug.Log ("Quit");
		StopMovement ();
		sp.Close ();
	}

	string valRead = "";
	int[] falseReturn = { -1, -1 };

	int[] GetHeadPosition ()
	{
		valRead = "";

		// Clear buffers
		sp.DiscardOutBuffer ();
		sp.DiscardInBuffer ();

		// Command string "pt\r" will return pan and tilt's positions in one packet
		sp.WriteLine ("pt\r");
		float test = Time.fixedTime;
		valRead = sp.ReadTo ("\r");
		//print ("valRead: " + valRead);
		// if correct response was received, validate the packet:

		String[] vals = valRead.Split (' ');
		if (isValidPosition (vals)) {
			//print ("Vals: " + vals [0] + " " + vals [1]);
			// Update interally saved positions
			lastReceivedPanPosition = Convert.ToInt32 (vals [0]);
			lastReceivedTiltPosition = Convert.ToInt32 (vals [1]);
			// Update GUI
			tiltPositionText.text = "" + lastReceivedTiltPosition;
			panPositionText.text = "" + lastReceivedPanPosition;
			return new int[]{ lastReceivedPanPosition, lastReceivedTiltPosition };
		} else {
			return falseReturn;
		}
	}
		
	// Verify that position data received is valid (2 values, only numbers)
	bool isValidPosition (string[] vals)
	{
		if (vals.Length >= 2 &&
		    Regex.IsMatch (vals [0], @"^[0-9]+$") &&
		    Regex.IsMatch (vals [1], @"^[0-9]+$")) {
			return true;
		} else {
			print ("INVALID POSITION");
			return false;
		}
	}
}

