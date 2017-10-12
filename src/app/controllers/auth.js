const login = (req, res) => {
  res.redirect('/');
};

const logout = (req, res) => {
  req.logout();
  res.redirect('/');
};

const loginValidate = (req, res) => {
  res.redirect('/');
};

export default { login, logout, loginValidate };