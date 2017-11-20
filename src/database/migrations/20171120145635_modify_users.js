exports.up = (knex, Promise) => knex.schema.table('users', (table) => {
  table.boolean('is_moderator')
    .after('is_admin')
    .defaultTo(false);

  table.boolean('has_joined_steamgroup')
  .after('is_admin')
  .defaultTo(false);
});

exports.down = (knex, Promise) => knex.schema.table('users', (table) => {
  table.dropColumn('is_moderator');
  table.dropColumn('joined_steamgroup');
});
