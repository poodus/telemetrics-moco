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
 * established in the user manual. Allows user to enter a device address,
 * connect to the device, drive it around, and stop it.
 * 
 */

public class ServoControl : MonoBehaviour
{
	// GUI
	public Slider tiltSlider;
	public Slider panSlider;
	public Text panPositionText;
	public Text tiltPositionText;
	public InputField deviceAddressInput;

	// Slider values
	int lastTiltValue = 0;
	int lastPanValue = 0;

	// Position values received from device
	int lastReceivedPanPosition = 0;
	int lastReceivedTiltPosition = 0;

	// Time
	float lastTimePositionUpdated = 0;
	float delayInSecondsForPositionUpdate = .050f;

	SerialPort sp;
	Boolean portOpened = false;
	string deviceAddress = "";

	// Main loop
	public void Update ()
	{
		if (portOpened) {
			// TODO periodically check if port is opened

			// TILT see if slider value has changed
			if ((int)tiltSlider.value != lastTiltValue) {
				sp.Write ("T " + (int)tiltSlider.value + "\r");
				lastTiltValue = (int)tiltSlider.value;
			}

			// PAN see if slider value has changed
			if ((int)panSlider.value != lastPanValue) {
				sp.Write ("P " + (int)panSlider.value + "\r");
				lastPanValue = (int)panSlider.value;
			}

			// Get position from device
			if ((Time.fixedTime - lastTimePositionUpdated) > delayInSecondsForPositionUpdate) {
				GetHeadPosition ();
				lastTimePositionUpdated = Time.fixedTime;
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
	public void Connect() {
		// If no address was entered
		if (deviceAddressInput.text == "") {
			UnityEditor.EditorUtility.DisplayDialog ("Device address needed", "Please enter a device address and reconnect.", "Ok");
		} 
		else {
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

	public void StopMovement ()
	{
		UnityEngine.Debug.Log ("Stop Movement");
		// Clear output buffer so command doesn't get delayed
		sp.DiscardOutBuffer ();
		// Write stop command
		sp.WriteLine ("R\r");
		// Put sliders at the 0 velocity position
		panSlider.value = 16383;
		tiltSlider.value = 16383;
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

	void GetHeadPosition ()
	{
		// Clear buffers
		sp.DiscardOutBuffer ();
		sp.DiscardInBuffer ();

		// Command string "pt\r" will return pan and tilt's positions in one packet
		sp.WriteLine ("pt\r");
		string valRead = sp.ReadTo ("\r");

		if (!valRead.Equals (" ") && !valRead.Equals("")) {
			String[] vals = valRead.Split (' ');
			if (vals.Length >= 2) {
				// Update interally saved positions
				lastReceivedPanPosition = Convert.ToInt32 (vals [0]);
				lastReceivedTiltPosition = Convert.ToInt32 (vals [1]);
				// Update GUI
				tiltPositionText.text = "" + lastReceivedTiltPosition;
				panPositionText.text = "" + lastReceivedPanPosition;
			}
		}

	}
}
