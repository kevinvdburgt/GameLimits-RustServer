import WebRcon from 'webrconjs';
import config from '../../config';

export const rcon = new WebRcon(config.rcon.host, config.rcon.port);
rcon.connect(config.rcon.pass);

export const exec = (message) => {
  if (rcon.status !== 'CONNECTED') {
    console.warn('Failed to execute rcon message, retying in 500ms..');
    setTimeout(() => exec(message), 500);
    try { rcon.connect(config.rcon.pass); } catch (e) {}
    return;
  }

  rcon.run(message);
};

export default { rcon, exec };
