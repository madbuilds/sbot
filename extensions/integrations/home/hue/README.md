# Integration: Philips Hue (Smart Home)

- [Discord: Link to Integration Discussion](https://discord.com/channels/834650675224248362/1295383440291659776)

![DEMO](./docs/demo.gif)

# Installation
* download [Q42.HueApi.dll - HUE API library](./lib/Q42.HueApi.dll) v3.23.1.0 (.net 4.5)
  - created by: https://github.com/michielpost/Q42.HueApi look in NuGet packages
* copy this downloaded lib to - **STREMER_BOT_INSTALLATION_PATH**/dlls/Q42.HueApi.dll
* copy below import code and paste into Streamer.Bot 
* ```text
  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
  ```
  ***NOTE:***\
  *if you want to just copy C# code instead make sure you rename class*\
  *the class name should be `public class CPHInline {}`*
* get your HUE Bridge IP Address 
  - *can be found in router or use: https://discovery.meethue.com/* \
    *should be something like: 192.168.\*.\* or similar*
* paste HUE Bridge IP into: `hue.bridge.ip` variable
  - this variable available inside of `[API] HUE` action
* make sure library you downloaded is in use by `[API] HUE`
  - import it manually if it's not: `Execution Code -> References` tab of `Execute Code` sub-action
* Close Streamer.Bot 
* Press physical button on HueBridge (Link button on top of the Bridge)
* Open Streamer.Bot again after button pressed (**you have not much time HURRY!**)
  - it will connect to your bridge and register
  - once registered you should see some random string\  
    inside of `Global Variables` of Streamer.Bot under `integration.phillipsHue.bridge.appKey`
* done, now you can use it

# How to Use
* When installation complete (complete Installation steps ok?)
* Open log file in **STREMER_BOT_INSTALLATION_PATH**/logs/ folder
  - you need last created log file (just sort it by date you will figure it out)
* There should be all your available Lights/Plugs details printed as in below example
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
* you interested on `HUE ID`, other details is for you to help to identify what you want
* copy `HUE ID` number, and paste into `hue.id` property inside of `[HUE] CHANGE COLOR` action
  - this is just an example action, you are the smart, you know what to do with it
* hit Test Trigger to execute action
* does color of the light changed?

# Available Methods
* #### TurnOnHueID
  Executes method to turn your Device ON by it's `HUE ID`
  ```text
  hue.id - mandatory
  ```
* #### TurnOffHueID 
  Executes method to Turn your device OFF by it's `HUE ID`
  ```text
  hue.id - mandatory
  ```
* #### ChangeLightColor
  Executes method to CHANGE light color/brightness/saturation
  ```text
  hue.id         - mandatory
  hue.hexColor   - mandatory
  hue.saturation - optional (values from 0 to 254)
  hue.brightness - optional (values from 0 to 254)
  
  if saturation/brightness is not set, 
  then light saturation/brightness will not be changed 
  ```

# Changelog
* 1.2.0 - migration to GitHub: GH-10
  - all code/instructions moved to github for better support
* 1.1.0 - new methods been created (see in: [available methods](#available-methods) section)
  - [turn on hue by id](#turnonhueid)
  - [turn off hue by id](#turnoffhueid)
* 1.0.0 - initial version
