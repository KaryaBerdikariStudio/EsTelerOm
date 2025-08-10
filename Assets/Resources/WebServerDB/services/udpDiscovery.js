const dgram = require('dgram');
const os = require('os');

function getLocalIPAddress() {
  const interfaces = os.networkInterfaces();
  for (const name in interfaces) {
    for (const iface of interfaces[name]) {
      if (!iface.internal && iface.family === 'IPv4') {
        return iface.address;
      }
    }
  }
  return '127.0.0.1'; // fallback
}

function startUdpDiscovery(port = 4211) {
  const server = dgram.createSocket('udp4');

  server.on('listening', () => {
    const address = server.address();
    console.log(`🚀 UDP discovery listening on ${address.address}:${address.port}`);
  });

  server.on('message', (msg, rinfo) => {
    const message = msg.toString();
    console.log(`📨 UDP message received: ${message} from ${rinfo.address}:${rinfo.port}`);

    if (message === 'DISCOVER_SERVER') {
      const ip = getLocalIPAddress(); // Reply with local IP
      const reply = Buffer.from(ip);
      server.send(reply, 0, reply.length, rinfo.port, rinfo.address, (err) => {
        if (err) {
          console.error('❌ UDP reply error:', err);
        } else {
          console.log(`✅ Sent discovery reply to ${rinfo.address}:${rinfo.port} -> ${ip}`);
        }
      });
    }
  });

  server.bind(port);
}

module.exports = { startUdpDiscovery };
