Motion Control interface for Telemetrics PT-LP pan tilt head
==============

A modern interface for an out-of-date camera head.

This program controls a Telemetrics PT-LP pan/tilt camera head, which was originally designed
to move large TV studio cameras without making appreciable noise.

Using the serial protocol available in the user manual. Users can enter a device address, connect, 
perform a basic calibration, drive the head around with a joystick-like interface, program basic A-B movements, and stop movement.

I found one of these camera heads at a surplus store for cheap, and wanted to see if it was still operational.
By using an FTDI USB->serial cable, I was able to connect and send serial commands to the head. I took
it a few steps further by making this application, which adds A-B movement ideal for timelapse photography.

Keyboard shortcuts:
Left/Right arrows - toggle pan speed (MAX LEFT, OFF, MAX RIGHT)
Up/Down arrows - toggle tilt speed (MAX DOWN, OFF, MAX UP)
S - stop movement
Made with Unity by Shane Reetz, 2017.
