const home = (req, res) => {
  res.render('page/home');
};

const rules = (req, res) => {
  res.render('page/rules');
};

export default { home, rules };