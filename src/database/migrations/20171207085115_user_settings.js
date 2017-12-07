exports.up = (knex, Promise) => knex.schema.createTableIfNotExists('user_settings', (table) => {
  table.increments().primary();
  table.integer('user_id').unsigned().references('users.id');
  table.string('setting');
  table.string('data');
});

exports.down = (knex, Promise) => knex.schema.dropTableIfExists('user_settings');
