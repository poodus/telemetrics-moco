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
 * established in the user manual.
 * 
 */

public class ServoControl : MonoBehaviour
{

	// GUI
	public Slider tiltSlider;
	public Slider panSlider;
	public Text panPositionText;
	public Text tiltPositionText;

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
	string deviceName = "/dev/tty.usbserial-FT0EGQ74";
	// TODO make this an option the user can change

	// Initizlie serial port and enable camera
	void Start ()
	{
		sp = new SerialPort (deviceName, 9600, Parity.None, 8, StopBits.One);
		sp.Open ();
		// TODO handle serial port exceptions
		sp.ReadTimeout = 500;
		sp.NewLine = "\r";
		StopMovement ();
		EnableCamera ();
	}

	// Main loop
	public void Update ()
	{
						
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

	public void StopMovement ()
	{
		UnityEngine.Debug.Log ("Stop Movement");
		// Clear output buffer so command doesn't get delayed
		sp.DiscardOutBuffer ();
		// Write stop command
		sp.WriteLine ("R\r");
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
		// "pt\r" will return pan and tilt's positions in one packet
		sp.WriteLine ("pt\r");
		string valRead = sp.ReadTo ("\r");
		if (!valRead.Equals (" ") && !valRead.Equals("")) {
			String[] vals = valRead.Split (' ');
			if (vals.Length >= 2) {
				lastReceivedPanPosition = Convert.ToInt32 (vals [0]);
				lastReceivedTiltPosition = Convert.ToInt32 (vals [1]);
				tiltPositionText.text = "" + lastReceivedTiltPosition;
				panPositionText.text = "" + lastReceivedPanPosition;
			}
		}

	}


}
