// services/api-tester.test.js
// Jest + Supertest integration tests for Express API routes
// --------------------------------------------------------

jest.spyOn(console, 'log').mockImplementation(() => {});
jest.spyOn(console, 'error').mockImplementation(() => {});

jest.mock('./database');
jest.mock('./mqtt');

const request = require('supertest');
const express = require('express');

const db      = require('./database');
const { publish } = require('./mqtt');
const router  = require('../routes/router');

describe('API Router Endpoints', () => {
  let app;

  beforeAll(() => {
    app = express();
    app.use(express.json());
    app.use('/api', router);
  });

  beforeEach(() => jest.clearAllMocks());

  describe('GET /api/test', () => {
    it('should return a simple JSON response', async () => {
      const res = await request(app).get('/api/test').expect(200);
      expect(res.body).toEqual({ tbl : 'Test', events: 'Ping', konten: 'Pong' });
    });
  });

  describe('POST /api/addClientele', () => {
    it('should add a new ESP32 client and publish events', async () => {
      db.upsertClient.mockResolvedValue();
      db.upsertFeedback.mockResolvedValue();
      db.insertLog = jest.fn().mockResolvedValue();
      publish.mockResolvedValue();

      const res = await request(app)
        .post('/api/addClientele')
        .send({ type: 'esp32', nama: 'device1' })
        .expect(201);

      expect(res.body).toHaveProperty('konten');
      expect(db.upsertClient).toHaveBeenCalledWith({ nama: 'device1', type: 'esp32', status: 'siap' });
      expect(db.upsertFeedback).toHaveBeenCalledWith({ nama: 'device1', feedback: 'siap' });
      expect(publish).toHaveBeenCalledWith('unity', expect.any(Object));
      expect(publish).toHaveBeenCalledWith('device1', expect.any(Object));
    });

    it('returns 500 on error', async () => {
      db.upsertClient.mockRejectedValue(new Error('DB error'));
      db.insertLog = jest.fn().mockResolvedValue();

      const res = await request(app)
        .post('/api/addClientele')
        .send({ type: 'unity', nama: 'clientX' })
        .expect(500);

      expect(res.body).toMatchObject({ tbl: 'Clientele', events: 'Add', konten: 'null' });
    });
  });

  describe('POST /api/updateClientStatus', () => {
    it('publishes and returns JSON', async () => {
      db.upsertFeedback.mockResolvedValue();
      db.insertLog = jest.fn().mockResolvedValue();
      publish.mockResolvedValue();

      const res = await request(app)
        .post('/api/updateClientStatus')
        .send({ nama: 'device1', type: 'esp32', status: 'ready' })
        .expect(200);

      expect(res.body.konten).toEqual({ nama: 'device1', type: 'esp32', status: 'ready' });
      expect(publish).toHaveBeenCalledWith('unity', expect.any(Object));
    });
  });

  describe('GET /api/clientele/get/:nama', () => {
    it('should return client status by name', async () => {
      db.getClientStatusNama.mockResolvedValue('siap');

      const res = await request(app)
        .get('/api/clientele/get/device1')
        .expect(200);

      expect(res.body).toEqual({
        tbl:    'Clientele',
        events: 'GetStatus/device1',
        konten: { nama: 'device1', status: 'siap' }
      });
    });
  });

  describe('GET /api/clientele/get/:nama', () => {
    it('returns client status by name', async () => {
      db.getClientStatusNama.mockResolvedValue('siap');
      const res = await request(app).get('/api/clientele/get/device1').expect(200);
      expect(res.body).toEqual({
        tbl:    'Clientele',
        events: 'GetStatus/device1',
        konten: { nama: 'device1', status: 'siap' }
      });
    });
  });

  describe('GET /api/clientele/get/type/:type', () => {
    it('returns an array of clients by type', async () => {
      const fakeList = ['deviceA', 'deviceB'];
      db.getClientStatusType.mockResolvedValue(fakeList);
      const res = await request(app)
        .get('/api/clientele/get/type/esp32')
        .expect(200);
      expect(res.body).toEqual({
        tbl:    'Clientele',
        events: 'GetStatus/esp32',
        konten: fakeList
      });
    });
  });

  describe('API Router Endpoints', () => {
    describe('GET /api/getAllKata/:tipe', () => {
      it(
        'fetches all Kamus entries for a type',
        async () => {
          const words = [
            { bahasaDaerah: 'kayu', bahasaIndonesia: 'tree' },
            { bahasaDaerah: 'batu', bahasaIndonesia: 'rock' }
          ];
          db.getAllKamusByType.mockResolvedValue(words);

          const res = await request(app)
            .get('/api/getAllKata/sunda')
            .expect(200);

          expect(res.body).toEqual({
            tbl: 'Kamus',
            events: 'FetchAllByType',
            konten: words
          });
        },
        10000 // timeout in ms
      );

      it(
        'returns empty array on error',
        async () => {
          db.getAllKamusByType.mockRejectedValue(new Error('DB error'));

          const res = await request(app)
            .get('/api/getAllKata/sunda')
            .expect(500);

          expect(res.body.konten).toEqual([]);
        },
        10000
      );
    });
  });

  describe('DELETE /api/clientele/clear', () => {
    it('should clear all clients', async () => {
      db.clearClientele.mockResolvedValue();
      db.insertLog = jest.fn().mockResolvedValue();

      const res = await request(app)
        .delete('/api/clientele/clear')
        .expect(200);

      expect(res.body.events).toBe('DeleteAll');
    });
  });

  describe('DELETE /api/clientele/delete/:nama', () => {
    it('deletes a specific client', async () => {
      db.deleteClient.mockResolvedValue();
      db.insertLog = jest.fn().mockResolvedValue();

      const res = await request(app)
        .delete('/api/clientele/delete/device1')
        .expect(200);

      expect(res.body.events).toBe('Delete/device1');
    });
  });

  describe('GET /api/getKata/:bahasa', () => {
    it('returns a random word and publishes it', async () => {
      const fakeEntry = { bahasaDaerah: 'kupu', bahasaIndonesia: 'butterfly' };
      db.randomizeKamus.mockResolvedValue(fakeEntry);
      db.insertLog = jest.fn().mockResolvedValue();
      publish.mockResolvedValue();

      const res = await request(app)
        .get('/api/getKata/jawa')
        .expect(200);

      expect(res.body.konten).toMatchObject(fakeEntry);
      expect(publish).toHaveBeenCalledWith('unity', expect.any(Object));
    });

    it('returns 500 on DB error', async () => {
      db.randomizeKamus.mockRejectedValue(new Error('fail DB'));
      db.insertLog = jest.fn().mockResolvedValue();

      await request(app)
        .get('/api/getKata/jawa')
        .expect(500);
    });
  });

  

  describe('POST /api/scanRFID', () => {
    it('fetches a letter and publishes Scan event', async () => {
      db.fetchRFIDLetter.mockResolvedValue('A');
      db.insertLog = jest.fn().mockResolvedValue();
      publish.mockResolvedValue();

      const res = await request(app)
        .post('/api/scanRFID')
        .send({ espId: 'esp1', uid: '1234' })
        .expect(200);

      expect(res.body.konten).toEqual({ nama: 'esp1', uid: '1234', letter: 'A' });
      expect(publish).toHaveBeenCalledWith('unity', expect.any(Object));
    });

    it('for esp32_0 returns letter === uid and skips DB & MQTT', async () => {
      // Spy on the DB and MQTT so we can assert they are *not* called
      const fetchSpy = jest.spyOn(db, 'fetchRFIDLetter');
      const logSpy   = jest.spyOn(db, 'insertLog');
      const pubSpy   = require('./mqtt').publish;

      const payload = { espId: 'esp32_0', uid: 'A' };
      const res = await request(app)
        .post('/api/scanRFID')
        .send(payload)
        .expect(200);

      // Check the response body
      expect(res.body).toEqual({
        tbl:    'RFID',
        events: 'Scan',
        konten: { nama: 'esp32_0', uid: 'A', letter: 'A' }
      });

      // Ensure we did *not* hit the DB or MQTT in this special case
      expect(fetchSpy).not.toHaveBeenCalled();
      expect(logSpy).not.toHaveBeenCalled();
      expect(pubSpy).not.toHaveBeenCalled();
    });

    it('returns 400 if missing params', async () => {
      await request(app)
        .post('/api/scanRFID')
        .send({ espId: 'esp1' })
        .expect(400);
    });
  });

  describe('POST /api/inputRFID', () => {
    it('upserts UID and publishes Input event', async () => {
      db.upsertUID.mockResolvedValue('B');
      db.insertLog = jest.fn().mockResolvedValue();
      publish.mockResolvedValue();

      const res = await request(app)
        .post('/api/inputRFID')
        .send({ espId: 'esp2', uid: '5678' })
        .expect(200);

      expect(res.body.konten).toEqual({ nama: 'esp2', uid: '5678', letter: 'B' });
    });

    it('returns 400 if missing params', async () => {
      await request(app)
        .post('/api/inputRFID')
        .send({ uid: '5678' })
        .expect(400);
    });
  });

  describe('GET /api/getLog/:event', () => {
    it('returns logs for one event', async () => {
      const fakeLogs = [{ id: 1, message: 'foo' }];
      db.getLog.mockResolvedValue(fakeLogs);

      const res = await request(app)
        .get('/api/getLog/Scan')
        .expect(200);

      expect(res.body.konten).toStrictEqual(fakeLogs);
    });
  });

  describe('GET /api/getLogs', () => {
    it('returns all logs', async () => {
      const allLogs = [{ id: 1 }, { id: 2 }];
      db.getLogs.mockResolvedValue(allLogs);

      const res = await request(app)
        .get('/api/getLogs')
        .expect(200);

      expect(res.body.konten).toStrictEqual(allLogs);
    });
  });

  describe('POST /api/feedbackPost', () => {
    it('adds feedback and publishes it', async () => {
      db.upsertFeedback.mockResolvedValue();
      db.insertLog = jest.fn().mockResolvedValue();
      publish.mockResolvedValue();

      const res = await request(app)
        .post('/api/feedbackPost')
        .send({ nama: 'device1', feedback: 'OK' })
        .expect(201);

      expect(res.body.konten).toEqual({ nama: 'device1', feedback: 'OK' });
      expect(publish).toHaveBeenCalledWith('unity', expect.any(Object));
    });
  });

  describe('GET /api/feedbackGet/:nama', () => {
    it('fetches feedback for a device', async () => {
      db.getFeedback.mockResolvedValue('OK');

      const res = await request(app)
        .get('/api/feedbackGet/device1')
        .expect(200);

      expect(res.body.konten).toEqual({ nama: 'device1', feedback: 'OK' });
    });
  });
});
