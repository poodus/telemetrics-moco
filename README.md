Motion Control interface for Telemetrics PT-LP pan/tilt head
==============

A modern interface for old hardware.


This program is designed to repurpose a robust servo-driven camera head which no longer had a controller unit.
Originally the head was designed to move large TV cameras around in a quiet studio environment.
Now it can run basic A-B moco for video and timelapse photography.


Using the serial protocol provided in the user manual, I was to make this interface for 
entering a device address, connecting to a head, performing basic velocity calibration, 
driving the head around with a joystick-like interface through GUI sliders or keyboard
shortcuts, programing basic A-B movements, and stopping the head.


I found one of these camera heads at a surplus store for cheap, and wanted to see if it was
still operational despite not having a controller. Often it's hard to find documentation on old 
equipment like this, so I was surprised to find that the manual had both the pinout for the 
XLR power cable so I could make a new one, and for the serial protocol. I used an FTDI USB->Serial 
cable so I could drive the head with a modern laptop.


##Keyboard shortcuts:
-**Left/Right arrows** - toggle pan speed (MAX LEFT, OFF, MAX RIGHT)
-**Up/Down arrows** - toggle tilt speed (MAX DOWN, OFF, MAX UP)
-**S** - stop movement

##Future development / current issues
- Deal with rollover if encoders are misaligned (after value 4080, they roll back to 0)
- Interpolated velocity for ramping up/down at the end of a move
- Positional accuracy improvements. Moves don't always land at the exact position requested. 
This is because of hardware limitations of the head as-is, and the velocity-estimation strategy
I used (a basic mapping). The current approach estimates the velocity needed, then runs the
head for the requested duration.
- Provide calibration utility to get position in more useful units like degrees
- Ability to set custom home position
- Software position limits


Made with Unity by Shane Reetz, 2017
shanereetz.com
