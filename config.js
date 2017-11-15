module.exports = {
  // Port on which to run the server
  port: 7777,

  // Host
  host: 'http://localhost:7777',

  // Authentication
  auth: {
    returnUrl: 'http://localhost:7777/login/return',
    realm: 'http://localhost:7777/',
    apiKey: '8C5E14C740EC1EA6212BA257466A76F7',
  },

  // Server RCON
  rcon: {
    host: '192.168.1.102',
    port: 28016,
    pass: 'changeme',
  },

  // Discord
  discord: {
    token: 'Mzc5MTc3NDI2MzgwNjUyNTQ0.DOmQPQ.IOPMQ7DuwoRtuvdEN8-2YC7kANo',
  },

  // Slack
  slack: {
    token: 'xoxb-4368424119-f5tqnFIhqFkOo6s2eb9K6xwZ',
    channel: 'glr',
  },

  // Database configuration
  database: {
    client: 'mysql',
    connection: {
      database: 'project_rust_gl',
      user: 'root',
      password: '',
      host: 'localhost',
      port: 3306,
      charset: 'utf8',
    },
    pool: {
      min: 2,
      max: 10,
    },
  },
};
