import { RtmClient, CLIENT_EVENTS, RTM_EVENTS } from '@slack/client';
import rcon from './rcon';
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
  if (message.channel === slack.channel.id && message.type === 'message') {
    rcon.run(`gl_chat_admin ${message.text}`);
  }
});

rcon.on('message', (message) => {
  console.log(message.message);
  if (message.message.substring(0, 6) === '[Chat]') {
    const msg = message.message.substring(7);

    if (slack.channel && slack.ready) {
      rtm.sendMessage(msg, slack.channel.id);
    }
  }
});

rtm.start();