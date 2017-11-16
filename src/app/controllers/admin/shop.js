import database from '../../../database/database';

const index = async (req, res) => {
  const items = await database
    .table('shop_items');

  res.render('page/admin/shop/index', { items });
};

const show = async (req, res, next) => {
  const { id } = req.params;

  const item = await database
    .table('shop_items')
    .where('id', id)
    .first();

  if (!item) {
    return next();
  }

  res.render('page/admin/shop/show', {
    item
  });
};

export default { index, show };
