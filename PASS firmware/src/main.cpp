#include <Arduino.h>
#include "GPIOManager.hpp"
#include "Pitot.hpp"
#include "Static.hpp"
#include "AHRS.hpp"
#include "CANBus.hpp"
#include "HandleCommands.hpp"

Pitot  pitot;
Static stat;
AHRS   ahrs;
CANBus    canBus;
GPIOManager gpio;

//#define LED_BUILTIN PC13
void setup() {
  // Serial Setup
  Serial.begin(115200);

  Serial.println("PASS Initializing");

  // Setup GPIO

  pitot.Setup();
  stat.Setup();
  ahrs.Setup();
  canBus.Setup();
}

void loop() {
  // Read command (if any)
  int cmd = Serial.read();
  if (cmd >= 0 && cmd < PassCommmandMax)
  {
    CommandHandler[cmd]();
  }

  canBus.Update();

  // Filter CAN-bus message
      // Write CAN-bus message to serial

  // Read AHRS
  // Read Barometer
  stat.PressurePa();
  // Read Diff Pressure
  pitot.PressurePa();
  // Read Sensors


  // Write Status Message for AHRS, barometr, diff pressures & sensors
  // Serial.print("F:");
  // Serial.print(imu->IMUGyroBiasValid() | 2*imu->getAccelCalibrationValid() | 4*imu->getRuntimeCompassCalibrationValid());        
  // Dump("R", data.fusionPose.x());
  // Dump("P", data.fusionPose.y());
  // Dump("H", (int)(540.0 + data.fusionPose.z() * RTMATH_RAD_TO_DEGREE) % 360);
  // Dump("G", data.accel.z());
  // Dump("Y", data.accel.y());
  // Dump("A", pitotKalmanFilter.updateEstimate(analogRead(ADC_CHANNEL_0)));
  // Dump("B", pressureKalmanFilter.updateEstimate(data.pressure));
  // Dump("T", temperatureKalmanFilter.updateEstimate(data.temperature));
  // Dump("E", TEKalmanFilter.updateEstimate(imu->getTotalEnergy(data.accel)));
  Serial.println();
}