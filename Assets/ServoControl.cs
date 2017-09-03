using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports;
using UnityEngine.UI;
using System.Text;
using System;

public class ServoControl : MonoBehaviour
{

	public Slider tiltSlider;
	public Slider panSlider;
	public Text panPositionText;
	public Text tiltPositionText;
	int lastTiltValue = 0;
	int lastPanValue = 0;

	int lastReceivedPanPosition = 0;
	int lastReceivedTiltPosition = 0;

	float lastTimePositionUpdated = 0;
	float delayInSecondsForPositionUpdate = .050f;

	SerialPort sp;

	// Use this for initialization
	void Start ()
	{
		sp = new SerialPort ("/dev/tty.usbserial-FT0EGQ74", 9600, Parity.None, 8, StopBits.One);
		sp.Open ();
		sp.ReadTimeout = 500;
		sp.NewLine = "\r";
		StopMovement ();
		EnableCamera ();

		UnityEngine.Debug.Log ("port open? " + sp.IsOpen);
	}

	// Update is called once per frame
	public void Update ()
	{
						
		// TILT
		if ((int)tiltSlider.value != lastTiltValue) {
			sp.Write ("T " + (int)tiltSlider.value + "\r");
			lastTiltValue = (int)tiltSlider.value;
		}

		// PAN
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
		// Clear output buffer so command doesn't get delayed
		sp.DiscardOutBuffer ();
		// Write stop command
		sp.WriteLine ("R\r");
	}

	public void EnableCamera ()
	{
		sp.Write ("L 1\r"); // Enable Camera
	}

	void OnApplicationQuit ()
	{
		StopMovement ();
		UnityEngine.Debug.Log ("Quit");
		sp.Close ();
	}

	void GetHeadPosition ()
	{
		sp.DiscardOutBuffer ();
		sp.DiscardInBuffer ();
		// pt will return pan and tilt's positions in one packet
		sp.WriteLine ("pt\r");
		string valRead = sp.ReadTo ("\r");
		if (!valRead.Equals (" ") && !valRead.Equals("")) {
			// TODO update last time position was checked
			Debug.Log ("val read: " + valRead);
			String[] vals = valRead.Split (' ');
			Debug.Log ("vals: " + vals.ToString ());
			if (vals.Length >= 2) {
				lastReceivedPanPosition = Convert.ToInt32 (vals [0]);
				lastReceivedTiltPosition = Convert.ToInt32 (vals [1]);
				tiltPositionText.text = "" + lastReceivedTiltPosition;
				panPositionText.text = "" + lastReceivedPanPosition;
			}
		}

	}


}
