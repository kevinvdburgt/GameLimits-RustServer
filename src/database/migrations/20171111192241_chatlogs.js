exports.up = (knex, Promise) => knex.schema.createTableIfNotExists('chatlogs', (table) => {
  table.increments().primary();
  table.integer('user_id').unsigned().references('users.id');
  table.string('display_name');
  table.string('message');
  table.timestamp('created_at').defaultTo(knex.fn.now());
});

exports.down = (knex, Promise) => knex.schema.dropTableIfExists('chatlogs');
