/**
 * This script updates the player information in the database, such as the displayname and avatar url
 */
import request from 'request';
import utf8 from 'utf8';
import database from '../database/database';
import config from '../../config';

const sync = async () => {
  const playerRecords = await database.table('users');

  playerRecords.forEach((player) => {
    // Fetch the public user data from the Steam API
    request(`http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key=${config.auth.apiKey}&steamids=${player.steam_id}`, async (err, res, body) => {
      if (err || res.statusCode != 200) {
        console.error(err);
        return;
      }

      const data = JSON.parse(body);

      if (player.avatar !== data.response.players[0].avatarfull || utf8.encode(player.display_name) !== utf8(data.response.players[0].personname)) {
        await database
          .table('users')
          .where('id', player.id)
          .update({
            avatar: data.response.players[0].avatarfull,
            display_name: utf8.encode(data.response.players[0].personname),
          });
        
          console.log(`Updated user data [${player.id}:${player.steam_id}]`);
      }
    });
  });
};

// Run this script on startup
sync();

// Run this script every hour
setInterval(() => sync(), 1000 * 3600);
