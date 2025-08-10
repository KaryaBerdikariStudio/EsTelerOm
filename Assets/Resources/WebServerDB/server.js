// server.js
const { getLocalIp } = require('./services/netUtils');
const kill         = require('kill-port');
const express      = require('express');
const bodyParser   = require('body-parser');
const cors         = require('cors');

const { initializeDatabase } = require('./services/database');
const router       = require('./routes/router');    // <-- your routes/index.js
const { startUdpDiscovery } = require('./services/udpDiscovery'); // <-- your UDP discovery
const { publish } = require('./services/mqtt');
require('./services/mqtt');                         // <-- start your MQTT client

// 1) Ensure SQLite tables exist:
initializeDatabase();

const app = express();
app.use(cors());
app.use(bodyParser.json());

// 2) Mount your API router (inc. `router.get('/sse/clients', register)`)
app.use('/api', router);

// 3) If you prefer not to have clients call `/api/sse/clients` explicitly,
//    you can also mount the very same SSE “register” handler at root:


startUdpDiscovery(4211); // Start UDP discovery on port 4211

var testPayload = {
  tabel: 'Clientele',
  events: 'Test',
  konten: { test : "test", rgb: "211;175;55"  }
};
publish("test/esp32", testPayload);

const HTTP_PORT = 8000;
kill(HTTP_PORT, 'tcp')
  .finally(() => {
    app.listen(HTTP_PORT, () => {
      console.log(`🚀 HTTP server listening on http://localhost:${HTTP_PORT}`);
      console.log(`   • API root:     http://localhost:${HTTP_PORT}/api`);
    });
  });
