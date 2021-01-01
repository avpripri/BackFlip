#include <Arduino.h>
#include "ChanelConfig.hpp"

#pragma once


// Get/Set GPIO values
class GPIOManager
{
    struct ChanelValue 
    {
        String name;
        uint16_t value;     
    };

    ChanelConfig Chanels[NUM_DIGITAL_PINS];
    ChanelValue Values[NUM_DIGITAL_PINS];

 public:
    void Setup()
    {
        ReadConfigurations();
        for(int i = 0; i < NUM_DIGITAL_PINS; i++)
            Values[i].name = Chanels[i].Name;
    }    

    void ReadConfigurations()
    {
        // read from flash
    }

    void WriteConfigurations()
    {
        // write to flash
    }

    ChanelConfig *GetChanelConfig(uint8_t chanelId)
    {
        return (chanelId < NUM_DIGITAL_PINS) ? (Chanels + chanelId) : NULL;
    }


    void SetChanel(uint8_t chanelId, uint16_t value)
    {
        if (chanelId < NUM_DIGITAL_PINS)
            Chanels[chanelId].Write(Values[chanelId].value = value);
    }

    // Read all values from input pins
    // Output pins are written once on the request
    ChanelValue *Update()
    {
        for(int i = 0; i < NUM_DIGITAL_PINS; i++)
        {
            if (Chanels[i].Mode != OUTPUT)
            {
                Values[i].value = Chanels[i].Read();
            }
            else
            {
                // OUTPUT... Ideally, we should read the port and set the value as assigned
                // You can ports and ensure the set value is still set, but it should be
            }
            
        }
        return Values;
    }
};