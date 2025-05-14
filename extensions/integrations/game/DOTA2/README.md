# Integration: Dota 2

- [Discord: Link to Integration Discussion](https://discord.com/channels/834650675224248362/1299390655780360323)

![DEMO](./docs/demo/SILENCE.gif)

# Installation
For using it, dota2 game needs to be prepared to allow send events to the target host (SBot in this case)
1) Open DOTA 2 game installation directory, and navigate to `game -> dota -> cfg` directory:
    - example: `{STEAM_PATH}\steamapps\common\dota 2 beta\game\dota\cfg`
2) Create new folder with the name: `gamestate_integration`
    - example: `{STEAM_PATH}\steamapps\common\dota 2 beta\game\dota\cfg\gamestate_integration`
3) Copy configuration file which has all required data inside (file below):
    - copy this: [file to be copied](./docs/gamestate_integration/gamestate_integration_sbot.cfg)
      - it has `http://127.0.0.1:13052/` as a target host (can be changed if needed)
    - to here: `{STEAM_PATH}\steamapps\common\dota 2 beta\game\dota\cfg\gamestate_integration`
4) copy below import code and paste into Streamer.Bot
   ```text
   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
   ```
   ***NOTE:***\
   *if you want to just copy [C# code](./Dota2GSI.cs) instead, make sure you rename class*\
   *the class name should be `public class CPHInline {}`*
5) _*\[OPTIONAL\]*_ in case you want to change `host:port` do below (if not go to step 6)
    - change `http://127.0.0.1:13052/` to whatever address you want in [file](./docs/gamestate_integration/gamestate_integration_sbot.cfg)
    - make sure your firewall/portforwarding allows connecting to the host
      port forwarding you might need to do on your router (depending on what ip you use)\
      to allow connections from external you can use below command:\
      `netsh http add urlacl url=http://+:13052/ user=PCNAME\admin`\
      change `PCNAME\admin` to your actual windows username
6) Restart StreamerBot or just hit Test Trigger to execute the code, so Dota2 Triggers are registered.
7) Have fun \& go crazy! 🙂

# How to Use
1) once [installation](#installation) steps are complete (is completed right?)
2) create any action in StreamerBot
3) in triggers section `right click` -> `Custom` -> `dota` -> `select what you like`
   ![triggers](./docs/screens/triggers.png)
4) play the game. Enjoy :)

# Changelog
* 1.0.0 *(14.05.2025)* - **initial version** [Pull Request: 19](https://github.com/madbuilds/sbot/pull/19)
  - player events
  - hero events
  - map events

# reference: 
* https://github.com/antonpup/Dota2GSI
* https://github.com/MrBean355/dota2-gsi/wiki/Dota-2-Setup