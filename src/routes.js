import { Router } from 'express';

const router = Router();

router.get('/', (req, res) => res.render('page/home'));

export default router;
