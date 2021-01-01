#include <Arduino.h>
#include "Honeywell_ABP.h"


#pragma once


class Pitot
{
    // // create Honeywell_ABP instance
    // // refer to datasheet for parameters
    // Honeywell_ABP abp(
    //     0x28,   // I2C address
    //     0,      // minimum pressure
    //     1,      // maximum pressure
    //     "pa");   // pressure unit

  public:

    void Setup()
    {
        // open I2C communication
        Wire.begin();
    }
    
    int PressurePa()
    {
        // abp.update();

        // return (int)abp.pressure();      
        return 0;
    }
};