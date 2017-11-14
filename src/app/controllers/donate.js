import paypal from '../../config/paypal';
import database from '../../database/database';
import config from '../../../config';

const show = async (req, res) => {
  res.render('page/donate/show');
};

const post = async (req, res) => {
  const eur = parseInt(req.body.amount);
  const rp = parseInt(eur * 64);

  if (eur < 1 || rp < 1 || isNaN(eur) || isNaN(rp)) {
    return res.render('page/donate/show');
  }

  var paymentInformation = {
    intent: 'sale',
    payer: {
      payment_method: 'paypal',
    },
    redirect_urls: {
      return_url: `${config.host}/donate/return`,
      cancel_url: `${config.host}/donate/cancel`,
    },
    transactions: [{
      item_list: {
        items: [{
          name: 'Reward Points',
          sku: `rp-${rp}`,
          price: `${eur}.00`,
          currency: 'EUR',
          quantity: 1,
        }],
      },
      amount: {
        currency: 'EUR',
        total: `${eur}.00`,
      },
      description: `${rp} Reward Points for Game Limits Rust server.`,
    }],
  };

  paypal.payment.create(paymentInformation, async (err, payment) => {
    if (err) {
      console.error(`${eur}`);
      console.log(err.response.details);
      res.status(500).render('page/error/500', {err: err.response.details});
      return;
    }

    await database
      .table('payments')
      .insert({
        user_id: req.user.id,
        paypal_id: payment.id,
        price: eur,
        points: rp,
        state: payment.state,
      });

    res.redirect(payment.links[1].href);
  });
};

const validate = async (req, res, next) => {
  const payerId = req.query.PayerID;
  const paymentId = req.query.paymentId;

  // Find the transaction in the database
  const paymentData = await database
    .table('payments')
    .where('paypal_id', paymentId)
    .where('state', 'created')
    .first();

  if (!paymentData) {
    return next();
  }

  // Validate the transaction
  const executeRequest = {
    payer_id: payerId,
  };

  // Execute the payment
  paypal.payment.execute(paymentId, executeRequest, async (err, payment) => {
    if (err) {
      console.error(err);
      return res.status(500).render('page/error/500', { err });
    }

    // Update the transaction state
    await database
      .table('payments')
      .where('id', paymentData.id)
      .update({
        state: payment.state,
      });
    
    // If the state is approved, add the Reward Points to the player's account
    if (payment.state === 'approved') {
      await database
        .table('reward_points')
        .insert({
          user_id: paymentData.user_id,
          points: paymentData.points,
          description: 'Replenished using a donation',
        });

      return res.render('page/donate/process', {
        success: true,
        points: paymentData.points,
      });
    }

    res.render('page/donate/process', {
      success: false,
    });
  });
};

export default { show, post, validate };
