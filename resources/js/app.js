import '../sass/app.sass';
import { setImmediate } from 'timers';

// RP Calulcator
const input = document.querySelector('.js-rp-input');
const result = document.querySelector('.js-rp-result');
const button = document.querySelector('.js-rp-button');
const form = document.querySelector('.js-rp-form');
const isNumeric = (n) => !isNaN(parseFloat(n)) && isFinite(n);

if (input && result && button && form) {
  input.addEventListener('keyup', () => {
    if (isNumeric(input.value) && input.value > 0) {
      result.innerHTML = `${(input.value * 64)} RP`;
    } else {
      result.innerHTML = `0 RP`;
    }
  }, false);

  form.addEventListener('submit', (e) => {
    button.classList.add('is-loading');
  }, false);
}
