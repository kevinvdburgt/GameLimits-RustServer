import passport from 'passport';
import SteamStrategy from 'passport-steam';
import database from '../database/database';
import auth from '../config/auth';

// Used to serialize the user for the session
passport.serializeUser((user, done) => {
  done(null, user.id);
});

// Used to deserialize the user for the session
passport.deserializeUser(async (id, done) => {
  const user = await database
    .table('users')
    .where('id', id)
    .first();

  if (!user) {
    return done('No user found');
  }

  return done(null, user);
});

passport.use(new SteamStrategy({
  returnURL: auth.steam.returnUrl,
  realm: auth.steam.realm,
  apiKey: auth.steam.apiKey,
}, (identifier, profile, done) => {
  process.nextTick(async () => {
    const user = await database
      .table('users')
      .where('steam_id', profile.id)
      .first();

    if (user) {
      return done(null, user);
    }

    const createdUser = await database
      .table('users')
      .insert({
        display_name: profile.displayName,
        steam_id: profile.id,
        avatar: profile.photos[2].value,
      });

      return done(null, user);


    // User
    //   .where({ steam_id: profile.id })
    //   .fetch()
    //   .then((user) => {
    //     // If the user is found, log them in
    //     if (user) {
    //       return done(null, user);
    //     }

    //     // The user has not been found, creating a new one
    //     User
    //       .forge({
    //         display_name: profile.displayName,
    //         steam_id: profile.id,
    //         avatar: profile.photos[2].value,
    //       })
    //       .save()
    //       .then((user) => {
    //         return done(null, user);
    //       })
    //       .catch((err) => {
    //         return done(err);
    //       });
    //   })
    //   .catch((err) => {
    //     return done(err);
    //   });
  });
}));
