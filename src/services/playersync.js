import request from 'request';
import config from '../../config';
import database from '../database/database';

const sync = async () => {
  const players = await database
    .table('users');

  players.forEach((player) => {
    request(`http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key=${config.auth.apiKey}&steamids=${player.steam_id}`, async (err, res, body) => {
      if (err || res.statusCode != 200) {
        console.error(err);
        return;
      }

      const data = JSON.parse(body);

      if (player.avatar !== data.response.players[0].avatarfull || player.display_name !== data.response.players[0].personaname) {
        await database
          .table('users')
          .where('id', player.id)
          .update({
            avatar: data.response.players[0].avatarfull,
            display_name: data.response.players[0].personaname,
          });
      }
    });
    
  });
};

setInterval(() => sync(), 1000 * 60 * 30);
