[![License](https://i.creativecommons.org/l/by-nc/4.0/88x31.png)](http://creativecommons.org/licenses/by-nc/4.0/)
[![Discord Bots](https://top.gg/api/widget/status/807182830752628766.svg)](https://top.gg/bot/807182830752628766)
[![Discord Bots](https://top.gg/api/widget/servers/807182830752628766.svg)](https://top.gg/bot/807182830752628766)
[![Discord Bots](https://top.gg/api/widget/upvotes/807182830752628766.svg)](https://top.gg/bot/807182830752628766)
# HLTV
## A bot that interacts with HLTV to inform you about the latest news and matches!
This bot automatically send messages for the latest results, news and started/ended events. For more information see [here](#Setup).
We also provide commands which scalps through HLTV and can provide you with information about players, teams and much more! For more details see [here](#Usage).
### Setup (only admins can use these commands)
| Command | Parameters | Description |
|:-:|:-:|:-:|
| !init | [#channel] (optional) | sets the current or specified channel as the default channel for news and results. |
| !minstars | [number between <br /> 0 and 5] | If a completed match has less than the specified stars, then it won't send a message on your server. <br /> The number of stars depends on the ranking of both teams and which event at which stage the match is. <br /> We are not resposible for the amount of stars a match recieves. |
| !featuredevents | [true or false] | If set true, the bot will only send a message if a featured event just stared/ended. |
| !prefix | [default: !] | Changes the prefix to a specified value. |
### Usage
| Command | Parameters | Description |
|:-:|:-:|:-:|
| !help | --- | Like any other help command, this will send you a similar help like this. |
| !ranking | [country] (optional) | Informs you about the global ranking or in a specified country |
| !player | [playername] | Gives you selected information about a player. <br /> The player will be cached for seven weeks for performance reasons. <br /> The bot may be slow to respond to this command.|
| !upcoming | [date] (optional) or [team] (optional)| lists the next scheduled matches of a specified team or on a selected date. |
| !upcomingevents | --- | responds with all currently scheduled upcoming events in the next 30 days. |
| !live | --- | This command will give you a list of ongoing matches with links to all livestreams and the HLTV page of that match. |
| !event | [name] | informs about a specified event (WIP).|
| !events | --- | shows you all ongoing events with links to their HLTV page. |
| !about | --- | Thats us! Try it yourself. |
## About us
We are a team of three developers who have been working on this bot during the last thee months.
If you have any questions issues about this bot, message us on discord (Revilum#9569 or Marcoooo
#0492) or write us an [issue](https://github.com/Zsunamy/HLTVDiscordBridge/issues) on Github. If you want to support us, you can leave a vote for us and send us a donation on [patreon](https://www.patreon.com/zsunamy).
