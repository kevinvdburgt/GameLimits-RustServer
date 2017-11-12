import { Router } from 'express';
import passport from 'passport';

import page from './controllers/page';
import auth from './controllers/auth';
import donate from './controllers/donate';

import adminDashboard from './controllers/admin/dashboard';
import adminPlayers from './controllers/admin/players';

const router = Router();

// // Helper functions
const isLoggedIn = (req, res, next) => {
  if (req.isAuthenticated()) {
    return next();
  }
  return res.status(403).render('page/error/403');
};

const isAdmin = (req, res, next) => {
  if (req.isAuthenticated() && req.user.is_admin == true) {
    return next();
  }
  return res.status(403).render('page/error/403');
};

// Pages
router.get('/', page.home);
router.get('/rules', page.rules);
router.get('/chatlog', page.chatlog);

router.get('/donate', donate.show);
router.post('/donate', donate.post);
router.get('/donate/return', donate.validate);

// Auth
router.get('/login', passport.authenticate('steam', { failureRedirect: '/' }), auth.login);
router.get('/login/return', passport.authenticate('steam', { failureRedirect: '/?failed=true' }), auth.loginValidate);
router.get('/logout', isLoggedIn, auth.logout);

// Admin
router.get('/admin', isAdmin, adminDashboard.index);
router.get('/admin/players', isAdmin, adminPlayers.index);

export default router;
