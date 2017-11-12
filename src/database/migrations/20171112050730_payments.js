exports.up = (knex, Promise) => knex.schema.createTableIfNotExists('payments', (table) => {
  table.increments().primary();
  table.integer('user_id').unsigned().references('users.id');
  table.string('paypal_id');
  table.string('paypal_approval').nullable();
  table.integer('price');
  table.integer('points');
  table.string('state');
  table.timestamp('created_at').defaultTo(knex.fn.now());
});

exports.down = (knex, Promise) => knex.schema.dropTableIfExists('payments');
