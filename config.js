module.exports = {
  // Port on which to run the server
  port: 7777,

  // Authentication
  auth: {
    returnUrl: 'http://localhost:7777/login/return',
    realm: 'http://localhost:7777/',
    apiKey: '8C5E14C740EC1EA6212BA257466A76F7',
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
