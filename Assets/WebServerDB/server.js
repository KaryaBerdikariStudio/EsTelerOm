// server.js (excerpt)
const kill                                      = require('kill-port');
const express                                   = require('express');
const bodyParser                                = require('body-parser');
const cors                                      = require('cors');
const { initializeDatabase }                    = require('./database');
const router                                    = require('./router');
const { startUdpDiscovery }                     = require('./udpDiscovery');
const {encyptIntoEsp32, encryptIntoDB, decrypt} = require('./cryptograph');




// 1) Initialize DB
initializeDatabase();

const app = express();
app.use(cors());
app.use(bodyParser.json());
app.use('/api', router);

const HTTP_PORT = 8000;
// Attempt to free up the port before listening
kill(HTTP_PORT, 'tcp')
  .then(() => {
    console.log(`🗑️ Freed port ${HTTP_PORT}, starting server…`);
  })
  .catch(() => {
    // It may already be free—that’s fine
  })
  .finally(() => {
    app.listen(HTTP_PORT, () => {
      console.log(`🚀 HTTP server listening on http://localhost:${HTTP_PORT}`);
    });
    // Start your UDP discovery as before
    startUdpDiscovery(4211);
  });