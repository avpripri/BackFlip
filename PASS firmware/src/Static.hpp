#include <Arduino.h>
#include "SPL06.h"


#pragma once


class Static
{
  public:

    void Setup()
    {
        InitPressureSensor();
    }

    int PressurePa()
    {
        return GetPressurePa();
    }
};