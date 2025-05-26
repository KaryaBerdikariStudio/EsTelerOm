// udpDiscovery.js
const dgram = require('dgram');
const os    = require('os');
const { encryptIntoEsp32 } = require('./cryptograph'); // your helper

/**
 * Start a UDP responder for “DISCOVER_SERVER” discovery.
 * @param {number} port – the UDP port to listen on (e.g. 4211)
 */
function startUdpDiscovery(port) {
  const server = dgram.createSocket('udp4');

  server.on('listening', () => {
    const { address, port } = server.address();
    console.log(`🚀 UDP discovery listening on ${address}:${port}`);
  });

  server.on('message', (msg, rinfo) => {
    if (msg.toString().trim() !== 'DISCOVER_SERVER') return;

    console.log(`🔍 Discovery request from ${rinfo.address}:${rinfo.port}`);

    // 1) Find our IPv4
    let localIp = '127.0.0.1';
    const ifaces = os.networkInterfaces();
    for (const name of Object.keys(ifaces)) {
      for (const iface of ifaces[name]) {
        if (iface.family === 'IPv4' && !iface.internal) {
          localIp = iface.address;
          break;
        }
      }
    }
    console.log(`📡 Local IP is ${localIp}`);

    // 2) Encrypt IP for ESP32
    let cipher;
    try {
      cipher = encryptIntoEsp32(localIp);
    } catch (e) {
      console.error('❌ Encryption failed:', e);
      return;
    }

    // 3) Send it back in base64‐decoded form
    console.log(`📦 Sending encrypted IP: ${cipher}`);
    server.send(cipher, rinfo.port, rinfo.address, (err) => {  // 🔥 Changed from buf to cipher
      if (err) console.error('❌ UDP send error:', err);
      else     console.log(`✅ Sent encrypted IP to ${rinfo.address}:${rinfo.port}`);
    });
  });

  server.bind(port);
}

module.exports = { startUdpDiscovery };
