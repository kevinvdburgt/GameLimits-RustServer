exports.up = (knex, Promise) => knex.schema.createTableIfNotExists('cooldowns', (table) => {
  table.increments().primary();
  table.integer('user_id').unsigned().references('users.id');
  table.string('name');
  table.timestamp('expires_at').defaultTo(knex.fn.now());
  table.timestamp('created_at').defaultTo(knex.fn.now());
});

exports.down = (knex, Promise) => knex.schema.dropTableIfExists('cooldowns');
