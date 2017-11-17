exports.up = (knex, Promise) => knex.schema.createTableIfNotExists('users', (table) => {
  table.increments().primary();
  table.string('display_name');
  table.string('steam_id');
  table.string('avatar');
  table.boolean('is_admin').defaultTo(false);
  table.integer('playtime').defaultsTo(0);
  table.integer('afktime').defaultsTo(0);
  table.integer('rewardtime').defaultTo(0);
  table.timestamps(true, true);
});

exports.down = (knex, Promise) => knex.schema.dropTableIfExists('users');
