#include <Arduino.h>
#pragma once

enum GPIOType {BINARY, ANALOG, TACHOMETER};

class ChanelConfig
{
  public:
    String Name;
    uint8_t PinAssignment;
    uint8_t Port:4;
    uint8_t Pin:4;
    uint8_t Mode:4;
    GPIOType IOType:4;        
    

    void Setup()
    {
        pinMode(PinAssignment, Mode);
        if (IOType == TACHOMETER)
        {
            // Setup a tach interupt
        }
    }

    uint16_t Read()
    {
        if (Mode == OUTPUT)
        {
            // GPIO_TypeDef *gpioDef = get_GPIO_Port(Port, Pin);

            // return (uint16_t)bitRead();
            return -32767; // how to read from the gpio pin data?
        }

        if (IOType == BINARY)
            return digitalRead(PinAssignment);
        if (IOType == ANALOG)
            return analogRead(PinAssignment);

        // return tach counts
    }

    void Write(int value)
    {
        if (Mode == OUTPUT)
            digitalWrite(PinAssignment, value);
    }
};