import { Router } from 'express';
import passport from 'passport';

const router = Router();

router.get('/', (req, res) => res.render('page/home'));

router.get('/login', passport.authenticate('steam', { failureRedirect: '/?failed=true' }), (req, res) => {
  return res.redirect('/');
});

router.get('/login/return', passport.authenticate('steam', { failureRedirect: '/?failed=true' }), (req, res) => {
  return res.redirect('/');
});

router.get('/logout', (req, res) => {
  req.logout();
  return res.redirect('/');
});

export default router;
