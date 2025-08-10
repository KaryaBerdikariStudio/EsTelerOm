// services/mqtt.js
const mqtt = require('mqtt');

const BROKER_URL = 'mqtt://localhost:1883';
const client = mqtt.connect(BROKER_URL);

client.on('connect', () => {
  console.log(`✅ MQTT connected to ${BROKER_URL}`);
});

client.on('error', err => {
  console.error('❌ MQTT connection error:', err);
});

/**
 * Publikasi MQTT dengan struktur standar
 * @param {string} topic - contoh: "unity", "esp32_{name}"
 * @param {object} payload - harus dalam format: { tabel, event, konten }
 */
function publish(topic, payload) {
  if (
    typeof payload !== 'object' ||
    !payload.tabel ||
    !payload.events ||
    typeof payload.konten !== 'object'
  ) {
    console.warn('⚠️ MQTT payload tidak valid format standarnya:', payload);
  }

  const message = JSON.stringify(payload);

  client.publish(topic, message, err => {
    if (err) {
      console.error(`❌ MQTT publish error ke ${topic}:`, err);
    } else {
      console.log(`📤 MQTT published ke ${topic}:`, message);
    }
  });
}


module.exports = { publish };
