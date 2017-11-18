exports.up = (knex, Promise) => knex.schema.createTableIfNotExists('homes', (table) => {
  table.increments().primary();
  table.integer('user_id').unsigned().references('users.id');
  table.string('name');
  table.float('x');
  table.float('y');
  table.float('z');
});

exports.down = (knex, Promise) => knex.schema.dropTableIfExists('homes');
