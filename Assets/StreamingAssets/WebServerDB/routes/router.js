const express     = require('express');
const router      = express.Router();
const db          = require('../services/database');
const { insertLog } = require('../services/database');


// ───────────────────────────────────────────────
// test
// ───────────────────────────────────────────────
router.get('/test', (req, res) => {
  console.log('📥 [GET] /test');
  res.status(200).json({
    tbl:    'Test',
    events: 'Ping',
    konten: 'Pong'});
});


// ───────────────────────────────────────────────
// Clientele Management
// ───────────────────────────────────────────────

router.post('/addClientele', async (req, res) => {
  const { type, nama } = req.body;
  console.log(`📥 [POST] /addClientele => type: ${type}, nama: ${nama}`);
  try {
    const status = "siap";
    await db.upsertClient({ nama, type, status });
    if (type === 'esp32') {
      await db.upsertFeedback({ nama, feedback: status });
    }
    await insertLog({ tbl: 'Clientele', events: 'Add', message: { nama, type, status } });

    const response = {
      tbl:    'Clientele',
      events: 'Add',
      konten: { nama, type, status }
    };
    res.status(201).json(response);

  } catch (err) {
    console.error('❌ Error in /addClientele:', err);
    await insertLog({
      tbl:    'Clientele',
      events:  'AddError',
      message:{ error: err.message, payload: req.body }
    });
    res.status(500).json({ tbl: 'Clientele', events: 'Add', konten: 'null' });
  }
});


var lastUpdateClienteleConsole = "";
router.post('/updateClientStatus', async (req, res) => {
  const { nama, type, status } = req.body;
  console.log(`📥 [POST] /updateClientStatus => nama: ${nama}, type: ${type}, status: ${status}`);
  try {
    await insertLog({ tbl: 'Clientele', events: 'Update', message: { nama, type, status } });
    await db.upsertClient({ nama, type, status });
    const response = {
      tbl:    'Clientele',
      events: 'Update',
      konten: { nama, type, status }
    };
    console.log(`📤 Clientele status updated:`, response);
    res.json(response);

  } catch (err) {
    console.error('❌ Error in /updateClientStatus:', err);
    await insertLog({
      tbl:    'Clientele',
      events:  'UpdateError',
      message:{ error: err.message, payload: req.body }
    });
    res.status(500).json({ tbl: 'Clientele', events: 'Update', konten: 'null' });
  }
});

var lastGetClienteleNameConsole = "";
let _lastGetByNameKey = null;


router.get('/clientele/get/:nama', async (req, res) => {
  const nama = req.params.nama;
  try {
    const status = await db.getClientStatusNama(nama);
    const payload = {
      tbl:    'Clientele',
      events: `GetStatus/${nama}`,
      konten: { nama, status }
    };

    // serialize a “key” for comparison
    const thisKey = `${nama}→${status}`;

    // only log if different from last time
    if (thisKey !== _lastGetByNameKey) {
      console.log(`📥 [GET] /clientele/get/${nama} =>`, payload);
      _lastGetByNameKey = thisKey;
    }

    res.json(payload);
  } catch (err) {
    console.error('❌ Error in GET /clientele/get/:nama:', err);
    res.status(500).json({
      tbl:    'Clientele',
      events: `GetStatus/${nama}`,
      konten: 'null'
    });
  }
});

// at top of this file, alongside your other `var last…` trackers
let _lastGetByTypeKey = null;

// …then in your GET /clientele/get/type/:type handler:
router.get('/clientele/get/type/:type', async (req, res) => {
  const type = req.params.type;
  try {
    const list = await db.getClientStatusType(type);

    // build your response
    const response = {
      tbl:    'Clientele',
      events: `GetStatus/${type}`,
      kontens: list    // note: use `kontens` for array
    };

    // serialize a “key” for comparison
    const thisKey = `${type}→${JSON.stringify(list)}`;

    // only log if the payload really changed
    if (thisKey !== _lastGetByTypeKey) {
      console.log(`📥 [GET] /clientele/get/type/${type} =>`, list);
      _lastGetByTypeKey = thisKey;
    }

    // send it (success always uses `kontens`)
    res.json(response);

  } catch (err) {
    console.error('❌ Error in GET /clientele/get/type/:type:', err);
    // on error, return the same shape with an empty array
    res.status(500).json({
      tbl:    'Clientele',
      events: `GetStatus/${type}`,
      kontens: []
    });
  }
});


router.delete('/clientele/clear', async (req, res) => {
  console.log('🗑️ [DELETE] /clientele/clear');
  try {
    await db.clearClientele();
    await insertLog({ tbl: 'Clientele', events: 'DeleteAll', message: {} });

    res.json({
      tbl:    'Clientele',
      events: 'DeleteAll',
      konten: { nama: 'All', type: 'All', status: 'All' }
    });
  } catch (err) {
    console.error('❌ Error in DELETE /clientele/clear:', err);
    await insertLog({
      tbl:    'Clientele',
      events:  'DeleteAllError',
      message:{ error: err.message }
    });
    res.status(500).json({ tbl: 'Clientele', events: 'DeleteAll', konten: 'null' });
  }
});

router.delete('/clientele/delete/:nama', async (req, res) => {
  const nama = req.params.nama;
  console.log(`🗑️ [DELETE] /clientele/delete/${nama}`);
  try {
    await db.deleteClient(nama);
    await insertLog({
      tbl:    'Clientele',
      events:  `Delete/${nama}`,
      message:{ nama, type: 'Unknown', status: 'Deleted' }
    });

    res.json({
      tbl:    'Clientele',
      events: `Delete/${nama}`,
      konten: { nama, type: 'Unknown', status: 'Deleted' }
    });
  } catch (err) {
    console.error('❌ Error in DELETE /clientele/delete/:nama:', err);
    await insertLog({
      tbl:    'Clientele',
      events:  `DeleteError/${nama}`,
      message:{ error: err.message }
    });
    res.status(500).json({ tbl: 'Clientele', events: `Delete/${nama}`, konten: 'null' });
  }
});

// ───────────────────────────────────────────────
// Kamus (Dictionary) Endpoints
// ───────────────────────────────────────────────

router.get('/getKata/:bahasa', async (req, res) => {
  const bahasa = req.params.bahasa;
  console.log(`📥 [GET] /getKata/${bahasa}`);
  try {
    const entry = await db.randomizeKamus(bahasa);
    const response = {
      tbl:    'Kamus',
      events: 'RandomWord',
      konten: {
        tipe:            bahasa,
        bahasaDaerah:    entry.bahasaDaerah,
        bahasaIndonesia: entry.bahasaIndonesia
      }
    };
    await insertLog({ tbl: 'Kamus', events: 'RandomWord', message: response.konten });
    res.json(response);
  } catch (err) {
    console.error('❌ Error in GET /getKata/:bahasa:', err);
    await insertLog({
      tbl:    'Kamus',
      event:  'RandomWordError',
      message:{ error: err.message }
    });
    res.status(500).json({ tbl: 'Kamus', events: 'RandomWord', konten: 'null' });
  }
});

router.get('/getAllKata/:tipe', async (req, res) => {
  const tipe = req.params.tipe;

  console.log(`📥 [GET] /getAllKata/${tipe}`);

  try {
    const words = await db.getAllKamusByType(tipe);
    res.json({ tbl: 'Kamus', events: 'FetchAllByType', kontens: words });
  } catch (err) {
    res.status(500).json({ tbl: 'Kamus', events: 'FetchAllByType', konten: [] });
  }
});


// ───────────────────────────────────────────────
// RFID Input Endpoints
// ───────────────────────────────────────────────

router.post('/scanRFID', async (req, res) => {
  const { espId, uid } = req.body;
  console.log(`📥 [POST] /scanRFID => espId: ${espId}, uid: ${uid}`);
  if (!espId || !uid) {
    console.warn('⚠️ espId or uid missing');
    return res.status(400).json({ tbl: 'RFID', events: 'Scan', konten: 'null' });
  }
  try {
    // Special case: esp32_0
    if (espId === 'esp32_0') {
      const payload = { nama: espId, uid, letter: uid };
      await insertLog({ tbl: 'RFID', events: 'Scan', message: payload });
      return res.json({ tbl: 'RFID', events: 'Scan', konten: payload });
    }

    // Normal case
    const letter = await db.fetchRFIDLetter(uid);
    const payload = { nama: espId, uid, letter };

    await insertLog({ tbl: 'RFID', events: 'Scan', message: payload });

    const response = { tbl: 'RFID', events: 'Scan', konten: payload };
    res.json(response);

  } catch (err) {
    console.error('❌ Error in POST /scanRFID:', err);
    await insertLog({ tbl: 'RFID', events: 'ScanError', message: { error: err.message } });
    res.status(500).json({ tbl: 'RFID', events: 'Scan', konten: 'null' });
  }
});

router.post('/inputRFID', async (req, res) => {
  const { espId, uid } = req.body;
  console.log(`📥 [POST] /inputRFID => espId: ${espId}, uid: ${uid}`);
  if (!espId || !uid) {
    return res.status(400).json({ tbl: 'RFID', events: 'Input', konten: 'null' });
  }
  try {
    const letter  = await db.upsertUID(uid);
    const payload = { nama: espId, uid, letter };
    await insertLog({ tbl: 'RFID', events: 'Input', message: payload });

    const response = { tbl: 'RFID', events: 'Input', konten: payload };
    console.log(`📤 RFID input processed:`, response);
    res.json(response);
  } catch (err) {
    console.error('❌ Error in POST /inputRFID:', err);
    await insertLog({
      tbl:    'RFID',
      events:  'InputError',
      message:{ error: err.message }
    });
    res.status(500).json({ tbl: 'RFID', events: 'InputError', konten: 'null' });
  }
});

// ───────────────────────────────────────────────
// Input Log Endpoints
// ───────────────────────────────────────────────

var lastLog = "";

router.get('/getLog/:event', async (req, res) => {
  const event = req.params.event;
  try {
    
    const logs = await db.getLog(event);
    var payload = { tbl: 'Logs', events: 'FetchOne', konten: logs }
    var currentLog = payload;
    res.json(payload);
    if (lastLog !== JSON.stringify(currentLog)) {
      console.log(`📥 [GET] /getLog/${event}`);
      lastLog = JSON.stringify(currentLog);
      console.log(`📤 Logs for event "${event}":`, logs);
    }
  } catch (err) {
    console.error('❌ Error in GET /getLog/:event:', err);
    res.status(500).json({ tbl: 'Logs', events: 'FetchOne', konten: 'null' });
  }
});

router.get('/getLogs', async (req, res) => {
  console.log('📥 [GET] /getLogs');
  try {
    const logs = await db.getLogs();
    res.json({ tbl: 'Logs', events: 'FetchAll', kontens: logs });
  } catch (err) {
    console.error('❌ Error in GET /getLogs:', err);
    res.status(500).json({ tbl: 'Logs', events: 'FetchAll', konten: 'null' });
  }
});

router.delete('/deleteLogs', async (req, res) => {
  console.log('🗑️ [DELETE] /deleteLogs');
  try {
    await db.deleteLogs();
    console.log('✅ All logs deleted');
    res.json({ tbl: 'Logs', events: 'DeleteAll', konten: { status: 'All logs deleted' } });
  } catch (err) {
    console.error('❌ Error in DELETE /deleteLogs:', err);
    res.status(500).json({ tbl: 'Logs', events: 'DeleteAll', konten: 'null' });
  }
});
// ───────────────────────────────────────────────
// Feedback Endpoints
// ───────────────────────────────────────────────

router.post('/feedbackPost', async (req, res) => {
  const { nama, feedback } = req.body;
  console.log(`📥 [POST] /feedbackPost => nama: ${nama}, feedback: ${feedback}`);
  try {
    await db.upsertFeedback({ nama, feedback });
    await insertLog({ tbl: 'Feedback', events: 'Add', message: { nama, feedback } });

    const response = { tbl: 'Feedback', events: 'Add', konten: { nama, feedback } };
    res.status(201).json(response);
  } catch (err) {
    console.error('❌ Error in POST /feedbackPost:', err);
    await insertLog({
      tbl:    'Feedback',
      events:  'AddError',
      message:{ error: err.message }
    });
    res.status(500).json({ tbl: 'Feedback', events: 'Add', konten: 'null' });
  }
});

router.get('/feedbackGet/:nama', async (req, res) => {
  const nama = req.params.nama;
  console.log(`📥 [GET] /feedbackGet/${nama}`);
  try {
    const feedback = await db.getFeedback(nama);
    res.json({ tbl: 'Feedback', events: `Get/${nama}`, konten: { nama, feedback } });
  } catch (err) {
    console.error('❌ Error in GET /feedbackGet/:nama:', err);
    res.status(500).json({ tbl: 'Feedback', events: `Get/${nama}`, konten: 'null' });
  }
});
// ───────────────────────────────────────────────
// Skor Endpoints
// ───────────────────────────────────────────────

router.post('/skorPost', async (req, res) => {
  const { nama, skor } = req.body;
  console.log(`📥 [POST] /skorPost => nama: ${nama}, skor: ${skor}`);
  try {
    await db.upsertSkor({ nama, skor });
    await insertLog({ tbl: 'Skor', events: 'Add', message: { nama, skor } });

    const response = { tbl: 'Skor', events: 'Add', konten: { nama, skor } };
    res.status(201).json(response);
  } catch (err) {
    console.error('❌ Error in POST /skorPost:', err);
    await insertLog({
      tbl:    'Skor',
      events:  'AddError',
      message:{ error: err.message }
    });
    res.status(500).json({ tbl: 'Skor', events: 'Add', konten: 'null' });
  }
});

router.get('/skorGet', async (req, res) => {
  console.log('📥 [GET] /skorGet');
  try {
    const scores = await db.getSkor();
    res.json({ tbl: 'Skor', events: 'FetchAll', kontens: scores });
  } catch (err) {
    console.error('❌ Error in GET /skorGet:', err);
    res.status(500).json({ tbl: 'Skor', events: 'FetchAll', konten: 'null' });
  }
});
module.exports = router;
