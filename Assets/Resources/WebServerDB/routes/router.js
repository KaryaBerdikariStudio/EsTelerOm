const express     = require('express');
const router      = express.Router();
const db          = require('../services/database');
const { publish } = require('../services/mqtt');
const { insertLog } = require('../services/database');

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
    await insertLog({ tbl: 'Clientele', event: 'Add', message: { nama, type, status } });

    const response = {
      tbl:    'Clientele',
      events: 'Add',
      konten: { nama, type, status }
    };
    res.status(201).json(response);

    if (type === 'esp32') {
      await publish('unity', response);
      await publish(nama,  response);
    } else if (type === 'unity') {
      await publish('unity', response);
    }
  } catch (err) {
    console.error('❌ Error in /addClientele:', err);
    await insertLog({
      tbl:    'Clientele',
      event:  'AddError',
      message:{ error: err.message, payload: req.body }
    });
    res.status(500).json({ tbl: 'Clientele', events: 'Add', konten: 'null' });
  }
});

router.post('/updateClientStatus', async (req, res) => {
  const { nama, type, status } = req.body;
  console.log(`📥 [POST] /updateClientStatus => nama: ${nama}, type: ${type}, status: ${status}`);
  try {
    await insertLog({ tbl: 'Clientele', event: 'Update', message: { nama, type, status } });
    if (type === 'esp32') {
      await db.upsertFeedback({ nama, feedback: status });
    }

    const response = {
      tbl:    'Clientele',
      events: 'Update',
      konten: { nama, type, status }
    };
    res.json(response);

    if (type === 'esp32') {
      await publish('unity', response);
      await publish(nama,  response);
    } else if (type === 'unity') {
      await publish('unity', response);
    }
  } catch (err) {
    console.error('❌ Error in /updateClientStatus:', err);
    await insertLog({
      tbl:    'Clientele',
      event:  'UpdateError',
      message:{ error: err.message, payload: req.body }
    });
    res.status(500).json({ tbl: 'Clientele', events: 'Update', konten: 'null' });
  }
});

router.get('/clientele/get/:nama', async (req, res) => {
  const nama = req.params.nama;
  console.log(`📥 [GET] /clientele/get/${nama}`);
  try {
    const status = await db.getClientStatusNama(nama);
    res.json({
      tbl:    'Clientele',
      events: `GetStatus/${nama}`,
      konten: { nama, status }
    });
  } catch (err) {
    console.error('❌ Error in GET /clientele/get/:nama:', err);
    res.status(500).json({
      tbl:    'Clientele',
      events: `GetStatus/${nama}`,
      konten: 'null'
    });
  }
});

router.get('/clientele/get/type/:type', async (req, res) => {
  const type = req.params.type;
  console.log(`📥 [GET] /clientele/get/${type}`);
  try {
    const list = await db.getClientStatusType(type);
    res.json({ tbl: 'Clientele', events: `GetStatus/${type}`, konten: list });
  } catch (err) {
    res.status(500).json({ tbl: 'Clientele', events: `GetStatus/${type}`, konten: 'null' });
  }
});

router.delete('/clientele/clear', async (req, res) => {
  console.log('🗑️ [DELETE] /clientele/clear');
  try {
    await db.clearClientele();
    await insertLog({ tbl: 'Clientele', event: 'DeleteAll', message: {} });

    res.json({
      tbl:    'Clientele',
      events: 'DeleteAll',
      konten: { nama: 'All', type: 'All', status: 'All' }
    });
  } catch (err) {
    console.error('❌ Error in DELETE /clientele/clear:', err);
    await insertLog({
      tbl:    'Clientele',
      event:  'DeleteAllError',
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
      event:  `Delete/${nama}`,
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
      event:  `DeleteError/${nama}`,
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
    await insertLog({ tbl: 'Kamus', event: 'RandomWord', message: response.konten });
    res.json(response);
    await publish('unity', response);
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
    res.json({ tbl: 'Kamus', events: 'FetchAllByType', konten: words });
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
    const letter  = await db.fetchRFIDLetter(uid);
    const payload = { nama: espId, uid, letter };
    await insertLog({ tbl: 'RFID', event: 'Scan', message: payload });

    const response = { tbl: 'RFID', events: 'Scan', konten: payload };
    res.json(response);
    await publish('unity', response);
  } catch (err) {
    console.error('❌ Error in POST /scanRFID:', err);
    await insertLog({
      tbl:    'RFID',
      event:  'ScanError',
      message:{ error: err.message }
    });
    res.status(500).json({ tbl: 'RFID', events: 'Scan', konten: 'null' });
  }
});

router.post('/inputRFID', async (req, res) => {
  const { espId, uid } = req.body;
  if (!espId || !uid) {
    return res.status(400).json({ tbl: 'RFID', events: 'Input', konten: 'null' });
  }
  try {
    const letter  = await db.upsertUID(uid);
    const payload = { nama: espId, uid, letter };
    await insertLog({ tbl: 'RFID', event: 'Input', message: payload });

    const response = { tbl: 'RFID', events: 'Input', konten: payload };
    res.json(response);
    await publish('unity', response);
  } catch (err) {
    console.error('❌ Error in POST /inputRFID:', err);
    await insertLog({
      tbl:    'RFID',
      event:  'InputError',
      message:{ error: err.message }
    });
    res.status(500).json({ tbl: 'RFID', events: 'InputError', konten: 'null' });
  }
});

// ───────────────────────────────────────────────
// Input Log Endpoints
// ───────────────────────────────────────────────

router.get('/getLog/:event', async (req, res) => {
  const event = req.params.event;
  console.log(`📥 [GET] /getLog/${event}`);
  try {
    const logs = await db.getLog(event);
    res.json({ tbl: 'Logs', events: 'FetchOne', konten: logs });
  } catch (err) {
    console.error('❌ Error in GET /getLog/:event:', err);
    res.status(500).json({ tbl: 'Logs', events: 'FetchOne', konten: 'null' });
  }
});

router.get('/getLogs', async (req, res) => {
  console.log('📥 [GET] /getLogs');
  try {
    const logs = await db.getLogs();
    res.json({ tbl: 'Logs', events: 'FetchAll', konten: logs });
  } catch (err) {
    console.error('❌ Error in GET /getLogs:', err);
    res.status(500).json({ tbl: 'Logs', events: 'FetchAll', konten: 'null' });
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
    await insertLog({ tbl: 'Feedback', event: 'Add', message: { nama, feedback } });

    const response = { tbl: 'Feedback', events: 'Add', konten: { nama, feedback } };
    res.status(201).json(response);
    await publish('unity', response);
  } catch (err) {
    console.error('❌ Error in POST /feedbackPost:', err);
    await insertLog({
      tbl:    'Feedback',
      event:  'AddError',
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

module.exports = router;
