exports.up = (knex, Promise) => knex.schema.table('users', (table) => {
  table.string('telegram')
    .after('rewardtime')
    .nullable()
    .defaultTo(null);

  table.string('telegram_token')
    .after('rewardtime')
    .nullable()
    .defaultTo(null);

  table.string('email')
    .after('rewardtime')
    .nullable()
    .defaultTo(null);

  table.string('email_token')
    .after('rewardtime')
    .nullable()
    .defaultTo(null);
});

exports.down = (knex, Promise) => knex.schema.table('users', (table) => {
  table.dropColumn('telegram');
  table.dropColumn('telegram_token');
  table.dropColumn('email');
  table.dropColumn('email_token');
});
