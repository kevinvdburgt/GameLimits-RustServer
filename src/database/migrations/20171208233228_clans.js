exports.up = (knex, Promise) => knex.schema.createTableIfNotExists('clans', (table) => {
  table.increments().primary();
  table.string('name');
  table.timestamp('created_at').defaultTo(knex.fn.now());
});

exports.down = (knex, Promise) => knex.schema.dropTableIfExists('clans');
