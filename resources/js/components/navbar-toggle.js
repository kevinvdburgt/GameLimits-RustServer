document.querySelector('.js-navbar-toggle').addEventListener('click', (event) => {
  const target = event.currentTarget.getAttribute('data-target');
  event.currentTarget.classList.toggle('is-active');
  document.querySelector(target).classList.toggle('is-active');
  event.preventDefault();
});
