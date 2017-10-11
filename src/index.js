import express from 'express';
import session from 'express-session';
import morgan from 'morgan';
import path from 'path';
import passport from 'passport';
import routes from './routes';
import SteamStrategy from 'passport-steam';

const app = express();
const LISTEN_PORT = process.env.LISTEN_PORT || 3000;

passport.serializeUser(function(user, done) {
  done(null, user);
});

passport.deserializeUser(function(obj, done) {
  done(null, obj);
});

// Setup passport
passport.use(new SteamStrategy({
  returnURL: 'http://localhost:3000/login/return',
  realm: 'http://localhost:3000/',
  apiKey: '8C5E14C740EC1EA6212BA257466A76F7',
}, (identifier, profile, done) => {
  process.nextTick(() => {
    profile.identifier = identifier;
    return done(null, profile);
  });
}));

// Use sessions
app.use(session({ secret: 'asdasdhgksdjf' }));

// Use the passport
app.use(passport.initialize());
app.use(passport.session());

// Set the templating engine to pug
app.set('view engine', 'pug');
app.set('views', path.join(__dirname, '..', 'resources', 'view'));

// Serve static files from the public folder
app.use(express.static(path.join(__dirname, '..', 'public')));

// Log all requests
app.use(morgan('tiny'));

// Add locals
app.use((req, res, next) => {
  res.locals = {
    user: req.user,
  };
  next();
});

// Bind the routes
app.use(routes);

// Handle 404 pages
app.use((req, res) => res.status(404).render('page/error/404'));

// Handle 500 pages
app.use((err, req, res, next) => res.status(500).render('page/error/500', {err}));

// Start listening
app.listen(LISTEN_PORT, () => console.log(`Listening on port ${LISTEN_PORT}`));
