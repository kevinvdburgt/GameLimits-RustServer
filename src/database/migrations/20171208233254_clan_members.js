exports.up = (knex, Promise) => knex.schema.createTableIfNotExists('clan_members', (table) => {
  table.increments().primary();
  table.integer('user_id').unsigned().references('users.id');
  table.string('role');
  table.timestamp('created_at').defaultTo(knex.fn.now());
});

exports.down = (knex, Promise) => knex.schema.dropTableIfExists('clan_members');
