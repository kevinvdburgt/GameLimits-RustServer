exports.up = (knex, Promise) => knex.schema.createTableIfNotExists('reward_points', (table) => {
  table.increments().primary();
  table.integer('user_id').unsigned().references('users.id');
  table.integer('points');
  table.string('description');
  table.timestamp('created_at').defaultTo(knex.fn.now());
});

exports.down = (knex, Promise) => knex.schema.dropTableIfExists('reward_points');
