import WebRcon from 'webrconjs';
import config from '../../config';
import telegram from './telegram';
import { setTimeout } from 'timers';

export const rcon = new WebRcon(config.rcon.host, config.rcon.port);
rcon.connect(config.rcon.pass);

export const exec = (message) => {
  if (rcon.status !== 'CONNECTED') {
    console.warn(`Failed to execute rcon message (${message}), retying in 500ms..`);
    setTimeout(() => exec(message), 500);
    try { rcon.connect(config.rcon.pass); } catch (e) {}
    return;
  }

  rcon.run(message);
};

rcon.on('message', (ctx) => {
  // Handle raid alerts
  if (ctx.message.substring(0, 14) === '[Raid] [alert]') {
    telegram.send(ctx.message.substring(15), `We have detected a start of a raid on one of your bases!\n\nIf the raid is still going on after 15 minutes we will sent you a new notification!`);
    return;
  }

  // Handle ping messages
  if (ctx.message.substring(0, 11) === '[RCON] ping') {
    exec(`rcon.pong ${ctx.message.substring(12)}`);
    return;
  }
});

setInterval(() => exec('keep-alive'), 1000 * 60 * 5);

export default { rcon, exec };
