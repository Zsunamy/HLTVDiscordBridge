[![License](https://i.creativecommons.org/l/by-nc/4.0/88x31.png)](http://creativecommons.org/licenses/by-nc/4.0/)
[![Discord Bots](https://top.gg/api/widget/status/807182830752628766.svg)](https://top.gg/bot/807182830752628766)
[![Discord Bots](https://top.gg/api/widget/servers/807182830752628766.svg)](https://top.gg/bot/807182830752628766)
[![Discord Bots](https://top.gg/api/widget/upvotes/807182830752628766.svg)](https://top.gg/bot/807182830752628766)
# HLTV
## A bot that interacts with HLTV to inform you about the latest news and matches!
## Click [here](https://discord.com/oauth2/authorize?client_id=807182830752628766&permissions=1073785936&scope=bot) to add the bot to your server.
This bot can automatically send messages for the latest match results, news and started/ended events on HLTV.
We also provide commands which search HLTV and provide you with information about players, teams and much more!
### Setup (only admins can use these commands)
| Command | Parameters | Description |
|:-:|:-:|:-:|
| !init | [#channel] (optional) | Sets the current or specified channel as the default channel for news and results. |
| !set stars | [number between <br /> 0 and 5] | If a completed match has less than the specified stars, then it won't send a message on your server. <br /> The number of stars depends on the ranking of both teams and which event at which stage the match is. <br /> We are not resposible for the amount of stars a match recieves. |
| !set prefix | [default: !] | Changes the prefix to a specified value. |
| !set featuredevents | [true or false] | If set true, the bot will only send a message if a featured event just stared/ended. |
| !set news | [true or false] | Disables/Enables autmoated messages about recent news.|
| !set results | [true or false] | Disables/Enables automated messages about the last completed match.|
| !set events | [true or false] | Disables/Enables automated messages started/ended events.|
### Usage
| Command | Parameters | Description |
|:-:|:-:|:-:|
| !ranking | [country/region] (optional) | Informs you about the global ranking or in a specified country/region |
| !team | [name] | Shows you a summary about the specified team. The information are sometimes a week behind.|
| !player | [playername] | Gives you selected information about a player. <br /> The player will be cached for seven days for performance reasons. <br /> The bot may be slow to respond to this command.|
| !upcoming | [date] (optional) or [team] (optional)| Lists the next scheduled matches of a specified team or on a selected date. |
| !upcomingevents | --- | Responds with all currently scheduled upcoming events in the next 30 days. |
| !live | --- | This command will give you a list of ongoing matches with links to all livestreams and the HLTV page of that match. |
| !event | [name] | Informs about a specified event.|
| !events | --- | Shows you all ongoing events with links to their HLTV page. |
| !help | [command (optional)] | Like any other help command, this will send you a similar help like this. |
| !about | --- | About us |
## About us
We are a team of three developers who have been working on this bot during the last three months.
If you have any questions issues about this bot, message us on discord (Revilum#9569, Marcoooo
#0492 or \~𝕷𝖆𝖍𝖚𝖘𝖆~#0699) or write us an [issue](https://github.com/Zsunamy/HLTVDiscordBridge/issues) on Github. If you want to support us, vote for our bot on top.gg or send us a donation on [patreon](https://www.patreon.com/zsunamy).
