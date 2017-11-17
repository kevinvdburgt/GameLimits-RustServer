exports.up = (knex, Promise) => knex.schema.createTableIfNotExists('shop_items', (table) => {
  table.increments().primary();
  table.string('key');
  table.integer('sort');
  table.string('name');
  table.string('description');
  table.string('image').nullable();
  table.string('command');
  table.string('category');
  table.integer('price');
  table.integer('type');
  table.integer('amount');
  table.timestamps();
});

exports.down = (knex, Promise) => knex.schema.dropTableIfExists('shop_items');
