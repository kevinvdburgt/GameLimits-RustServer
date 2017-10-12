// Update with your config settings.

const migrations = {
  tableName: 'migrations',
  directory: __dirname + '/../database/migrations/',
};

module.exports = {
  production: {
    client: 'mysql',
    connection: {
      database: process.env.MYSQL_DATABASE || 'project_rust',
      user:     process.env.MYSQL_USER || 'root',
      password: process.env.MYSQL_PASSWORD || '',
      host:     process.env.MYSQL_HOST || 'localhost',
      port:     process.env.MYSQL_PORT || 3306,
    },
    pool: {
      min: 2,
      max: 10
    },
    migrations,
    debug: true,
  },
};
