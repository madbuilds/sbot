# Integration: Philips Hue (Smart Home)

- [Discord: Link to Integration Discussion](https://discord.com/channels/834650675224248362/1295383440291659776)

![DEMO](./docs/demo.gif)

# Installation
1) download [Q42.HueApi.dll - HUE API library](./lib/Q42.HueApi.dll) v3.24.0 (.net 4.5)
   - created by: https://github.com/michielpost/Q42.HueApi look in NuGet packages
   - original nuget: https://www.nuget.org/packages/Q42.HueApi/3.24.0
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
* 1.3.0 - Bridge Pro support [Pull Request 14](https://github.com/madbuilds/sbot/pull/23)
* 1.2.1 - logs fixed [Pull Request 13](https://github.com/madbuilds/sbot/pull/13)
* 1.2.0 - migration to GitHub: [Pull Request 10](https://github.com/madbuilds/sbot/pull/10)
  - all code/instructions moved to github for better support
* 1.1.0 - new methods been created (see in: [available methods](#available-methods) section)
  - [turn on hue by id](#turnonhueid)
  - [turn off hue by id](#turnoffhueid)
* 1.0.0 - initial version
