exports.up = (knex, Promise) => knex.schema.createTableIfNotExists('skills', (table) => {
  table.increments().primary();
  table.integer('user_id');
  table.string('skill');
  table.integer('level');
  table.integer('xp');
});

exports.down = (knex, Promise) => knex.schema.dropTableIfExists('skills');
