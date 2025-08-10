// server.js

// 0) Crash‐dump handlers: MUST be first
process.on('uncaughtException', err => {
  console.error('❌ Uncaught Exception:', err.stack || err);
});
process.on('unhandledRejection', (reason, promise) => {
  console.error('❌ Unhandled Rejection at:', promise, 'reason:', reason);
});

const { getLocalIp } = require('./services/netUtils');
const kill            = require('kill-port');
const express         = require('express');
const bodyParser      = require('body-parser');
const cors            = require('cors');

const { initializeDatabase } = require('./services/database');
const router                = require('./routes/router');
const { startUdpDiscovery } = require('./services/udpDiscovery');
require('./services/mqtt'); // starts MQTT client

// 1) Ensure SQLite tables exist:
initializeDatabase();

const app = express();
app.use(cors());
app.use(bodyParser.json());

// 2) Mount your API router under /api
app.use('/api', router);

// —–– Add a quick /api/test here if your router doesn’t already log it —––
app.get('/api/test', (req, res) => {
  console.log('📥 [GET] /api/test');
  res.status(200).json({ tbl: 'Test', events: 'Ping', konten: 'Pong' });
});

const HTTP_PORT = 8000;
const HOST_IP   = getLocalIp(); // e.g. "192.168.1.42"

kill(HTTP_PORT, 'tcp').finally(() => {
  kill(4211, 'udp').finally(() => {
    // Now it’s safe to bind both HTTP and UDP exactly once:
    app.listen(HTTP_PORT, () => {
      console.log(`🚀 HTTP server listening on http://${HOST_IP}:${HTTP_PORT}`);
      console.log(`   • API root:     http://${HOST_IP}:${HTTP_PORT}/api`);
    });
    startUdpDiscovery(4211);
  });
});