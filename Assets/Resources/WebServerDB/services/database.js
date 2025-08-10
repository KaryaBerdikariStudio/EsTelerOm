// services/database.js

const fs     = require("fs");
const path   = require("path");
const sqlite = require("sqlite3").verbose();

// —————————————————————————————————————————————————————————————
// Setup / Initialization
// —————————————————————————————————————————————————————————————

const dbPath = path.join(__dirname, "../game_database.db");
if (!fs.existsSync(dbPath)) {
  fs.writeFileSync(dbPath, "");
  console.log("Database file created at:", dbPath);
}

const db = new sqlite.Database(dbPath, err => {
  if (err) console.error("❌ DB connection error:", err.message);
  else     console.log("✅ Connected to SQLite at:", dbPath);
});

function initializeDatabase() {
  db.serialize(() => {
    db.run(`CREATE TABLE IF NOT EXISTS Clientele (
      nama   TEXT PRIMARY KEY,
      type   TEXT NOT NULL,
      status TEXT NOT NULL
    )`);
    db.run(`CREATE TABLE IF NOT EXISTS Feedback (
      nama     TEXT PRIMARY KEY,
      feedback TEXT NOT NULL
    )`);
    db.run(`CREATE TABLE IF NOT EXISTS RFID_Card (
      uid   TEXT PRIMARY KEY,
      huruf TEXT NOT NULL
    )`);
    db.run(`CREATE TABLE IF NOT EXISTS Kamus (
      tipe              TEXT NOT NULL,
      bahasaIndonesia   TEXT NOT NULL,
      bahasaDaerah      TEXT NOT NULL
    )`);
    db.run(`CREATE TABLE IF NOT EXISTS Skor (
      playerName TEXT NOT NULL,
      playerSkor INTEGER NOT NULL,
      timestamp  DATETIME NOT NULL
    )`);
    db.run(`CREATE TABLE IF NOT EXISTS Logs (
      tbl       TEXT NOT NULL,
      event     TEXT NOT NULL,
      message   TEXT NOT NULL,
      timestamp DATETIME DEFAULT CURRENT_TIMESTAMP
    )`);
    console.log("✅ Database and tables initialized successfully!");
  });
}

// —————————————————————————————————————————————————————————————
// Clientele (devices)  
// —————————————————————————————————————————————————————————————

function upsertClient({ nama, type, status }) {
  const sql = `
    INSERT INTO Clientele(nama, type, status)
      VALUES(?,?,?)
    ON CONFLICT(nama) DO UPDATE
      SET type    = excluded.type,
          status  = excluded.status
  `;
  return new Promise((res, rej) => {
    db.run(sql, [nama, type, status], err => err ? rej(err) : res());
  });
}

function getClientStatusNama(nama) {
  return new Promise((res, rej) => {
    db.get(
      `SELECT status FROM Clientele WHERE nama = ?`,
      [nama],
      (err, row) => {
        if (err) return rej(err);
        if (!row) return rej(new Error("Client not found"));
        res(row.status);
      }
    );
  });
}

function getClientStatusType(type) {
  return new Promise((res, rej) => {
    db.get(
      `SELECT status FROM Clientele WHERE type = ?`,
      [type],
      (err, row) => {
        if (err) return rej(err);
        if (!row) return rej(new Error("Client with "+ type +" not found"));
        res(row.status);
      }
    );
  });
}

function clearClientele() {
  return new Promise((res, rej) => {
    db.run(`DELETE FROM Clientele`, err => err ? rej(err) : res());
  });
}

function deleteClient(nama) {
  return new Promise((res, rej) => {
    db.run(
      `DELETE FROM Clientele WHERE nama = ?`,
      [nama],
      function (err) {
        if (err) return rej(err);
        this.changes === 0
          ? rej(new Error("Client not found"))
          : res();
      }
    );
  });
}

// —————————————————————————————————————————————————————————————
// Feedback  
// —————————————————————————————————————————————————————————————

function upsertFeedback({ nama, feedback }) {
  const sql = `
    INSERT INTO Feedback(nama, feedback)
      VALUES(?,?)
    ON CONFLICT(nama) DO UPDATE
      SET feedback = excluded.feedback
  `;
  return new Promise((res, rej) => {
    db.run(sql, [nama, feedback], err => err ? rej(err) : res());
  });
}

function getFeedback(nama) {
  return new Promise((res, rej) => {
    db.get(
      `SELECT feedback FROM Feedback WHERE nama = ?`,
      [nama],
      (err, row) => {
        if (err) return rej(err);
        res(row ? row.feedback : "");
      }
    );
  });
}

// —————————————————————————————————————————————————————————————
// RFID Card → letter mapping  
// —————————————————————————————————————————————————————————————

function fetchRFIDLetter(uid) {
  return new Promise((res, rej) => {
    db.get(
      `SELECT huruf FROM RFID_Card WHERE uid = ?`,
      [uid],
      (err, row) => {
        if (err) return rej(err);
        res(row ? row.huruf.toUpperCase() : "");
      }
    );
  });
}

function upsertUID(uid) {
  return new Promise((res, rej) => {
    db.get(
      `SELECT huruf FROM RFID_Card WHERE uid = ?`,
      [uid],
      (err, row) => {
        if (err) return rej(err);
        if (row) return res(row.huruf.toUpperCase());

        // otherwise, assign next letter
        db.get(
          `SELECT huruf FROM RFID_Card ORDER BY ROWID DESC LIMIT 1`,
          [],
          (e2, lastRow) => {
            if (e2) return rej(e2);
            let next = "A";
            if (lastRow && /^[A-Z]$/.test(lastRow.huruf)) {
              next = lastRow.huruf === "Z"
                ? "A"
                : String.fromCharCode(lastRow.huruf.charCodeAt(0) + 1);
            }
            db.run(
              `INSERT INTO RFID_Card(uid, huruf) VALUES(?,?)`,
              [uid, next],
              insertErr => insertErr ? rej(insertErr) : res(next)
            );
          }
        );
      }
    );
  });
}

// —————————————————————————————————————————————————————————————
// Kamus (dictionary)  
// —————————————————————————————————————————————————————————————

function upsertKamus({ tipe, bahasaIndonesia, bahasaDaerah }) {
  const sql = `
    INSERT INTO Kamus(tipe, bahasaIndonesia, bahasaDaerah)
      VALUES(?,?,?)
    ON CONFLICT(tipe) DO UPDATE
      SET bahasaIndonesia = excluded.bahasaIndonesia,
          bahasaDaerah    = excluded.bahasaDaerah
  `;
  return new Promise((res, rej) => {
    db.run(sql, [tipe, bahasaIndonesia, bahasaDaerah], err => err ? rej(err) : res());
  });
}

function getAllKamusByType(tipe) {
  return new Promise((res, rej) => {
    db.all(
      `SELECT tipe, bahasaIndonesia, bahasaDaerah
         FROM Kamus
        WHERE tipe = ?
        ORDER BY bahasaDaerah ASC`,
      [tipe],
      (err, rows) => {
        if (err) return rej(err);
        res(rows);
      }
    );
  });
}


function randomizeKamus(tipe) {
  return new Promise((res, rej) => {
    db.get(
      `SELECT bahasaDaerah, bahasaIndonesia
         FROM Kamus
        WHERE tipe = ?
        ORDER BY RANDOM()
        LIMIT 1`,
      [tipe],
      (err, row) => {
        if (err) return rej(err);
        if (!row) return rej(new Error("No Kamus entries found"));
        res(row);
      }
    );
  });
}

// —————————————————————————————————————————————————————————————
// Skor (scores)  
// —————————————————————————————————————————————————————————————

function insertSkor({ nama, skor, timestamp }) {
  const sql = `
    INSERT INTO Skor(playerName, playerSkor, timestamp)
      VALUES(?,?,?)
  `;
  return new Promise((res, rej) => {
    db.run(sql, [nama, skor, timestamp], err => err ? rej(err) : res());
  });
}

function getSkor() {
  return new Promise((res, rej) => {
    db.all(
      `SELECT playerName, playerSkor, timestamp
         FROM Skor
        ORDER BY timestamp DESC`,
      [],
      (err, rows) => {
        if (err) return rej(err);
        res(rows);
      }
    );
  });
}

// —————————————————————————————————————————————————————————————
// Logs  
// —————————————————————————————————————————————————————————————

function insertLog({ tbl, event, message }) {
  const sql = `
    INSERT INTO Logs(tbl, event, message)
      VALUES(?,?,?)
  `;
  return new Promise((res, rej) => {
    db.run(sql, [tbl, event, JSON.stringify(message)], err => err ? rej(err) : res());
  });
}

function getLogs() {
  return new Promise((res, rej) => {
    db.all(`SELECT * FROM Logs ORDER BY timestamp DESC`, [], (err, rows) => {
      if (err) return rej(err);
      res(rows);
    });
  });
}

function getLog(eventName) {
  return new Promise((res, rej) => {
    db.get(
      `SELECT * FROM Logs
         WHERE event = ?
      ORDER BY timestamp DESC
         LIMIT 1`,
      [eventName],
      (err, row) => {
        if (err) return rej(err);
        res(row);
      }
    );
  });
}

// —————————————————————————————————————————————————————————————
// Exports  
// —————————————————————————————————————————————————————————————

module.exports = {
  db,
  initializeDatabase,

  // Clientele
  upsertClient,
  getClientStatusNama,
  getClientStatusType,
  clearClientele,
  deleteClient,

  // Feedback
  upsertFeedback,
  getFeedback,

  // RFID
  fetchRFIDLetter,
  upsertUID,

  // Kamus
  upsertKamus,
  randomizeKamus,
  getAllKamusByType,

  // Skor
  insertSkor,
  getSkor,

  // Logs
  insertLog,
  getLogs,
  getLog,
};
