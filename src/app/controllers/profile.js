import database from '../../database/database';
import { random } from '../../utils';

const notifications = async (req, res) => {
  let token = null;

  if (req.user.telegram == null || (req.query.deauthorize && req.query.deauthorize === 'telegram')) {
    token = random('0123456789', 6);
    
    await database
      .table('users')
      .update({
        telegram_token: token,
        telegram: null,
      })
      .where({
        id: res.locals._user.id,
      });
  }

  res.render('page/notifications', { token });
};

export default { notifications };
