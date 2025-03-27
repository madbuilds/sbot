# Integration: Philips Hue (Smart Home)

- [Discord: Link to Library Discussion](https://discord.com/channels/834650675224248362/1295383440291659776)

# Installation
- download [Q42.HueApi.dll - HUE API library](./lib/Q42.HueApi.dll) v3.23.1.0 (.net 4.5)
  - created by: https://github.com/michielpost/Q42.HueApi look in NuGet packages
- copy this downloaded lib to - **STREMER_BOT_INSTALLATION_PATH**/dlls/Q42.HueApi.dll
- copy below import code and paste into Streamer.Bot 
- ```text
  import code
  ```
- get your HUE Bridge IP Address 
  - *can be found in router ot use: https://discovery.meethue.com/* \
    *should be something like: 192.168.\*.\* or similar*
- paste HUE Bridge IP into: `hue.bridge.ip` variable
  - this variable available inside of `[API] HUE` action
- make sure library you downloaded is in use by `[API] HUE`
  - import it manually if it's not: `Execution Code -> References` tab of `Execute Code` sub-action
- Close Streamer.Bot 
- Press physical button on HueBridge (Link button on top of the Bridge)
- Open Streamer.Bot again after button pressed (**you have not much time HURRY!**)
  - it will connect to your bridge and register
  - once registered you should see some random string\  
    inside of `Global Variables` of Streamer.Bot under `integration.phillipsHue.bridge.appKey`
- done, now you can use it

# How to Use
- When installation complete (complete Installation steps ok?)
- Open log file in **STREMER_BOT_INSTALLATION_PATH**/logs/ folder
  - you need last created log file (just sort it by date you will figure it out)
- There should be all your available Lights/Plugs details printed as in below example
  - ```text
    [2025-03-22 11:56:50.013 INF] INFO : [API] HUE :: PRINT ALL THE AVAILABLE HUE LIGHTS
    [2025-03-22 11:56:50.013 INF] INFO : [API] HUE :: =====================
    [2025-03-22 11:56:50.013 INF] INFO : [API] HUE :: HUE ID    : 1
    [2025-03-22 11:56:50.013 INF] INFO : [API] HUE :: HUE NAME  : Hue color lamp 1
    [2025-03-22 11:56:50.013 INF] INFO : [API] HUE :: HUE TYPE  : Extended color light
    [2025-03-22 11:56:50.013 INF] INFO : [API] HUE :: HUE MODEL : LCA001
    [2025-03-22 11:56:50.014 INF] INFO : [API] HUE :: HUE STATE : OFF
    [2025-03-22 11:56:50.014 INF] INFO : [API] HUE :: =====================
    [2025-03-22 11:56:50.014 INF] INFO : [API] HUE :: HUE ID    : 4
    [2025-03-22 11:56:50.014 INF] INFO : [API] HUE :: HUE NAME  : Kitchen - plug(refrigerator)
    [2025-03-22 11:56:50.014 INF] INFO : [API] HUE :: HUE TYPE  : On/Off plug-in unit
    [2025-03-22 11:56:50.014 INF] INFO : [API] HUE :: HUE MODEL : LOM001
    [2025-03-22 11:56:50.014 INF] INFO : [API] HUE :: HUE STATE : ON
    ``` 
- you interested on `HUE ID`, other details is for you to help to identify what you want
- copy `HUE ID` number, and paste into `hue.id` property inside of `[HUE] CHANGE COLOR` action
  - this is just an example action, you are the smart, you know what to do with it
- hit Test Trigger to execute action
- does color of the light changed?

# Available Methods
- ### TurnOnHueID
  Executes method to turn your Device ON by it's `HUE ID`
  ```text
  hue.id - mandatory
  ```
- ### TurnOffHueID 
  Executes method to Turn your device OFF by it's `HUE ID`
  ```text
  hue.id - mandatory
  ```
- ### ChangeLightColor
  Executes method to CHANGE light color/brightness/saturation
  ```text
  hue.id         - mandatory
  hue.hexColor   - mandatory
  hue.saturation - optional (values from 0 to 254)
  hue.brightness - optional (values from 0 to 254)
  
  if saturation/brightness is not set, 
  then light saturation/brightness will not be changed 
  ```