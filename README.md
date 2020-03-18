# BackFlip PFD
Better primary flight display - intended for use with [RTIMULib2-Arduino](https://github.com/avpripri/RTIMULib2)
 ADHRS + Pressure Sensors

![backflip](https://github.com/avpripri/avpripri/blob/master/resources/betterflight2.jpg)

# Background
This is a DirectX Windows application designed to be the best "split-window" companion with an electronic flight bag or similar flight/navigation computer.   It's designed to be a better primary flight display system period.

# Visualization

Classif EFIS designs are very "busy".  There's a lot of data in too small a screen.  The general "situational awareness" that comes with an EFIS is spectacular, but EFIS' come with a bad side-effect.  Even if you fly with them often, it's difficult to "at-a-glance" on a primary flight display _KNOW_ you're flying fine.  I took that and tried to make a better display.

# Brutal Simplicity

You don't actualy... NEED all that information all the time.  Airspeed, altitude, heading all great to know... but they don't mean your "ok" they are numbers, ultimately just data points, a well qualified, rested and  aware pilot has to decode into the current situation.  The true "information" the pilot _MUST_ have should blend this into the situation and give you one-glance display that means... we're good.  What is that information...

* Angle of Attack
* Attitude
* Slip / Skip
* Course

## Angle of Attack / Attitude / Slip-Skid

I combined these three into one simple visualization, the "Attitude of Attack Chevron"  and it is the most important instrument in the plane.

The center triangle represents the nose of the aircraft.  The chevrons go from highest AoA above on top as "Red", to zero AoA "Blue" on the bottom.  For any given phase of flight the "target" AoA is calculated for you.  For powered takeoff target AoA would correlate with Vx-speed bellow 400' and Vy above until engine temperature dictates higher speeds.  In unaccellerated level flight on a glider would correlate to the McCready speed-to-fly.  For a cruising powered aircraft, it correlates to Best Range Speed in cruise.  For landing this AoA target transitions to Maximum safe manuevering angle-of-attack (based on minimum safe speed) as you approach the runway environment.

## Course

Hoops really are the most obvious course depiction.  They don't require any translation, you mind knows exactly what to do at a glance.  I will admit I turned them off on my G3X because I just got bored of seeing them.  So for extended cruise the distance would spread out as far a one mile which reduces tedium.

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

G - Vertical Accelleration

A - Indicated pitot pressure ADC output (Kalman filter on raw output)

B - Barometeric pressure, in millibars

T - Board Temperature


All numbers except 'F' are formatted as floating point
