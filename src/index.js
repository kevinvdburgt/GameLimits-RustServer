import express from 'express';
import morgan from 'morgan';
import path from 'path';
import routes from './routes';

const app = express();
const LISTEN_PORT = process.env.LISTEN_PORT || 3000;

// Set the templating engine to pug
app.set('view engine', 'pug');
app.set('views', path.join(__dirname, '..', 'resources', 'view'));

// Serve static files from the public folder
app.use(express.static(path.join(__dirname, '..', 'public')));

// Log all requests
app.use(morgan('tiny'));

// Bind the routes
app.use(routes);

// Handle 404 pages
app.use((req, res) => res.status(404).render('page/error/404'));

// Handle 500 pages
app.use((err, req, res, next) => res.status(500).render('page/error/500', {err}));

// Start listening
app.listen(LISTEN_PORT, () => console.log(`Listening on port ${LISTEN_PORT}`));
