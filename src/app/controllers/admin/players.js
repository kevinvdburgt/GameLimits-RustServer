import User from '../../models/user';

const index = (req, res) => {
  User
    .query()
    .orderBy('id', 'desc')
    // .fetchPage({
    //   pageSize: 20,
    //   page: 0,
    // })
    .then((users) => {
      console.log(users);
      res.render('page/admin/players/index', {
        users
      });
    })
    .catch((err) => {
      throw err;
    });
};

export default { index };