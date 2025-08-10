// router.js
const express = require('express');
const { db } = require('./database');
const { encryptIntoEsp32, encryptIntoDB, decrypt } = require('./cryptograph');
const router = express.Router();

let kataYangDitebak = "";


/** ——— SSE SETUP ——— **/
const sseClients = [];

function broadcastClientUpdate(msg) {
  const data = JSON.stringify(msg);
  sseClients.forEach(c => c.res.write(`data: ${data}\n\n`));
}

// SSE stream endpoint (only once!)
router.get('/sse/clients', (req, res) => {
  res.writeHead(200, {
    'Content-Type':  'text/event-stream',
    'Cache-Control':  'no-cache',
    Connection:       'keep-alive'
  });
  // initial heartbeat so the client knows the stream is alive
  res.write(': heartbeat\n\n');

  const client = { id: Date.now(), res };
  sseClients.push(client);

  req.on('close', () => {
    const idx = sseClients.findIndex(c => c.id === client.id);
    if (idx >= 0) sseClients.splice(idx, 1);
  });
});


/** ——— HOOK HELPER ——— **
 * Wraps a route so that after a successful (2xx) response,
 * we call cb(req.body) and broadcast its contents over SSE.
 */
function hook(path, cb) {
  const layer = router.stack.find(r => r.route?.path === path)?.route?.stack[0]?.handle;
  if (!layer) return console.warn(`hook: no route for ${path}`);
  router.stack.find(r => r.route?.path === path).route.stack[0].handle = async (req, res, next) => {
    await layer(req, res, next);
    if (res.statusCode >= 200 && res.statusCode < 300) {
      cb(req.body);
    }
  };
}

/** ——— GAME WORD ——— **/
router.post('/setKata', (req, res) => {
  try {
    const { kata } = req.body;
    
    if (!kata || typeof kata !== 'string') {
      return res.status(400).json({ 
        error: "Parameter 'kata' diperlukan dan harus berupa string" 
      });
    }
    kataYangDitebak = kata.toUpperCase();
    console.log(`Kata berhasil diupdate: ${kataYangDitebak}`);
    
    res.json({ 
      success: true,
      message: "Kata disimpan",
      kata: kataYangDitebak 
    });

  } catch (error) {
    console.error("Error di /setKata:", error);
    res.status(500).json({
      error: "Terjadi kesalahan server internal"
    });
  }
});


/** ——— CLIENTELE ENDPOINTS ——— **/
async function upsertClientele(ip, type, nama, status) {
  const sql = `
    INSERT INTO Clientele (ip, type, nama, status)
    VALUES (?, ?, ?, ?)
    ON CONFLICT(nama) DO UPDATE
      SET ip     = excluded.ip,
          type   = excluded.type,
          status = excluded.status
  `;
  return new Promise((res, rej) => db.run(sql, [ip, type, nama, status], err => err ? rej(err) : res()));
}

router.post('/addClientele', async (req, res) => {
  const { ip, type, nama, status } = req.body;
  if (!ip || !type || !nama || !status) 
    return res.status(400).json({ error: "ip, type, nama & status required" });
  try {
    await upsertClientele(ip, type, nama, status);
    res.json({ message: "Clientele upserted", ip, type, nama, status });
  } catch (e) {
    console.error(e);
    res.status(500).json({ error: "DB error upserting clientele" });
  }
});

// Broadcast new clients
hook('/addClientele', ({ ip, type, nama }) =>
  broadcastClientUpdate({ event: 'clientAdded', ip, type, nama })
);


router.get('/clientele/type/:type/:status?', (req, res) => {
  const { type, status } = req.params;
  const sql = status
    ? `SELECT nama FROM Clientele WHERE type = ? AND status = ? ORDER BY nama`
    : `SELECT nama FROM Clientele WHERE type = ? ORDER BY nama`;
  db.all(sql, status ? [type, status] : [type], (err, rows) => {
    if (err) return res.status(500).json({ error: "DB error fetching clientele" });
    res.json({ clientele: rows });
  });
});

router.get('/clientele/nama/:nama', (req, res) => {
  db.all("SELECT status FROM Clientele WHERE nama = ?", [req.params.nama], (err, rows) => {
    if (err) return res.status(500).json({ error: "DB error fetching status" });
    res.json({ clientele: rows });
  });
});

router.get('/clientele/ip/:ip', (req, res) => {
  db.get("SELECT status FROM Clientele WHERE ip = ?", [req.params.ip], (err, row) => {
    if (err) return res.status(500).json({ error: "DB error fetching status" });
    if (!row) return res.status(404).json({ error: "IP not found" });
    res.json(row);
  });
});

router.post('/clientele/updateStatus', (req, res) => {
  const { nama, status, type } = req.body;
  if (!nama || !status) 
    return res.status(400).json({ error: "nama & status required" });

  db.run(
    `UPDATE Clientele SET status = ? WHERE nama = ? AND type = ?`,
    [status, nama, type],
    function(err) {
      if (err) {
        console.error(err);
        return res.status(500).json({ error: "DB error updating status" });
      }
      if (this.changes === 0) {
        return res.status(404).json({ error: "Client not found" });
      }
      res.json({ message: "✅ Status updated", nama, status, type });
    }
  );
});

// Broadcast status updates
hook('/clientele/updateStatus', ({ nama, status, type }) =>
  broadcastClientUpdate({ event: 'statusUpdated', nama, status, type })
);

router.delete('/deleteClientele', (req, res) => {
  db.run("DELETE FROM Clientele", [], err => {
    if (err) return res.status(500).json({ error: "DB error clearing clientele" });
    res.json({ message: "✅ All clientele cleared" });
  });
});

router.delete('/deleteClientele/:nama', (req, res) => {
  db.run("DELETE FROM Clientele WHERE nama = ?", [req.params.nama], err => {
    if (err) return res.status(500).json({ error: "DB error deleting clientele" });
    res.json({ message: "✅ Client deleted" });
  });
});


/** ——— INPUT / CHECK‑INPUT ——— **/
/** ——— INPUT / CHECK‑INPUT ——— **/
function checkPlayerTerkonfirmasi(nama) {
  return new Promise((res, rej) => {
    console.log(`🔍 Checking player confirmation for: ${nama}`);
    db.get("SELECT status FROM Clientele WHERE nama = ?", [nama], (err, row) => {
      if (err) {
        console.error(`❌ DB error checking player ${nama}:`, err);
        return rej(err);
      }
      const isConfirmed = row && row.status === "siapBermain";
      console.log(`🔄 Player ${nama} confirmation status: ${isConfirmed ? "Confirmed" : "Not confirmed"}`);
      res(isConfirmed);
    });
  });
}

function fetchRFIDLetter(uid) {
  return new Promise((res, rej) => {
    console.log(`📲 Fetching RFID letter for UID: ${uid}`);
    db.get("SELECT huruf FROM RFID_Card WHERE uid = ?", [uid], (e, row) => {
      if (e) {
        console.error(`❌ RFID fetch error for ${uid}:`, e);
        return rej(e);
      }
      if (!row) {
        console.log(`⚠️ UID not found: ${uid}`);
        return rej(new Error("UID not found"));
      }
      const letter = row.huruf.toUpperCase();
      console.log(`✅ RFID letter found: ${letter} for UID: ${uid}`);
      res(letter);
    });
  });
}

let lastHuruf = "";

function insertInputLog({ type, huruf, nama, benarSalah }) {
  type = type || "esp32";
  return new Promise((res, rej) => {
    console.log(`📝 Inserting input log - Player: ${nama}, Letter: ${huruf}, Status: ${benarSalah}`);
    db.run(
      `INSERT INTO Input_Logs (type, huruf, nama, benarSalah) VALUES (?, ?, ?, ?)`,
      [type, huruf, nama, benarSalah],
      err => {
        if (err) {
          console.error(`❌ Error inserting log for ${nama}:`, err);
          return rej(err);
        }
        console.log(`✅ Input logged successfully for ${nama}`);
        res();
      }
    );
  });
}

router.post('/checkInput', async (req, res) => {
  console.log('\n=== New Input Check ===');
  console.log('Received body:', req.body);
  
  const { nama, uid } = req.body;
  if (!nama || !uid) {
    console.log('❌ Missing parameters - nama or uid');
    return res.status(400).json({ error: "nama & uid required" });
  }

  if (!kataYangDitebak) {
    console.log('⚠️ Game word not set');
    return res.status(400).json({ error: "Game word not set" });
  }


  

  try {
    console.log(`🔒 Checking authorization for ${nama}`);
    const isConfirmed = await checkPlayerTerkonfirmasi(nama);
    if (!isConfirmed) {
      console.log(`⛔ Unauthorized access attempt by ${nama}`);
      return res.status(403).json({ error: "Player not confirmed" });
    }

    let benarSalah = "";
    console.log(`🔑 Processing RFID input for ${nama}`);
    const huruf = await fetchRFIDLetter(uid);

    if (huruf === lastHuruf) {
      console.log(`⚠️ Duplicate input detected for ${nama}: ${huruf}`);
      console.log('📤 Broadcasting inputLogged event:', row);
          
      benarSalah = "duplicate";
    
    }else {
      lastHuruf = huruf;

      benarSalah = kataYangDitebak.includes(huruf) ? "benar" : "salah";
    }

    
    console.log(`🔠 Letter check result: ${huruf} - ${benarSalah}`);

    console.log(`📥 Saving input for ${nama}`);
    await insertInputLog({ 
      type: "esp32", 
      huruf, 
      nama, 
      benarSalah 
    });

    db.get(
      `SELECT type, huruf, nama, benarSalah AS status
       FROM Input_Logs
       WHERE nama = ?
       ORDER BY rowid DESC
       LIMIT 1`,
      [nama],
      (err, row) => {
        if (err) {
          console.error(`❌ Error fetching last input:`, err);
          return res.status(500).json({ error: "DB error fetching log" });
        }

        // ======== BROADCAST DIRECTLY HERE ========
        if (row) {
          console.log('📤 Broadcasting inputLogged event:', row);
          broadcastClientUpdate({
            event: 'inputLogged',
            type: row.type,
            huruf: row.huruf,
            nama: row.nama,
            status: row.status
          });
        }
        // ======== END OF BROADCAST ========

        res.json(row || {});
      }
    );
  } catch (e) {
    console.error(`🔥 Critical error:`, e);
    res.status(500).json({ error: e.message });
  }
});




/** ——— REGISTERED PLAYERS ——— **/
router.post('/daftarPlayer', (req, res) => {
  const { namaDevice, namaPlayer, RGB } = req.body;
  if (!namaDevice || !namaPlayer || !RGB)
    return res.status(400).json({ error: 'Missing fields' });

  db.run(
    `INSERT INTO RegisteredPlayer (namaDevice, namaPlayer, RGB)
       VALUES (?, ?, ?)
     ON CONFLICT(namaDevice) DO UPDATE
       SET namaPlayer = excluded.namaPlayer,
           RGB        = excluded.RGB`,
    [namaDevice, namaPlayer, RGB],
    function(err) {
      if (err) return res.status(500).json({ error: 'DB error' });
      res.json({ message: '✅ Player registered', id: this.lastID });
    }
  );
});

router.get('/lihatPlayer/device/:namaDevice', (req, res) => {
  db.get("SELECT * FROM RegisteredPlayer WHERE namaDevice = ?", [req.params.namaDevice], (err, row) => {
    if (err) return res.status(500).json({ error: 'DB error' });
    row ? res.json(row) : res.status(404).json({ error: 'Player not found' });
  });
});

router.get('/lihatPlayer/player/:namaPlayer', (req, res) => {
  db.get("SELECT * FROM RegisteredPlayer WHERE namaPlayer = ?", [req.params.namaPlayer], (err, row) => {
    if (err) return res.status(500).json({ error: 'DB error' });
    row ? res.json(row) : res.status(404).json({ error: 'Player not found' });
  });
});

router.delete('/deletePlayer', (req, res) => {
  db.run("DELETE FROM RegisteredPlayer", [], err => {
    if (err) return res.status(500).json({ error: "DB error deleting players" });
    res.json({ message: "✅ All players cleared" });
  });
});


/** ==== UID Mapper ==== **/

router.get('/cekHurufTerakhir', (req, res) => {
  db.get(
    "SELECT huruf FROM RFID_Card ORDER BY rowid DESC LIMIT 1",
    [],
    (err, row) => {
      if (err) return res.status(500).json({ error: "DB error fetching the latest UID" });
      if (!row) return res.status(404).json({ error: "No UIDs found" });
      res.json(row);
    }
  );
});

router.post('/registerUID', (req, res) => {
  const { uid, huruf } = req.body;

  if (!uid || !huruf) {
    return res.status(400).json({ error: "Both 'uid' and 'huruf' are required" });
  }

  // Check if the UID already exists
  db.get("SELECT * FROM RFID_Card WHERE uid = ?", [uid], (err, row) => {
    if (err) {
      console.error(`❌ Error checking UID:`, err);
      return res.status(500).json({ error: "DB error checking UID" });
    }

    if (row) {
      // UID already exists
      return res.status(409).json({ message: "UID already used", uid });
    }

    // Insert the new UID and huruf
    db.run(
      `INSERT INTO RFID_Card (uid, huruf) VALUES (?, ?)`,
      [uid, huruf],
      function (err) {
        if (err) {
          console.error(`❌ Error registering UID:`, err);
          return res.status(500).json({ error: "DB error registering UID" });
        }
        res.json({ message: "✅ UID registered successfully", uid, huruf });
      }
    );
  });
});

router.post('/inputDaftarUID', (req, res) => {
  const { uid } = req.body;

  if (!uid) {
    return res.status(400).json({ error: "UID is required" });
  }

  broadcastClientUpdate({ event: 'wantregistercard', uid });

});

module.exports = router;
