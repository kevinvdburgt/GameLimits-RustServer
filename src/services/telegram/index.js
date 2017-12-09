/**
 * This service script keeps the telegram connection alive and serves some utils as well
 */
import Telegraf from 'telegraf';
import rcon from '../rcon';
import database from '../../database/database';
import config from '../../../config';
import commands from './commands';

// Create the bot itself
const bot = new Telegraf(config.telegram.token);

// Broadcast a message to all telegram-connected players
const broadcast = async (message, html) => {
  const users = await database
    .table('users')
    .whereNotNull('telegram');

  if (!users) {
    return;
  }

  users.forEach((user) => {
    bot.telegram.sendMessage(user.telegram, message);
  });
};

// Broadcast a message to all telegram-connected players which are admins
const broadcastAdmin = async (message) => {
  const users = await database
    .table('users')
    .whereNotNull('telegram')
    .where({
      is_admin: true,
    });

  if (!users) {
    return;
  }

  users.forEach((user) => {
    bot.telegram.sendMessage(user.telegram, message);
  });
};

// Send a message to a specific user
const send = async (steamOrUserId, message) => {
  const user = await database
    .table('users')
    .whereNotNull('telegram')
    .where({
      id: steamOrUserId,
    })
    .orWhere({
      steam_id: steamOrUserId,
    })
    .first();
  
  if (!user) {
    return false;
  }

  bot.telegram.sendMessage(user.telegram, message);
  return true;
};

// Command: /start
bot.start((ctx) => commands.start(ctx));
bot.command('/auth', (ctx) => commands.auth(ctx));

// Start the bot polling
bot.startPolling();

export default { bot, broadcast, broadcastAdmin, send };

// // Create the bot
// const bot = new Telegraf(config.telegram.token);

// // Display the start page
// bot.start((ctx) => {
//   ctx.reply(`Welcome to Game Limits Rust Bot.\n\n` +
//     `To get started login in your steam account at https://rust.gamelimits.com/ and open the notification settings.`);

//   ctx.reply(`Then autorize your account by using the command /auth <your token>`);

//   ctx.reply(`Use the command /help for a list of commands of our bot.`);
// });

// // Display the help page
// bot.command('/help', (ctx) => {
//   ctx.reply(`Welcome to the Game Limits Rust Bot.`);
// });

// // Start the authorizing process
// bot.command('/auth', async (ctx) => {
//   const params = /([0-9]+):([0-9]+)/.exec(ctx.message.text);

//   if (params == null)
//   {
//     ctx.reply(`Please specify your authcode /auth <token>\n\nLogin in your steam account at https://rust.gamelimits.com/ and open the notification settings.`);
//     return;
//   }

//   // Find the user based on the given params
//   const user = await database
//     .table('users')
//     .where({
//       id: params[1],
//       telegram_token: params[2],
//       telegram: null,
//     })
//     .first();

//   if (!user) {
//     ctx.reply(`Invalid auth code.\n\nLogin in your steam account at https://rust.gamelimits.com/ and open the notification settings.`)
//     return;
//   }

//   await database
//     .table('users')
//     .where({
//       id: user.id,
//     })
//     .update({
//       telegram: ctx.message.from.id,
//       telegram_token: null,
//     });
  
//   ctx.replyWithHTML(`Welcome <strong>${user.display_name}</strong>!\n\nYou will now receive important notifications from our server, such as raid alerts in your cupboard range!`);
// });

// bot.startPolling();

// const broadcast = async (message) => {
//   const users = await database
//     .table('users')
//     .whereNotNull('telegram');

//   users.forEach((user) => bot.telegram.sendMessage(user.telegram, message));
// };

// export default { bot, broadcast };



// // import TelegramBot from 'node-telegram-bot-api';
// // import database from '../database/database';

// // const token = '479848213:AAELJ_NiURg909nBT8OdK1CntokYkXIf2hY';

// // const bot = new TelegramBot(token, { polling: true });

// // bot.onText(/\/auth ([0-9]+):([0-9]+)/, async (msg, match) => {
// //   const userid = match[1];
// //   const token = match[2];

// //   // Find the authorization code
// //   const user = await database
// //     .table('users')
// //     .where({
// //       id: userid,
// //       telegram_token: token,
// //       telegram: null,
// //     })
// //     .first();

// //   if (user) {
// //     await database
// //       .table('users')
// //       .update({
// //         telegram: msg.from.id,
// //         telegram_token: null,
// //       })
// //       .where({
// //         id: user.id
// //       });
    
// //       bot.sendMessage(msg.from.id, `You have been authorized as ${user.display_name}`);
// //   } else {
// //     bot.sendMessage(msg.from.id, 'Invalid auth code');
// //   }
// // });
