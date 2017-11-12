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

const show = async (req, res, next) => {
  const { id } = req.params;

  const user = await database
    .table('users')
    .where('id', id)
    .first();

  if (!user) {
    return next();
  }

  res.render('page/admin/players/show', {
    user
  });
};

export default { index, show };
