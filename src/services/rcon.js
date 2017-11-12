import WebRcon from 'webrconjs';
import config from '../../config';

const rcon = new WebRcon(config.rcon.host, config.rcon.port);
rcon.connect(config.rcon.pass);

export default rcon;

// rcon.on('message', (msg) => {
//   console.log('msg>>>', msg);
// });

// rcon.on('connect', () => {
//   console.log('connect>>>', );
// });

// rcon.on('disconnect', () => {
//   console.log('disconnect>>>', );
// });

// rcon.on('error', (err) => {
//   console.log('err>>>', err);
// });

// rcon.connect('changeme');

// console.log('x');
