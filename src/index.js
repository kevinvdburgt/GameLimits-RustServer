import 'babel-polyfill';
import express from 'express';
import session from 'express-session';
import path from 'path';
import passport from 'passport';
import bodyParser from 'body-parser';
import SessionFileStore from 'session-file-store';
import moment from 'moment';
import database from './database/database';
import config from '../config';
import routes from './app/routes';
import './config/passport';
import './services/playersync';

const app = express();
const sessionFileStore = SessionFileStore(session);

// Migrate the database when needed to it latest migration.
database.migrate.latest();

// Setup the sessions
app.use(session({
  store: new sessionFileStore({
    path: path.join(__dirname, '..', 'data', 'sessions'),
  }),
  secret: 'keyboard cat secret rust',
  resave: false,
  saveUninitialized: true,
  // cookie: { secure: true },
}));

// Setup passport
app.use(passport.initialize());
app.use(passport.session());

// Setup serving static files
app.use(express.static(path.join(__dirname, '..', 'public')));

// Setup pug as the templating engine
app.set('view engine', 'pug');
app.set('views', path.join(__dirname, '..', 'resources', 'view'));

// parse application/x-www-form-urlencoded
app.use(bodyParser.urlencoded({ extended: false }))

// parse application/json
app.use(bodyParser.json());

// Add locals
app.use((req, res, next) => {
  res.locals = {
    _user: req.user,
    moment,
  };
  next();
});

// Load the application routes
app.use(routes);

// Process 404 pages
app.use((req, res) => res.status(404).render('page/error/404'));

// Process 500 pages
app.use((err, req, res, next) => res.status(500).render('page/error/500', {err}));

// Start listening
app.listen(config.port, () => {
  console.log(`Started listening on port ${config.port}`);
});


// import 'babel-polyfill';
// import express from 'express';
// import session from 'express-session';
// import path from 'path';
// import morgan from 'morgan';
// import passport from 'passport';
// import SessionFileStore from 'session-file-store';
// import routes from './app/routes';
// import database from './database/database';
// import './config/passport';

// const listenPort = process.env.LISTEN_PORT || 3000;
// const app = express();
// const sessionFileStore = SessionFileStore(session);

// // Migrate the database on startup
// database.migrate.latest();

// // Setup the sessions
// app.use(session({
//   store: new sessionFileStore({
//     path: path.join(__dirname, '..', 'data', 'sessions'),
//   }),
//   secret: 'keyboard cat',
//   resave: false,
//   saveUninitialized: true,
//   // cookie: { secure: true },
// }));

// // Setup passport
// app.use(passport.initialize());
// app.use(passport.session());

// // Setup serving static files
// app.use(express.static(path.join(__dirname, '..', 'public')));

// // Setup pug for templating
// app.set('view engine', 'pug');
// app.set('views', path.join(__dirname, '..', 'resources', 'view'));

// // Setup the morgan logger
// app.use(morgan('tiny'));

// // Add locals
// app.use((req, res, next) => {
//   res.locals = {
//     user: req.user,
//   };
//   next();
// });

// // Add the app routes
// app.use(routes);

// // Process 404 pages
// app.use((req, res) => res.status(404).render('page/error/404'));

// // Process 500 pages
// app.use((err, req, res, next) => res.status(500).render('page/error/500', {err}));

// // Start listening
// app.listen(listenPort, () => {
//   console.log(`Started listening on port ${listenPort}`);
// });
