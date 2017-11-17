exports.up = (knex, Promise) => knex.schema.createTableIfNotExists('shop_history', (table) => {
  table.increments().primary();
  table.integer('user_id').unsigned().references('users.id');
  table.integer('shop_item_id').unsigned().references('shop_items.id');
  table.timestamp('created_at').defaultTo(knex.fn.now());
});

exports.down = (knex, Promise) => knex.schema.dropTableIfExists('shop_history');
