// services/netUtils.js
const os = require('os');

/**
 * Returns the first non‑internal IPv4 address found on this machine,
 * or `127.0.0.1` if none are found.
 */
function getLocalIp() {
  const ifaces = os.networkInterfaces();
  for (const name of Object.keys(ifaces)) {
    for (const iface of ifaces[name]) {
      if (iface.family === 'IPv4' && !iface.internal) {
        return iface.address;
      }
    }
  }
  return '127.0.0.1';
}

module.exports = { getLocalIp };
