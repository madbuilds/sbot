# Integration: Philips Hue (Smart Home)

- [Discord: Link to Integration Discussion](https://discord.com/channels/834650675224248362/1295383440291659776)

![DEMO](./docs/demo.gif)

# Installation
1) download [Q42.HueApi.dll - HUE API library](./lib/Q42.HueApi.dll) v3.23.1.0 (.net 4.5)
   - created by: https://github.com/michielpost/Q42.HueApi look in NuGet packages
   - asd
2) copy this downloaded lib to - **STREMER_BOT_INSTALLATION_PATH**/dlls/Q42.HueApi.dll
3) copy below import code and paste into Streamer.Bot 
   ```text
   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
   ```
   ***NOTE:***\
   *if you want to just copy C# code instead make sure you rename class*\
   *the class name should be `public class CPHInline {}`*
4) get your HUE Bridge IP Address 
   - *can be found in router or use: https://discovery.meethue.com/* \
     *should be something like: 192.168.\*.\* or similar*
5) paste HUE Bridge IP into: `hue.bridge.ip` variable
   - this variable available inside of `[API] HUE` action
6) make sure library you downloaded is in use by `[API] HUE`
   - import it manually if it's not: `Execution Code -> References` tab of `Execute Code` sub-action
7) Close Streamer.Bot 
8) Press physical button on HueBridge (Link button on top of the Bridge)
9) Open Streamer.Bot again after button pressed (**you have not much time HURRY!**)
   - it will connect to your bridge and register
   - once registered you should see some random string\  
     inside of `Global Variables` of Streamer.Bot under `integration.phillipsHue.bridge.appKey`
10) done, now you can use it

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
* 1.2.1 - logs fixed [Pull Request 13](https://github.com/madbuilds/sbot/pull/13)
* 1.2.0 - migration to GitHub: [Pull Request 10](https://github.com/madbuilds/sbot/pull/10)
  - all code/instructions moved to github for better support
* 1.1.0 - new methods been created (see in: [available methods](#available-methods) section)
  - [turn on hue by id](#turnonhueid)
  - [turn off hue by id](#turnoffhueid)
* 1.0.0 - initial version
