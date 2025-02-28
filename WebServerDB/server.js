const express = require("express");
const cors = require("cors");
const bodyParser = require("body-parser");
const sqlite3 = require("sqlite3").verbose();
const dgram = require("dgram"); // For UDP broadcast
const path = require("path");
const { db, initializeDatabase } = require("./database"); // Import DB module

const app = express();
const PORT = 8000;

app.use(cors());
app.use(bodyParser.json());

// Initialize SQLite database and tables
initializeDatabase();

const os = require('os');

// ----------------------------
// UDP Broadcast Discovery
// ----------------------------
function getLocalIPAddress() {
  const interfaces = os.networkInterfaces();
  for (const iface in interfaces) {
    for (const alias of interfaces[iface]) {
      if (alias.family === 'IPv4' && !alias.internal) {
        return alias.address;
      }
    }
  }
  return '127.0.0.1'; // Fallback
}


// ----------------------------
// UDP Broadcast Discovery
// ----------------------------
const UDP_PORT = 4211;  // Port on which this backend listens for discovery requests
const udpServer = dgram.createSocket("udp4");

udpServer.on("listening", () => {
  const address = udpServer.address();
  console.log(`UDP Server listening on ${address.address}:${address.port}`);
});

udpServer.on("message", (msg, rinfo) => {
  console.log(`UDP message from ${rinfo.address}:${rinfo.port} - ${msg}`);
  if (msg.toString().trim() === "DISCOVER_SERVER") {
    // Get the local IP address dynamically.
    const localIP = getLocalIPAddress();
    const response = Buffer.from(localIP);
    udpServer.send(response, 0, response.length, rinfo.port, rinfo.address, (err) => {
      if (err) console.error("Error sending UDP response:", err);
      else console.log(`Sent UDP response (${localIP}) to ${rinfo.address}:${rinfo.port}`);
    });
  }
});


udpServer.bind(UDP_PORT);

// ----------------------------
// Express Endpoints
// ----------------------------

// Serve index.html at the root
app.get("/", (req, res) => {
  res.sendFile(path.join(__dirname, "index.html"));
});

// Endpoint: Clear InputLog
app.get("/clear-log", (req, res) => {
  db.run("DELETE FROM InputLog", [], function (err) {
    if (err) return res.status(500).json({ error: err.message });
    res.json({ message: "✅ Input log cleared successfully!" });
  });
});

// Endpoint: Log an RFID input (with IP address)
// Example: GET /input-log/192.168.1.100/ABC123
app.get("/input-log/:ip/:uid", (req, res) => {
  const { ip, uid } = req.params;
  const timestamp = new Date().toISOString().slice(0, 19).replace("T", " ");
  db.run("INSERT INTO InputLog (ip_address, uid, timestamp) VALUES (?, ?, ?)", [ip, uid, timestamp], function (err) {
    if (err) return res.status(500).json({ error: err.message });
    res.json({ message: "Input recorded", UID: uid, IP: ip, timestamp });
  });
});

// Endpoint: Retrieve all input logs
app.get("/input-log", (req, res) => {
  db.all("SELECT uid, ip_address FROM InputLog ORDER BY timestamp DESC", [], (err, rows) => {
    if (err) return res.status(500).json({ error: err.message });
    res.json({ input_log: rows });
  });
});

// Helper function: Add new RFID card mapping (incremental letter assignment)
function addRFID(uid, res) {
  db.get("SELECT huruf FROM RFID_Card ORDER BY ROWID DESC LIMIT 1", [], (err, lastRow) => {
    if (err) return res.status(500).json({ error: err.message });
    let nextHuruf = "A";
    if (lastRow) {
      nextHuruf = lastRow.huruf === "Z" ? "A" : String.fromCharCode(lastRow.huruf.charCodeAt(0) + 1);
    }
    db.run("INSERT INTO RFID_Card (uid, huruf) VALUES (?, ?)", [uid, nextHuruf], function (err) {
      if (err) return res.status(500).json({ error: err.message });
      res.json({ message: "RFID card added successfully!", uid, huruf: nextHuruf });
    });
  });
}

// Endpoint: Get RFID card by UID (or add if not exists)
app.get("/rfid/:uid", (req, res) => {
  const { uid } = req.params;
  db.get("SELECT * FROM RFID_Card WHERE uid = ?", [uid], (err, row) => {
    if (err) return res.status(500).json({ error: err.message });
    if (row) {
      res.json(row);
    } else {
      addRFID(uid, res);
    }
  });
});

// Endpoint: Retrieve all RFID cards
app.get("/rfid-cards", (req, res) => {
  db.all("SELECT * FROM RFID_Card", [], (err, rows) => {
    if (err) return res.status(500).json({ error: err.message });
    res.json({ rfid_cards: rows });
  });
});

// Endpoint: Get all words from Kamus
app.get("/words", (req, res) => {
  db.all("SELECT * FROM Kamus", [], (err, rows) => {
    if (err) return res.status(500).json({ error: err.message });
    res.json({ words: rows });
  });
});

// Endpoint: Add a new score
app.post("/scores/add", (req, res) => {
  const { name, score } = req.body;
  if (!name || score == null) return res.status(400).json({ error: "Invalid data" });
  const dateNow = new Date().toISOString();
  db.run("INSERT INTO Scores (name, score, date) VALUES (?, ?, ?)", [name, score, dateNow], function (err) {
    if (err) return res.status(500).json({ error: err.message });
    res.json({ message: "Score added successfully", id: this.lastID, name, score, date: dateNow });
  });
});

// Endpoint: Retrieve all scores
app.get("/scores", (req, res) => {
  db.all("SELECT * FROM Scores ORDER BY score DESC", [], (err, rows) => {
    if (err) return res.status(500).json({ error: err.message });
    res.json({ scores: rows });
  });
});

// Start the Express server
app.listen(PORT, () => {
  console.log(`🚀 Server running on http://localhost:${PORT}`);
});
