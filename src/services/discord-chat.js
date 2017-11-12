import Discord from 'discord.js';
import rcon from './rcon';
import config from '../../config';

const discord = new Discord.Client();
let channel = null;

discord.on('ready', () => {
  channel = discord.channels.find('name', 'admin');
});

// Server -> Discord
rcon.on('message', (message) => {
  if (message.message.substring(0, 15) === '[Chat] [Global]') {
    const msg = message.message.substring(16);

    if (channel) {
      channel.send(msg);
    }
  }
});

// Discord -> Server
discord.on('message', (message) => {
  if (message.content[0] === '`' && message.content[message.content.length - 1] === '`' && message.content.length > 2) {
    rcon.run(`gl_chat_admin ${message.content.replace(/^\`+|\`+$/g, '')}`);
  }
});

discord.login(config.discord.token);

// rcon.on('message', (x) => console.log(x));

// import Discord from 'discord.js';

// const discord = new Discord.Client();

// discord.on('ready', () => {
//   const channel = discord.channels.find('name', 'admin');
//   // discord.channels.get(
//   channel.sendMessage('@everyone ADMIN MENTION');
//   console.log(channel);

// });
// discord.on('message', (msg) => {
//   if (msg.content === 'ping') {
//     msg.reply('uhh');
//   }
// });
// discord.login('Mzc5MTc3NDI2MzgwNjUyNTQ0.DOmQPQ.IOPMQ7DuwoRtuvdEN8-2YC7kANo');
