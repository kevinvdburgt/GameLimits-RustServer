import config from '../../config';

export default {
  // Steam
  steam: {
    returnUrl: config.auth.returnUrl,
    realm: config.auth.realm,
    apiKey: config.auth.apiKey,
  },
};
