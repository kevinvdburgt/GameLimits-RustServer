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

export default { home, rules, chatlog, blog };
