#include <Arduino.h>
#include "Adafruit_BNO055.h"

#pragma once


/* Set the delay between fresh samples */
#define BNO055_SAMPLERATE_DELAY_MS (100)

// Check I2C device address and correct line below (by default address is 0x29 or 0x28)
//                                   id, address
Adafruit_BNO055 bno = Adafruit_BNO055(55, 0x28);

class AHRS
{
  public:

    bool Setup()
    {
        /* Initialise the sensor */
        if(!bno.begin())
        {
            /* There was a problem detecting the BNO055 ... check your connections */
            Serial.print("Ooops, no BNO055 detected ... Check your wiring or I2C ADDR!");
            return false;
        }
        
        delay(200);

        /* Use external crystal for better accuracy */
        bno.setExtCrystalUse(true);
        return true;
    }

    bool Update(sensors_event_t *pEvent)
    {
        return bno.getEvent(pEvent);
    }
};