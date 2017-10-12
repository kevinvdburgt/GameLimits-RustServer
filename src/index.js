import express from 'express';
import session from 'express-session';
import path from 'path';
import morgan from 'morgan';
import passport from 'passport';
import knex from 'knex';
import SessionFileStore from 'session-file-store';
import routes from './app/routes';
import knexfile from './config/knexfile';
import './config/passport';

const listenPort = process.env.PORT || 3000;
const app = express();
const sessionFileStore = SessionFileStore(session);

// Migrate the database on startup
knex(knexfile.production).migrate.latest();

// Setup the sessions
app.use(session({
  store: new sessionFileStore({
    path: path.join(__dirname, '..', 'data', 'sessions'),
  }),
  secret: 'keyboard cat',
  resave: false,
  saveUninitialized: true,
  // cookie: { secure: true },
}));

// Setup passport
app.use(passport.initialize());
app.use(passport.session());

// Setup serving static files
app.use(express.static(path.join(__dirname, '..', 'public')));

// Setup pug for templating
app.set('view engine', 'pug');
app.set('views', path.join(__dirname, '..', 'resources', 'view'));

// Setup the morgan logger
app.use(morgan('tiny'));

// Add locals
app.use((req, res, next) => {
  res.locals = {
    user: req.user,
  };
  next();
});

// Add the app routes
app.use(routes);

// Process 404 pages
app.use((req, res) => res.status(404).render('page/error/404'));

// Process 500 pages
app.use((err, req, res, next) => res.status(500).render('page/error/500', {err}));

// Start listening
app.listen(listenPort, () => {
  console.log(`Started listening on port ${listenPort}`);
});
