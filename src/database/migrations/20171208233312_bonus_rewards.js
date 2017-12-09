exports.up = (knex, Promise) => knex.schema.createTableIfNotExists('bonus_rewards', (table) => {
  table.increments().primary();
  table.integer('user_id').unsigned().references('users.id');
  table.string('code');
  table.string('message');
  table.string('command');
  table.boolean('is_redeemed').defaultTo(false);
  table.timestamp('created_at').defaultTo(knex.fn.now());
});

exports.down = (knex, Promise) => knex.schema.dropTableIfExists('bonus_rewards');
