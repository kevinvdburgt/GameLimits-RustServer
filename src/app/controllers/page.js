import database from '../../database/database';

const home = (req, res) => {
  res.render('page/home');
};

const rules = (req, res) => {
  res.render('page/rules');
};

const chatlog = async (req, res) => {
  const logs = await database
    .table('chatlogs')
    .orderBy('id', 'desc')
    .limit(500);

  res.render('page/chatlog', {
    logs
  });
};

const blog = (req, res) => {
  res.render('page/blog');
};

const mods = (req, res, next) => {
  const { mod } = req.params;

  const modPages = [
    'changelog',
    'info',
    'cars',
    'chat',
    'combat-raid-block',
    'friends',
    'kits',
    'playtime',
    'raid-alert',
    'remove-tool',
  ];

  if (!modPages.includes(mod)) {
    return next();
  }

  res.render(`page/mods/${mod}`, { mod });
};

export default { home, rules, chatlog, blog, mods };
