import TelegramBot from 'node-telegram-bot-api';
import database from '../database/database';

const token = '479848213:AAELJ_NiURg909nBT8OdK1CntokYkXIf2hY';

const bot = new TelegramBot(token, { polling: true });

bot.onText(/\/auth ([0-9]+):([0-9]+)/, async (msg, match) => {
  const userid = match[1];
  const token = match[2];

  const user = await database
    .table('users')
    .where({
      id: userid,
      telegram_token: token,
      telegram: null,
    })
    .first();

  if (user) {
    await database
      .table('users')
      .update({
        telegram: msg.from.id,
        telegram_token: null,
      })
      .where({
        id: user.id
      });
    
      bot.sendMessage(msg.from.id, `You have been authorized as ${user.display_name}`);
  } else {
    bot.sendMessage(msg.from.id, 'Invalid auth code');
  }
});
