exports.up = (knex, Promise) => knex.schema.createTableIfNotExists('friends', (table) => {
  table.increments().primary();
  table.integer('user_id').unsigned().references('users.id');
  table.integer('with_user_id').unsigned().references('users.id');
  table.timestamp('created_at').defaultTo(knex.fn.now());
});

exports.down = (knex, Promise) => knex.schema.dropTableIfExists('friends');
