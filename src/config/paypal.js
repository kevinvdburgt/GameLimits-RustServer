import paypal from 'paypal-rest-sdk';

paypal.configure({
  'mode': 'sandbox',
  'client_id': 'AcJjyBtaMc9luThXNH7bMd7dRR_O6-bp4Lr-pUgwGxa3OmfOb4LxfvvAhSOPcz15a7nTKsP8p6iGBjBT',
  'client_secret': 'EA9m9rSo9NKUAwWe2hLweXc6D_ZLHPChOsgnt5FUDYpIVjbhdJO2KCsGNeYkoEKDjfUr8w_8KbSScxXD'
});

export default paypal;
