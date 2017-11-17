import { RtmClient, CLIENT_EVENTS, RTM_EVENTS } from '@slack/client';
import decode from 'decode-html';
import { rcon, exec } from './rcon';
import config from '../../config';

const rtm = new RtmClient(config.slack.token);

const slack = {
  ready: true,
  channel: null,
};

rtm.on(CLIENT_EVENTS.RTM.AUTHENTICATED, (data) => {
  slack.channel = data.channels.find((channel) => {
    return channel.name === config.slack.channel;
  });

  if (!slack.channel) {
    slack.channel = data.groups.find((group) => {
      return group.name === config.slack.channel;
    });
  }

  if (!slack.channel) {
    console.error('Couldnt find the slack channel..');
  }
});

rtm.on(CLIENT_EVENTS.RTM.RTM_CONNECTION_OPENED, () => {
  slack.ready = true;
});

rtm.on(RTM_EVENTS.MESSAGE, (message) => {
  if (!message.text || message.channel !== slack.channel.id || message.type !== 'message') {
    return;
  }

  decode(message.text).split('\n').forEach((line) => {
    console.log(line.substring(0, 1));

    if (line.substring(0, 2) === '>>') {
      exec(`${line.substring(2)}`);
      console.log(`[RCON:OUT]: ${line.substring(2)}`);
    } else if (line.substring(0, 1) === '>') {
      exec(`gl_chat_admin ${line.substring(1)}`);
      console.log(`[Chat]: ${line.substring(1)}`);
    }
  });
});

rcon.on('message', (message) => {
  console.log(`[RCON:IN]: ${message.message}`)
  if (message.message.substring(0, 6) === '[Chat]') {
    const msg = message.message.substring(7);

    if (slack.channel && slack.ready) {
      rtm.sendMessage('>' + msg, slack.channel.id);
    }
  } else {
    if (slack.channel && slack.ready) {
      rtm.sendMessage(`\`\`\`${message.message}\`\`\``, slack.channel.id);
    }
  }
});

rtm.start();
