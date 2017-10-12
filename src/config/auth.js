export default {
  // Steam
  steam: {
    returnUrl: process.env.STEAM_RETURN_URL || 'http://localhost:3000/login/return',
    realm: process.env.STEAM_REALM || 'http://localhost:3000/',
    apiKey: process.env.STEAM_API || '8C5E14C740EC1EA6212BA257466A76F7',
  },
};
