# BackFlip PFD
Better primary flight display - intended for use with [RTIMULib2-Arduino](https://github.com/avpripri/RTIMULib2)
 ADHRS + Pressure Sensors

![backflip](https://github.com/avpripri/avpripri/blob/master/backflip.jpg)

A ton of capability in a small package;
- Angle of Attack
- Airspeed
- Altitude
- Heading
- Attitude
- OGN Traffic (FLARM-like)
- 3D GPS Position

# Background
This is a DirectX Windows application designed to be the best "split-window" companion with an electronic flight bag or similar flight/navigation computer.   It's designed to be a better primary flight display system period.

# Hardware

Many avionics companies offer flight sensors, they have a few names... air-data, ADAHRS, IMU.  What makes this project different is that it's DIY, and cost.. $60 vs. $2000.  This is because we're leveraging hardware developed to a price-point to fly drones.  So one would ask, what about quality?  Is it any good... in short, yes, it's very good.  It definately does the job.

For manned visual reference flight, the FAA requires pressure altitude, airspeed and heading.  The individual sensors required for this are;

- Altimeter - barometeric pressure
- Airspeed - differential pressure
- Compass - Magnetometer

All but one of the above are commonly available on a single, compact and solid-state PCB board. Differential pressure, required for indiciated airspeed, must be added by solder-on a MPXV7002DP module.  See build instructions.
 
Additionally to the required information above, these flight controller boards also have 3 axis accellerometer and 3 axis solid state gyros which, when signal processed yield;

- Pitch
- Roll
- Stabalized heading (merged with compass above)
- Yaw
- Accelleration

And... to top it all off.  There's a relatively powerful 32-bit STM32 F3 microprocessor also right there on that same tiny board which is more than powerful enough to poll the sensors and signal process to derive the pilot-ready outputs.

For this project, I chose to use an older generation of flight controller, the Naze32 "FULL" version or 10 dof. It is important that you get the "FULL" version or 10-dof.  Most flight controllers are just gyros, that won't work for this purpose.  For the Naze32 FULL, here are the list of sensors;

- MPU6050 (Gryo/ACC)
- HMC5883L (Mag)
- MS5611 (Baro)

The [RTIMULib2-Arduino](https://github.com/avpripri/RTIMULib2) project is already configured to build, initialize, calibrate and generate data for these sensors.  Other boards can be used with other sensors, you'll need to configure and build your own firmway (see the project).

# BOM/Build
  
  ## Source the following on amazon/ebay/alibaba (copy/paste search)
  1. Naze32 / Flip32 F3 FULL 10 DOF (make sure the details include all the sensors above)
  1. MPXV7002 module
  1. Any windows tablet/laptop/surface
  
  ## Build the software
  
  ### Air-Data 
  
  1. In Visual Studio Code, install the "Platform IO" extension.  This is a popular extension for developing small embedded devices like BlackFlip.
  1. Clone [RTIMULib2-Arduino](https://github.com/avpripri/RTIMULib2) 
  1. Open RTIMULib2 above
  1. Connect the Naze32 board via USB to your computer
  1. Build and Flash
 
  ### Display
  1. Clone [Backflip](https://github.com/avpripri/BackFlip)
  2. Open Backflip.sln in Visual Studio (full version, community works)
  3. Build and run.
  
  ## Airspeed
  
  1. Solder the 3.3v power and ground from Naze32 to the Vin on the MPXV7002 module.  There's a 3.3v source on the programming pins of the board 
  1. Solder the signal line from teh MPXV7002 to servo ch2 on the Naze32 (this has been configured in the firmware into an analog to digital input chanel).

  ## Testing
  
  1. Open the serial window in Visual Studio Code
  1. Select the correct COMM port
  1. Set the baud rate to 9600
  
  You should see an output like...
  ```F:5,R:-30.11,P:-2.25,H:100.00,G:0.81,Y:-0.47,A:2173.24,B:1021.16,T:19.72
     F:5,R:-30.11,P:-2.26,H:100.00,G:0.81,Y:-0.47,A:2173.10,B:1021.17,T:19.72
     F:5,R:-30.11,P:-2.26,H:100.00,G:0.81,Y:-0.47,A:2173.29,B:1021.18,T:19.72
  ```

  Success, you have an IMU!!!
  
  
  ## Installation
  
  1. You'll want to mount the Naze32 and the MPXV7002 into a small project box to protect it.
  1. Mount this module onto your aircraft in the correct orientation.  RTIMULIB does support alterante orientation, but you'll need to set a flag and re-flash the firmware. 
  1. Connect the air data module to the tablet via a USB cable, I use a USB hub with external power so everything is getting agood power source and I have plenty of available usb ports.
  
# Visualization

Classic EFIS designs are very "busy".  There's a lot of data in too small a screen.  The general "situational awareness" that comes with an EFIS is spectacular, but EFIS' come with a bad side-effect.  Even if you fly with them often, it's difficult to "at-a-glance" on a primary flight display _KNOW_ you're flying fine.  I took that and tried to make a better display.

# Brutal Simplicity

You don't actually... NEED all that information all the time.  Airspeed, altitude, heading all great to know... but they don't mean your flying safe, they are just numbers, ultimately just data points.  A well qualified, rested and aware pilot is expert at decode these values and integrating that into "flying the air plane".  But I propose an improved visualization that extracts that data and only present what the pilot needs at a glance.  The following are critical to "flying the air plane".

* Angle of Attack
* Attitude
* Slip / Skip

# Tactical, not strategic

This display is 100% tactical... what do I need to _DO_ to fly the air plane right in this moment.  Strategic flying folds in more information for navigation, traffic and weather.  I argue that should go on another visualization dedicated to that "bigger picture" purpose.

## Angle of Attack / Attitude / Slip-Skid

I combined these three into one simple visualization, the "Attitude of Attack Chevron"  and it is the most important instrument in the plane.

The center triangle represents the nose of the aircraft.  The chevrons go from highest AoA above on top as "Red", to zero AoA "Blue" on the bottom.  For any given phase of flight the "target" AoA is calculated for you.  For powered take off target AoA would correlate with Vx-speed bellow 400' and Vy above until engine temperature dictates higher speeds.  In unaccelerated level flight on a glider would correlate to the McCready speed-to-fly.  For a cruising powered aircraft, it correlates to Best Range Speed in cruise.  For landing this AoA target transitions to Maximum safe manoeuvring angle-of-attack (based on minimum safe speed) as you approach the runway environment.

# Backflip IMU

The Backflip PFD takes attitude and pressure data from the Backflip IMU. 

Uses the RTIMULib2-Arduino project, which outputs in this format;

```F:5,R:-30.11,P:-2.25,H:100.00,G:0.81,Y:-0.47,A:2173.24,B:1021.16,T:19.72
F:5,R:-30.11,P:-2.26,H:100.00,G:0.81,Y:-0.47,A:2173.10,B:1021.17,T:19.72
F:5,R:-30.11,P:-2.26,H:100.00,G:0.81,Y:-0.47,A:2173.29,B:1021.18,T:19.72
F:5,R:-30.13,P:-2.24,H:100.00,G:0.81,Y:-0.47,A:2173.35,B:1021.19,T:19.72
F:5,R:-30.12,P:-2.24,H:100.00,G:0.82,Y:-0.47,A:2173.53,B:1021.21,T:19.73
F:5,R:-30.12,P:-2.24,H:100.00,G:0.82,Y:-0.47,A:2173.58,B:1021.24,T:19.74
F:5,R:-30.12,P:-2.24,H:100.00,G:0.82,Y:-0.47,A:2173.50,B:1021.23,T:19.73
F:5,R:-30.12,P:-2.24,H:100.00,G:0.81,Y:-0.47,A:2173.64,B:1021.22,T:19.73
```

F - Flags => 1 - Gyro Bias Valid | 2 Accel Calibration Valid | 4 Compass Calibration Valid

R - Roll in radians

P - Pitch in radians

H - Compass Heading in degrees 0-North through 359

G - Vertical Acceleration

A - Indicated pitot pressure ADC output (Kalman filter on raw output)

B - Barometric pressure, in millibars

T - Board Temperature


All numbers except 'F' are formatted as floating point
