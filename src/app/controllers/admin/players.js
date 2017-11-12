import database from '../../../database/database';

const index = async (req, res) => {
  const users = await database
    .table('users')
    .map(async (user) => {
      const rp = await database
      .table('reward_points')
      .where('user_id', user.id)
      .sum('points as rp');
    
      user.rp = rp[0].rp;
      user.rp = user.rp === null ? 0 : user.rp;
      return user;
    });
    
  res.render('page/admin/players/index', { users });
};

export default { index };
