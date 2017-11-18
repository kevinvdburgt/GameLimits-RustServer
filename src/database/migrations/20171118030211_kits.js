exports.up = (knex, Promise) => knex.schema.createTableIfNotExists('kits', (table) => {
  table.increments().primary();
  table.string('key');
  table.integer('cooldown');
  table.string('command');
  table.timestamps();
});

exports.down = (knex, Promise) => knex.schema.dropTableIfExists('kits');
