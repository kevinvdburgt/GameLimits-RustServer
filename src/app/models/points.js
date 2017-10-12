import Bookshelf from '../../config/bookshelf';

import User from './user';

export default Bookshelf.Model.extend({
  tableName: 'points',
  hasTimestamps: true,
  user: () => this.belongsTo(User),
});
