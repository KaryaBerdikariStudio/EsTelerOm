// api-tester.js
const fetch = require('node-fetch');

const API = 'http://localhost:8000/api';  // adjust port/path if needed

async function run() {
  try {
    // 1) set-kata
    let res = await fetch(`${API}/set-kata`, {
      method: 'POST',
      headers: { 'Content-Type':'application/json' },
      body: JSON.stringify({ kata: 'TES' })
    });
    console.log('set-kata:', res.status, await res.json());

    // 2) add-clientele
    res = await fetch(`${API}/add-clientele`, {
      method:'POST',
      headers:{ 'Content-Type':'application/json' },
      body: JSON.stringify({ ip:'192.168.1.10', type:'esp32', nama:'PlayerA' })
    });
    console.log('add-clientele:', res.status, await res.json());

    // 3) get clientele by type
    res = await fetch(`${API}/clientele/type/esp32`);
    console.log('get clientele:', res.status, await res.json());

    // 4) check-input (simulate RFID)
    res = await fetch(`${API}/check-input`, {
      method:'POST',
      headers:{ 'Content-Type':'application/json' },
      body: JSON.stringify({ ip:'192.168.1.10', uid:'FAKE_UID' })
    });
    console.log('check-input:', res.status, await res.json());

    // 5) add-score
    res = await fetch(`${API}/scores/add`, {
      method:'POST',
      headers:{ 'Content-Type':'application/json' },
      body: JSON.stringify({ name:'Alice', score:42 })
    });
    console.log('scores/add:', res.status, await res.json());

    // 6) get-scores
    res = await fetch(`${API}/scores`);
    console.log('scores:', res.status, await res.json());

    // 7) cek-uid
    res = await fetch(`${API}/cek-uid/FAKE_UID`);
    console.log('cek-uid:', res.status, await res.json());


    // 9) delete one clientele
    res = await fetch(`${API}/clientele/192.168.1.10`, { method:'DELETE' });
    console.log('delete-clientele:', res.status, await res.json());

    // 10) reset all clientele
    res = await fetch(`${API}/clientele`, { method:'DELETE' });
    console.log('clear-clientele:', res.status, await res.json());

  } catch (e) {
    console.error('Test script error:', e);
  }
}

run();
