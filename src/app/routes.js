import { Router } from 'express';
import passport from 'passport';

import page from './controllers/page';
import auth from './controllers/auth';
import donate from './controllers/donate';

import profile from './controllers/profile';

import adminDashboard from './controllers/admin/dashboard';
import adminPlayers from './controllers/admin/players';
import adminShop from './controllers/admin/shop';

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

// Redirects
router.get('/discord', (req, res) => res.redirect(302, 'https://discord.gg/se7U5BR'));
router.get('/steam', (req, res) => res.redirect(302, 'http://steamcommunity.com/groups/rust-gamelimits'));
router.get('/mods', (req, res) => res.redirect(302, 'https://rust.gamelimits.com/mods/changelog'));

// Pages
router.get('/', page.home);
router.get('/rules', page.rules);
router.get('/chatlog', page.chatlog);
router.get('/blog', page.blog);
router.get('/mods/:mod', page.mods);

router.get('/donate', donate.show);
router.post('/donate', donate.post);
router.get('/donate/return', donate.validate);

// Auth
router.get('/login', passport.authenticate('steam', { failureRedirect: '/' }), auth.login);
router.get('/login/return', passport.authenticate('steam', { failureRedirect: '/?failed=true' }), auth.loginValidate);
router.get('/logout', isLoggedIn, auth.logout);

// Profile
router.get('/profile/notifications', isLoggedIn, profile.notifications);

// Admin
router.get('/admin', isAdmin, adminDashboard.index);
router.get('/admin/players', isAdmin, adminPlayers.index);
router.get('/admin/players/:id', isAdmin, adminPlayers.show);
router.get('/admin/shop', isAdmin, adminShop.index);
router.get('/admin/shop/:id', isAdmin, adminShop.show);

export default router;
