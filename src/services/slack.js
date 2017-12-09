import { RtmClient, CLIENT_EVENTS, RTM_EVENTS } from '@slack/client';
import decode from 'decode-html';
import { rcon, exec } from './rcon';
import telegram from './telegram';
import config from '../../config';

// Create the slack rtm api connection
const rtm = new RtmClient(config.slack.token);

// Slack cached config data
const slackData = {
  ready: false,
  channel: {
    chat: null,
    rcon: null,
  },
};

// Fire the event when the RTM has been autorized
rtm.on(CLIENT_EVENTS.RTM.AUTHENTICATED, (data) => {
  slackData.channel.chat = data.channels.find((channel) => channel.name === config.slack.channel.chat);
  slackData.channel.rcon = data.channels.find((channel) => channel.name === config.slack.channel.rcon);

  if (!slackData.channel.chat) {
    slackData.channel.chat = data.groups.find((channel) => channel.name === config.slack.channel.chat);
  }

  if (!slackData.channel.rcon) {
    slackData.channel.rcon = data.groups.find((channel) => channel.name === config.slack.channel.rcon);
  }

  if (slackData.channel.chat && slackData.channel.rcon) {
    slackData.ready = true;
  } else {
    console.error('Could not find the slack channels:', slackData.channel);
  }
});

// Handle incomming messages
rtm.on(RTM_EVENTS.MESSAGE, (message) => {
  if (!message.text || message.type !== 'message' || (message.channel !== slackData.channel.chat.id && message.channel !== slackData.channel.rcon.id)) {
    return;
  }

  decode(message.text).split('\n').forEach((line) => {
    if (message.channel === slackData.channel.chat.id && line.substring(0, 1) === '>') {
      exec(`chat.admin ${line.substring(1)}`);
      console.log(`Admin chat: ${line.substring(1)}`);
    } else if (message.channel === slackData.channel.rcon.id && line.substring(0, 2) === '>>') {
      // Service command
      const params = line.substring(2).split(' ');
      let uid, message;
      switch (params.shift()) {
        case "telegram":
          uid = params.shift();
          message = params.join(' ');

          if (!uid || message.length === 0) {
            return;
          }

          telegram.send(uid, message);
          break;

        case "telegram-broadcast":
          message = params.join(' ');

          if (message.length === 0) {
            return;
          }

          telegram.broadcast(message);
          break;

        case "telegram-admin":
          message = params.join(' ');

          if (message.length === 0) {
            return;
          }

          telegram.broadcastAdmin(message);
          break;
      }
    } else if (message.channel === slackData.channel.rcon.id && line.substring(0, 1) === '>') {
      // Server command
      exec(line.substring(1));
      console.log(`Admin rcon: ${line.substring(1)}`);
    }
  });
});

// Handle incomming rcon messages
rcon.on('message', (ctx) => {
  console.log(`Rcon Data: ${ctx.message}`);

  // Send chat related data to the chat channel
  if (ctx.message.substring(0, 6) === '[Chat]' && ctx.message.substring(0, 21) !== '[Chat] [rcon] [admin]') {
    const message = ctx.message.substring(7);
    if (slackData.ready) {
      rtm.sendMessage(`>${message}`, slackData.channel.chat.id);
    }
  }

  // Send all data to the rcon channel
  if (slackData.ready) {
    rtm.sendMessage(`\`\`\`${ctx.message}\`\`\``, slackData.channel.rcon.id);
  }
});

// Start the slack connection
rtm.start();
