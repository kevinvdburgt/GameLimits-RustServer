const config = require('../../config');

const knex = config.database;

knex.migrations = {
  tableName: 'migrations',
  directory: `${__dirname}/../database/migrations/`,
};

module.exports = knex;
