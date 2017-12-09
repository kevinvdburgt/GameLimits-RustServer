/**
 * Telegram Command Handler
 */
import database from '../../database/database';

const start = (ctx) => {
  ctx.replyWithHTML(`Welcome to <i>Rust - Game Limits</i>.\n\nTo get setup your account, visit https://rust.gamelimits.com/ and login using your steam account. When you're loggedin on the website navigate to 'Notification Settings'\n\n(you can find that by hovering over your name).`)
};

const auth = async (ctx) => {
  const params = /([0-9]+):([0-9]+)/.exec(ctx.message.text);

  if (params == null) {
    ctx.replyWithHTML(`Please specify your authcode <code>/auth [token]</code>\n\nTo get setup your account, visit https://rust.gamelimits.com/ and login using your steam account. When you're loggedin on the website navigate to 'Notification Settings'\n\n(you can find that by hovering over your name).`);
    return;
  }

  // Find the user based on the given token
  const user = await database
    .table('users')
    .where({
      id: params[1],
      telegram_token: params[2],
      telegram: null,
    })
    .first();

  // Check if the authcode was found
  if (!user) {
    ctx.replyWithHTML(`Invalid auth code.\n\nTo get setup your account, visit https://rust.gamelimits.com/ and login using your steam account. When you're loggedin on the website navigate to 'Notification Settings'\n\n(you can find that by hovering over your name).`);
    return;
  }

  // Update the telegram api datas
  await database
    .table('users')
    .where({
      id: user.id,
    })
    .update({
      telegram: ctx.message.from.id,
      telegram_token: null,
    });
  
  // Sends a welcome message!
  ctx.reply(`Welcome ${user.display_name}!`);
  ctx.reply(`You will now receive raid alert notifications on your Telegram App once someone is trying to raid one of your bases.`);
};

export default { start, auth };
