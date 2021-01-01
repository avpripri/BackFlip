#include <Arduino.h>
#include "ChanelConfig.hpp"
#include "CANBus.hpp"

#pragma once

extern GPIOManager gpio;

void HandleSetGPIOValue (){
    auto chanelId = Serial.read();
    auto value = Serial.read();

    gpio.SetChanel(chanelId, value);
}

void HandleSetGPIOConfig (){ 
    auto chanelId = Serial.read();
    Serial.readBytes((char *)gpio.GetChanelConfig(chanelId), sizeof(ChanelConfig));
}

void HandleSetCANBusFilter (){ }

void HandleSendCANMessage (){ }

void HandleGetFullStats (){ }

enum PassCommmand {SetGPIOConfig, SetGPIOValue, SetCANBusFilter, SendCANMessage, GetFullStats, PassCommmandMax };

void (*CommandHandler[])(void) =
{
  &HandleSetGPIOValue, 
  &HandleSetGPIOConfig, 
  &HandleSetCANBusFilter, 
  &HandleSendCANMessage, 
  &HandleGetFullStats
};
