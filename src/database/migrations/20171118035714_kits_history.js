exports.up = (knex, Promise) => knex.schema.createTableIfNotExists('kits_history', (table) => {
  table.increments().primary();
  table.integer('user_id').unsigned().references('users.id');
  table.integer('kit_id').unsigned().references('kits.id');
  table.timestamp('created_at').defaultTo(knex.fn.now());
});

exports.down = (knex, Promise) => knex.schema.dropTableIfExists('kits_history');
