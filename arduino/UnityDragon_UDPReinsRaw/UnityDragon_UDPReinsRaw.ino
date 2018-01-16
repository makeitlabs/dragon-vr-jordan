/* 
  joemcder 11/2017
  Read buttons and send commands to Unity game thru UDP broadcast
  See:  https://github.com/shekit/dragon-vr-jordan  and 'UnityDragon_HTTPget.ino' for node.js version
  mods 1/1018
  -----------
  Pot readings sent as 0-100% without interpretation
  MPU6050 inertial unit added to detect foward(UP)and back(DOWN)  rocking.
  Pots will now indicate only LEFT,RIGHT,HALT
  
 */

#define CPU_ESP8266
//#define CPU_ESP32
 
#ifdef CPU_ESP8266
  #include <ESP8266WiFi.h>
  #include <WiFiUdp.h>
#endif

// Wemos Lolin32 Lite
#ifdef CPU_ESP32
  #include <WiFi.h>  //ESP32.  I removed standard WiFi Arduino library to avoid conflict
#endif

#include "I2Cdev.h"
#include "MPU6050_6Axis_MotionApps20.h"
#include <Wire.h>
boolean wifiConnected = false;
unsigned int udpPort = 3334;
WiFiUDP udp;
const char * udpAddress = "192.168.1.255";  // ippupy

const char* ssid     = "iPuppyJM";
const char* password = "dragonnet";
char* hexPrintBuf = "                                                      ";

#define BTNPRESSED  LOW
#define BTNUNPRESSED HIGH

unsigned char btnPin[] = {0,2,4,17,16}; 
bool btnState[] = {false,false,false,false,false}; 
char btnTagOn[5][8] = {"", "RI", "LE", "UP", "DN"};
char btnTagOff[5][10] = {"", "EL", "ER", "EU", "ED"};
char* btnTagRoll = "RL";
char* btnTagMove = "MV";
char* BtnCommand = btnTagRoll;

#ifdef CPU_ESP8266
  // The ESP8266 WILL NOT WORK PROPERLY!!!
  // This is a hack to let us compile for testing
  #define PotLeft A0  
  #define PotRight A0
#endif

#ifdef CPU_ESP32
  #define PotLeft A4  
  #define PotRight A5
#endif

unsigned int potLeftHi = 3000;  //A4 potentiometer input analog
unsigned int potLeftLow = 500;
unsigned int potLeftScale = potLeftHi-potLeftLow;  

unsigned int potRightHi = 3000;  //A5 potentiometer input analog
unsigned int potRightLow = 500; 
unsigned int potRightScale = potRightHi-potRightLow;   
unsigned long valLeft = 0;  
unsigned long valRight = 0;
byte PctLeft = 0;
byte PctRight = 0;



#define UdpBufLen 21
byte UdpMessageBuf[UdpBufLen];

unsigned int ledPin1 = 34;  //A6   turn on if network logon successful
unsigned int ledPin2 = 35;  //A7   blink for net activity

unsigned long lastMillis = 0;
unsigned long currentMillis = 0;
unsigned int frameCount = 0;  //cyclic
unsigned int frameMax = 5;

MPU6050 mpu;
#define INTERRUPT_PIN 5 

// MPU control/status vars
bool dmpReady = false;  // set true if DMP init was successful
uint8_t mpuIntStatus;   // holds actual interrupt status byte from MPU
uint8_t devStatus;      // return status after each device operation (0 = success, !0 = error)
uint16_t packetSize;    // expected DMP packet size (default is 42 bytes)
uint16_t fifoCount;     // count of all bytes currently in FIFO
uint8_t fifoBuffer[64]; // FIFO storage buffer

// orientation/motion vars
Quaternion q;           // [w, x, y, z]         quaternion container
VectorFloat gravity;    // [x, y, z]            gravity vector
float ypr[3];           // [yaw, pitch, roll]   yaw/pitch/roll container and gravity vector

volatile bool mpuInterrupt = false;     // indicates whether MPU interrupt pin has gone high
void dmpDataReady() {
    mpuInterrupt = true;
}


void setup() {
  int i;
  
  // initialize the serial communication:
  Serial.begin(115200);
  Serial.setTimeout(2);  //millis
  // initialize the ledPin as an output:
  pinMode(ledPin1, OUTPUT);
  digitalWrite(ledPin1, LOW);
  pinMode(ledPin2, OUTPUT);
  digitalWrite(ledPin2, LOW);
  for (i=1; i<=4; i++) {
    pinMode(btnPin[i], INPUT_PULLUP);    
  }
  //
  wifiConnected = connectWifi();
  if(wifiConnected){
    //send ROLL command ?
  }

  Wire.begin();
  Wire.setClock(400000);

  // initialize device
  Serial.println(F("Initializing I2C devices..."));
  mpu.initialize();
  pinMode(INTERRUPT_PIN, INPUT);

  // verify connection
  Serial.println(F("Testing device connections..."));
  Serial.println(mpu.testConnection() ? F("MPU6050 connection successful") : F("MPU6050 connection failed"));
  // load and configure the DMP
  Serial.println(F("Initializing DMP..."));
  devStatus = mpu.dmpInitialize();

  // supply your own gyro offsets here, scaled for min sensitivity
  mpu.setXGyroOffset(220);
  mpu.setYGyroOffset(76);
  mpu.setZGyroOffset(-85);
  mpu.setZAccelOffset(1788); // 1688 factory default for my test chip

  // make sure it worked (returns 0 if so)
  if (devStatus == 0) {
      // turn on the DMP, now that it's ready
      Serial.println(F("Enabling DMP..."));
      mpu.setDMPEnabled(true);

      // enable Arduino interrupt detection
      Serial.println(F("Enabling interrupt detection (Arduino external interrupt 0)..."));
      attachInterrupt(digitalPinToInterrupt(INTERRUPT_PIN), dmpDataReady, RISING);
      mpuIntStatus = mpu.getIntStatus();

      // set our DMP Ready flag so the main loop() function knows it's okay to use it
      Serial.println(F("DMP ready! Waiting for first interrupt..."));
      dmpReady = true;

      // get expected DMP packet size for later comparison
      packetSize = mpu.dmpGetFIFOPacketSize();
  } else {
      // ERROR!
      // 1 = initial memory load failed
      // 2 = DMP configuration updates failed
      // (if it's going to break, usually the code will be 1)
      Serial.print(F("DMP Initialization failed (code "));
      Serial.print(devStatus);
      Serial.println(F(")"));
  }


}

void loop() {
  int i;
  int p;
  int btnCount = 0;

  if (!dmpReady) { 
    // MPU 6050 didn't initialize properly,
    // send invalid values
    ypr[0] = -255.00;
    ypr[1] = -255.00;
    ypr[2] = -255.00;
  }
  
    currentMillis = millis();
    //"frame rate" 10/sec
    if ((currentMillis - lastMillis) > 50) {

      frameCount++;
      lastMillis = currentMillis;
      //check 4 buttons
      BtnCommand = "NN";  //none
      for(i=1; i<=4; i++ ) {
        p = digitalRead(btnPin[i]);
        if (p==LOW) {  //LOW  0  means pressed
          btnCount++;  //more than 1 pressed? 
          btnState[i] = BTNPRESSED;
          Serial.println(btnPin[i]);
        }
        else {  //unpressed
          if (btnState[i] == BTNPRESSED){
            BtnCommand = btnTagOff[i];  //if previusly pressed must send uppressed message
            btnState[i] = BTNUNPRESSED;        
          }
        }
      }//btn check loop
       
      switch (btnCount) {
      case 0:
        //need code here?  
        break;
      case 1:
        for(i=1; i<=4; i++ ) {
          if (btnState[i] == BTNPRESSED)
             BtnCommand = btnTagOn[i];
        }    
        break;
      case 2:
        BtnCommand = btnTagRoll;
        break;  
      case 3:
        BtnCommand = btnTagMove;
        break;    
      default:
        break;
     }
      //get potentiometer values
      valLeft = analogRead(PotLeft) - potLeftLow;
      if (valLeft<0)
        valLeft = 0;
      if (valLeft>potLeftScale)
        valLeft = potLeftScale;
        
      valRight = analogRead(PotRight)- potRightLow;
      if (valRight<0)
        valRight = 0;
      if (valRight>potRightScale)
        valRight = potRightScale;
      PctLeft = (valLeft * 100)/potLeftScale;
      PctRight = (valRight * 100)/potRightScale;
  
      UdpBufClear();
      UdpMessageBuf[0] = 1; //command type
      UdpMessageBuf[1] = BtnCommand[0];
      UdpMessageBuf[2] = BtnCommand[1];
      UdpMessageBuf[3] = PctLeft; //left pot
      UdpMessageBuf[4] = PctRight; //right pot
  
      //6050 values
    
      int16_t yaw = int(ypr[0] * 180/M_PI);
      int16_t pitch = int(ypr[1] * 180/M_PI);
      int16_t roll = int(ypr[2] * 180/M_PI);
  
      UdpMessageBuf[5] = byte((yaw & 0xff00) >> 8);
      UdpMessageBuf[6] = byte(yaw & 0x00ff);
  
      UdpMessageBuf[7] = byte((pitch & 0xff00) >> 8);
      UdpMessageBuf[8] = byte(pitch & 0x00ff);

      //Serial.print(ypr[1]  * 180/M_PI);
      //Serial.print(" | ");
      //Serial.print(pitch);
      //Serial.print(" = ");
      //Serial.print(UdpMessageBuf[7]);
      //Serial.print(" ");
      //Serial.println(UdpMessageBuf[8]);
  
      UdpMessageBuf[9] = byte((roll & 0xff00) >> 8);
      UdpMessageBuf[10] = byte(roll & 0x00ff);
  
      SendCommand(UdpMessageBuf);
       
      if (frameCount > 5) {
        frameCount = 0;
        // SendCommand(UdpMessageBuf);
      }   
  
    
    }
  
  if (dmpReady && (mpuInterrupt || (fifoCount >= packetSize))) {
    // this code will execute whenever the MPU6050 needs attention,
    // as long as it was detected during setup()


    // reset interrupt flag and get INT_STATUS byte
    mpuInterrupt = false;
    mpuIntStatus = mpu.getIntStatus();

    // get current FIFO count
    fifoCount = mpu.getFIFOCount();

    // check for overflow (this should never happen unless our code is too inefficient)
    if ((mpuIntStatus & 0x10) || fifoCount == 1024) {
        // reset so we can continue cleanly
        mpu.resetFIFO();
        Serial.println(F("FIFO overflow!"));

    // otherwise, check for DMP data ready interrupt (this should happen frequently)
    } else if (mpuIntStatus & 0x02) {
        // wait for correct available data length, should be a VERY short wait
        while (fifoCount < packetSize) fifoCount = mpu.getFIFOCount();

        // read a packet from FIFO
        mpu.getFIFOBytes(fifoBuffer, packetSize);
        
        // track FIFO count here in case there is > 1 packet available
        // (this lets us immediately read more without waiting for an interrupt)
        fifoCount -= packetSize;

              // display Euler angles in degrees
            mpu.dmpGetQuaternion(&q, fifoBuffer);
            mpu.dmpGetGravity(&gravity, &q);
            mpu.dmpGetYawPitchRoll(ypr, &q, &gravity);
            //Serial.print("ypr\t");
            //Serial.print(ypr[0] * 180/M_PI);
            //Serial.print("\t");
            //Serial.print(ypr[1] * 180/M_PI);
            //Serial.print("\t");
            //Serial.println(ypr[2] * 180/M_PI);
    } // else if (mpuIntStatus & 0x02)
  }  // else if (dmpReady) 
    

 return;
} // loop()

void UdpBufClear() {
  for (int c=0; c < UdpBufLen; c++) {
    UdpMessageBuf[c] = 0;
  }
}


//sending UDP broadcast
//send byte array.  1st byte is command ID followed by 20 bytes of data
void SendCommand(byte cmd[]) {
    // Serial.print("Send command:");
    // Serial.println(binArrayToHexStr(cmd, UdpBufLen));
    udp.beginPacket(udpAddress, udpPort);
    udp.write(cmd, UdpBufLen);
    udp.endPacket();
}


// connect to wifi – returns true if successful or false if not
boolean connectWifi(){
  boolean state = true;
  int i = 0;
  Serial.println("");
  Serial.println("Attempting to connect to access point...");    
  delay(1000);
  int status = WiFi.status();
  if (status == WL_CONNECTED) {
    WiFi.disconnect();
    delay(100);
  }

  
  WiFi.begin(ssid, password);
  Serial.println("");
  Serial.print("Connecting to WiFi: "); Serial.println (ssid);
  // Wait for connection
  Serial.print("Connecting");
  while (WiFi.status() != WL_CONNECTED) {
    delay(1000);
    Serial.print(".");
    if (i > 25){  // 10 tries before giving up
      state = false;
      break;
    }
    i++;
  }
  
  if (state){
    Serial.println("");
    printWifiStatus();
    udp.begin(udpPort);
  }
  else {
    Serial.println("");
    Serial.println("Connection failed.");
  }
  return state;
}

void printWifiStatus() {
  // print the SSID of the network you're attached to:
  Serial.print("SSID: ");
  Serial.println(WiFi.SSID());
  // print your WiFi  IP address:
  IPAddress ip = WiFi.localIP();
  Serial.print("IP Address: ");
  Serial.println(ip);

  // print the received signal strength:
  long rssi = WiFi.RSSI();
  Serial.print("signal strength (RSSI):");
  Serial.print(rssi);
  Serial.println(" dBm");
}
const char hexmap[] = {'0', '1', '2', '3', '4', '5', '6', '7',
                           '8', '9', 'a', 'b', 'c', 'd', 'e', 'f'};
char * binArrayToHexStr(byte ba[], int count) {
  for (int i=0; i<count; i++) {     
    hexPrintBuf[2 * i]     = hexmap[(ba[i] & 0xF0) >> 4];
    hexPrintBuf[2 * i + 1] = hexmap[ba[i] & 0x0F];
  }
  hexPrintBuf[2*count + 1] = 0;
  return hexPrintBuf;
}





