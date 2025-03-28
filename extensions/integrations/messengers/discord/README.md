# Integration: Discord

- [Discord: Link to Integration Discussion](https://discord.com/channels/834650675224248362/1299390655780360323)

![DEMO](./docs/demo.gif)

# Installation
For using it, Discord Bot needs to be created, it will allow to connect to Discord API:
1) Open Discord DeveloperPortal: https://discord.com/developers/applications
2) Create New Application with name you would like to have.
3) Open newly created Application Settings -> Bot section
   - you can setup Avatar and Banner of bot
   - get `BotToken` - required part, will be needed in next steps
   - **Important**: enable `Message Content Intent` and `Server Members Intent` if this disabled, bot will not start
4) copy below import code and paste into Streamer.Bot
   ```text
   import code
   ```
5) put your `BotToken` into integration
   - copy `BotToken` from created Discord Bot above
   - paste it into `[API] Discord` Action, `bot.token` property.
6) Invite your newly created Bot into Discord Server
   - invite link is: https://discord.com/oauth2/authorize?client_id={CLIENT_ID}&scope=bot&permissions=532844898368
   - replace {CLIENT_ID} with your actual client_id, it's in `Settings` -> `General information` -> `application id`): 
7) Restart StreamerBot or just hit Test Trigger to execute the code, so Discord Triggers are registered.
8) Have fun! 🙂

# How to Use

# Available Methods

# Changelog