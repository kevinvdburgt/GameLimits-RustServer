import Bookshelf from '../../config/bookshelf';

export default Bookshelf.Model.extend({
  tableName: 'users',
  hasTimestamps: true,
});

// export default class User extends Bookshelf.Model {
//   get tableName() {
//     return 'users';
//   }

//   get hasTimestamps() {
//     return true;
//   }

//   * totalPoints () {
//     return this.fetch({ withRelated: 'points' }).then((user) => user);
//     return 'none, sorry';
//   }
// }

// import Bookshelf from '../../config/bookshelf';

// import Points from './points';

// export default Bookshelf.Model.extend({
//   tableName: 'users',

//   hasTimestamps: true,

//   points: function () {
//     return this.hasMany(Points);
//   },

//   totalPoints: function () {
//     return 'lulz';
//     // return this.fetch({ withRelated: 'points' }).then((user) => {
//     //   // console.log(user.related('points').toJSON());
//     //   return 123;
//     // });
//   }
// });
