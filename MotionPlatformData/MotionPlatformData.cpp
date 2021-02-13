// Downloaded from https://developer.x-plane.com/code-sample/motionplatformdata/


/*
Plugin to show how to derive motion platform data from our datarefs
Thanks to Austin for allowing us to use the original Xplane conversion code.

Version 1.0.0.1			Intitial Sandy Barbour - 05/08/2007
*/
#include <iostream>
#include<ws2tcpip.h>
#include<winsock2.h>

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <cmath>

#include "XPLMDisplay.h"
#include "XPLMGraphics.h"
#include "XPLMProcessing.h"
#include "XPLMDataAccess.h"



#pragma comment(lib,"ws2_32.lib") //Winsock Library

// Globals.
// Use MPD_ as a prefix for the global variables

// Used to store data for display
char MPD_Buffer[6][80];
// Used to store calculated motion data
float MPD_MotionData[6];

// Window ID
XPLMWindowID MPD_Window = NULL;

// Datarefs
XPLMDataRef	MPD_DR_groundspeed = NULL;
XPLMDataRef	MPD_DR_fnrml_prop = NULL;
XPLMDataRef	MPD_DR_fside_prop = NULL;
XPLMDataRef	MPD_DR_faxil_prop = NULL;
XPLMDataRef	MPD_DR_fnrml_aero = NULL;
XPLMDataRef	MPD_DR_fside_aero = NULL;
XPLMDataRef	MPD_DR_faxil_aero = NULL;
XPLMDataRef	MPD_DR_fnrml_gear = NULL;
XPLMDataRef	MPD_DR_fside_gear = NULL;
XPLMDataRef	MPD_DR_faxil_gear = NULL;
XPLMDataRef	MPD_DR_m_total = NULL;
XPLMDataRef	MPD_DR_the = NULL;
XPLMDataRef	MPD_DR_psi = NULL;
XPLMDataRef	MPD_DR_phi = NULL;
XPLMDataRef MPD_DR_airspeed = NULL;
XPLMDataRef MPD_DR_pitch = NULL;
XPLMDataRef MPD_DR_baro_press = NULL;
XPLMDataRef MPD_DR_verticle_accel = NULL;



//---------------------------------------------------------------------------
// Function prototypes
SOCKET OpenAndConnect();
int SendSocketMessage(char* message);

float MotionPlatformDataLoopCB(float elapsedMe, float elapsedSim, int counter, void * refcon);

void MotionPlatformDataDrawWindowCallback(
                                   XPLMWindowID         inWindowID,    
                                   void *               inRefcon);    

void MotionPlatformDataHandleKeyCallback(
                                   XPLMWindowID         inWindowID,    
                                   char                 inKey,    
                                   XPLMKeyFlags         inFlags,    
                                   char                 inVirtualKey,    
                                   void *               inRefcon,    
                                   int                  losingFocus);    

int MotionPlatformDataHandleMouseClickCallback(
                                   XPLMWindowID         inWindowID,    
                                   int                  x,    
                                   int                  y,    
                                   XPLMMouseStatus      inMouse,    
                                   void *               inRefcon);    

float MPD_fallout(float data, float low, float high);
float MPD_fltlim(float data, float min, float max);
float MPD_fltmax2 (float x1,const float x2);
void MPD_CalculateMotionData(void);

//---------------------------------------------------------------------------
// SDK Mandatory Callbacks

PLUGIN_API int XPluginStart(
						char *		outName,
						char *		outSig,
						char *		outDesc)
{
	strcpy(outName, "MotionPlatformData");
	strcpy(outSig, "xplanesdk.examples.motiondplatformdata");
	strcpy(outDesc, "A plug-in that derives motion platform data from datarefs.");

	MPD_Window = XPLMCreateWindow(
		                      50, 600, 500, 500,								/* Area of the window. */
                              1,												/* Start visible. */
                              MotionPlatformDataDrawWindowCallback,			/* Callbacks */
                              MotionPlatformDataHandleKeyCallback,
                              MotionPlatformDataHandleMouseClickCallback,
                              NULL);											/* Refcon - not used. */

	XPLMRegisterFlightLoopCallback(MotionPlatformDataLoopCB, 1.0, NULL);
	
	MPD_DR_groundspeed = XPLMFindDataRef("sim/flightmodel/position/groundspeed");
	MPD_DR_fnrml_prop = XPLMFindDataRef("sim/flightmodel/forces/fnrml_prop");
	MPD_DR_fside_prop = XPLMFindDataRef("sim/flightmodel/forces/fside_prop");
	MPD_DR_faxil_prop = XPLMFindDataRef("sim/flightmodel/forces/faxil_prop");
	MPD_DR_fnrml_aero = XPLMFindDataRef("sim/flightmodel/forces/fnrml_aero");
	MPD_DR_fside_aero = XPLMFindDataRef("sim/flightmodel/forces/fside_aero");
	MPD_DR_faxil_aero = XPLMFindDataRef("sim/flightmodel/forces/faxil_aero");
	MPD_DR_fnrml_gear = XPLMFindDataRef("sim/flightmodel/forces/fnrml_gear");
	MPD_DR_fside_gear = XPLMFindDataRef("sim/flightmodel/forces/fside_gear");
	MPD_DR_faxil_gear = XPLMFindDataRef("sim/flightmodel/forces/faxil_gear");
	MPD_DR_m_total = XPLMFindDataRef("sim/flightmodel/weight/m_total");
	MPD_DR_the = XPLMFindDataRef("sim/flightmodel/position/theta");
	MPD_DR_psi = XPLMFindDataRef("sim/flightmodel/position/psi");
	MPD_DR_phi = XPLMFindDataRef("sim/flightmodel/position/phi");
	MPD_DR_airspeed = XPLMFindDataRef("sim/flightmodel/position/indicated_airspeed");
	MPD_DR_pitch = XPLMFindDataRef("sim/flightmodel/position/theta");
	MPD_DR_baro_press = XPLMFindDataRef("sim/weather/barometer_current_inhg");
	MPD_DR_verticle_accel = XPLMFindDataRef("sim/flightmodel/position/local_ay");

	memset(MPD_Buffer, 0, sizeof(MPD_Buffer));

	return 1;
}

//---------------------------------------------------------------------------

PLUGIN_API void	XPluginStop(void)
{
    XPLMDestroyWindow(MPD_Window);
	XPLMUnregisterFlightLoopCallback(MotionPlatformDataLoopCB, NULL);
}

//---------------------------------------------------------------------------

PLUGIN_API int XPluginEnable(void)
{
	return 1;
}

//---------------------------------------------------------------------------

PLUGIN_API void XPluginDisable(void)
{
}

//---------------------------------------------------------------------------

PLUGIN_API void XPluginReceiveMessage(XPLMPluginID inFrom, int inMsg, void * inParam)
{
}


//---------------------------------------------------------------------------
// Mandatory callback for SDK 2D Window
// Used to display the data to the screen

void MotionPlatformDataDrawWindowCallback(
                                   XPLMWindowID         inWindowID,    
                                   void *               inRefcon)
{

	float		rgb [] = { 1.0, 1.0, 1.0 };
	int			l, t, r, b;

	XPLMGetWindowGeometry(inWindowID, &l, &t, &r, &b);
	XPLMDrawTranslucentDarkBox(l, t, r, b);

	for (int i=0; i<6; i++)
		XPLMDrawString(rgb, l+10, (t-20) - (10*i), MPD_Buffer[i], NULL, xplmFont_Basic);
}                                   

//---------------------------------------------------------------------------
// Mandatory callback for SDK 2D Window
// Not used in this plugin

void MotionPlatformDataHandleKeyCallback(
                                   XPLMWindowID         inWindowID,    
                                   char                 inKey,    
                                   XPLMKeyFlags         inFlags,    
                                   char                 inVirtualKey,    
                                   void *               inRefcon,    
                                   int                  losingFocus)
{
}                                   

//---------------------------------------------------------------------------
// Mandatory callback for SDK 2D Window
// Not used in this plugin

int MotionPlatformDataHandleMouseClickCallback(
                                   XPLMWindowID         inWindowID,    
                                   int                  x,    
                                   int                  y,    
                                   XPLMMouseStatus      inMouse,    
                                   void *               inRefcon)
{
	return 1;
}                                      

//---------------------------------------------------------------------------
// FlightLoop callback to calculate motion data and store it in our buffers
const float standardPressure = 1013.25f;

float MotionPlatformDataLoopCB(float elapsedMe, float elapsedSim, int counter, void * refcon)
{
	MPD_CalculateMotionData();
	int rawPressureValue = (int)MPD_MotionData[0];
	float heading = MPD_MotionData[1];
	float roll = MPD_MotionData[2];
	float pitch = MPD_MotionData[3];
	float barometricPressure = MPD_MotionData[4];
	float verticalAcceleration = MPD_MotionData[5];

	sprintf(MPD_Buffer[0], "rawPitotPressure = %d", rawPressureValue);
	sprintf(MPD_Buffer[1], "Heading = %f", heading);
	sprintf(MPD_Buffer[2], "Roll = %f", roll);
	sprintf(MPD_Buffer[3], "Pitch = %f", pitch);
	sprintf(MPD_Buffer[4], "Barometric pressure = %f", barometricPressure);
	sprintf(MPD_Buffer[5], "Vertical Acceleration = %f", verticalAcceleration);

	char message[256];
	sprintf(message, "F:5,R:%f,P:%f,H:%f,G:%f,A:%d,B:%f,T:19.73<EOF>",roll, pitch, heading, verticalAcceleration, rawPressureValue, barometricPressure );
	SendSocketMessage(message);

	//SendSocketMessage("Hello<EOF>");
	return (float)0.1;
}

//---------------------------------------------------------------------------
// Original function used in the Xplane code.

float MPD_fallout(float data, float low, float high)
{
	if (data < low) return data;
	if (data > high) return data;
	if (data < ((low + high) * 0.5)) return low;
    return high;
}

//---------------------------------------------------------------------------
// Original function used in the Xplane code.

float MPD_fltlim(float data, float min, float max)
{
	if (data < min) return min;
	if (data > max) return max;
	return data;
}

//---------------------------------------------------------------------------
// Original function used in the Xplane code.

float MPD_fltmax2 (float x1,const float x2)
{
	return (x1 > x2) ? x1 : x2;
}

//---------------------------------------------------------------------------
// This is original Xplane code converted to use 
// our datarefs instead of the Xplane variables

void MPD_CalculateMotionData(void)
{
	const float degToRad = 0.01745329;
	const float hgToMillibar = 33.86389;
	float groundspeed = XPLMGetDataf(MPD_DR_groundspeed);
	float fnrml_prop = XPLMGetDataf(MPD_DR_fnrml_prop);
	float fside_prop = XPLMGetDataf(MPD_DR_fside_prop);
	float faxil_prop = XPLMGetDataf(MPD_DR_faxil_prop);
	float fnrml_aero = XPLMGetDataf(MPD_DR_fnrml_aero);
	float fside_aero = XPLMGetDataf(MPD_DR_fside_aero);
	float faxil_aero = XPLMGetDataf(MPD_DR_faxil_aero);
	float fnrml_gear = XPLMGetDataf(MPD_DR_fnrml_gear);
	float fside_gear = XPLMGetDataf(MPD_DR_fside_gear);
	float faxil_gear = XPLMGetDataf(MPD_DR_faxil_gear);
	float m_total = XPLMGetDataf(MPD_DR_m_total);
	float the = XPLMGetDataf(MPD_DR_the);
	float psi = XPLMGetDataf(MPD_DR_psi) ;
	float phi = XPLMGetDataf(MPD_DR_phi) * degToRad;
	float airspeed = XPLMGetDataf(MPD_DR_airspeed);
	float pitch = XPLMGetDataf(MPD_DR_pitch) * degToRad;
	float barometricPressure = XPLMGetDataf(MPD_DR_baro_press)*hgToMillibar;
	float verticleAccel = XPLMGetDataf(MPD_DR_verticle_accel);
	float  indicatedPitotPressure = 0.0538193f * pow(airspeed, 2) + 2178;

	float ratio = MPD_fltlim(groundspeed*0.2,0.0,1.0);
	float a_nrml= MPD_fallout(fnrml_prop+fnrml_aero+fnrml_gear,-0.1,0.1)/MPD_fltmax2(m_total,1.0);
	float a_side= (fside_prop+fside_aero+fside_gear)/MPD_fltmax2(m_total,1.0)*ratio;
	float a_axil= (faxil_prop+faxil_aero+faxil_gear)/MPD_fltmax2(m_total,1.0)*ratio;

	const float dp_Coef = 4.91744f;
	const float AIS_Baseline = 2178;

	auto mps = airspeed / 1.943844f;

	// The IMU send the raw ADC value, we have to exproximate it by inverting the equation we use in the PFD
	auto pitotSensorReadingEst = AIS_Baseline + mps * mps / dp_Coef;

	// Store the results in an array so that we can easily display it.

	MPD_MotionData[0] = pitotSensorReadingEst;// = indicatedPitotPressure;
	MPD_MotionData[1] = psi;//heading
	MPD_MotionData[2] = phi;//roll
	MPD_MotionData[3] = pitch;
	MPD_MotionData[4] = barometricPressure;
	MPD_MotionData[5] = verticleAccel;

}

//---------------------------------------------------------------------------

int SendSocketMessage(char* message) {
	WSADATA wsa = {};
	if (WSAStartup(MAKEWORD(2, 2), &wsa) != 0)
	{
		return 1;
	}


	ADDRINFO Hints, * AddrInfo, * AI;
	memset(&Hints, 0, sizeof(Hints));
	Hints.ai_family = AF_INET6;
	Hints.ai_socktype = SOCK_STREAM;
	Hints.ai_protocol = IPPROTO_TCP;
	int RetVal = getaddrinfo("::1", "11000", &Hints, &AddrInfo);
	SOCKET ConnSocket = OpenAndConnect();
	int ppp = send(ConnSocket, message, strlen(message), 0);
	closesocket(ConnSocket);
	return 0;
}
SOCKET OpenAndConnect()
{
	char* Server = (char*)"::1";
	char* PortName = (char*)"11000";
	int Family = AF_INET6;
	int SocketType = SOCK_STREAM;

	int iResult = 0;
	SOCKET ConnSocket = INVALID_SOCKET;

	ADDRINFO* AddrInfo = NULL;
	ADDRINFO* AI = NULL;
	ADDRINFO Hints;

	char* AddrName = NULL;

	memset(&Hints, 0, sizeof(Hints));
	Hints.ai_family = Family;
	Hints.ai_socktype = SocketType;
	Hints.ai_protocol = IPPROTO_TCP;
	iResult = getaddrinfo(Server, PortName, &Hints, &AddrInfo);
	if (iResult != 0) {
		printf("Cannot resolve address [%s] and port [%s], error %d: %s\n",
			Server, PortName, WSAGetLastError(), gai_strerror(iResult));
		return INVALID_SOCKET;
	}

	for (AI = AddrInfo; AI != NULL; AI = AI->ai_next) {
		ConnSocket = socket(AI->ai_family, AI->ai_socktype, AI->ai_protocol);
		if (ConnSocket == INVALID_SOCKET) {
			printf("Error Opening socket, error %d\n", WSAGetLastError());
			continue;
		}
		
		printf("Attempting to connect to: %s\n", Server ? Server : "localhost");
		if (connect(ConnSocket, AI->ai_addr, (int)AI->ai_addrlen) != SOCKET_ERROR)
			break;

		if (getnameinfo(AI->ai_addr, (socklen_t)AI->ai_addrlen, AddrName,
			sizeof(AddrName), NULL, 0, NI_NUMERICHOST) != 0) {
			strcpy_s(AddrName, sizeof(AddrName), "<unknown>");
			printf("connect() to %s failed with error %d\n", AddrName, WSAGetLastError());
			closesocket(ConnSocket);
			ConnSocket = INVALID_SOCKET;
		}
	}
	return ConnSocket;
}

int main(int argc, char* argv[]) { 
	int body = 0;
}