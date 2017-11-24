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
    .orderBy('id', 'desc');

  res.render('page/chatlog', {
    logs
  });
};

const blog = (req, res) => {
  res.render('page/blog');
};

const mods = (req, res) => {
  res.render('page/mods');
};

export default { home, rules, chatlog, blog, mods };
