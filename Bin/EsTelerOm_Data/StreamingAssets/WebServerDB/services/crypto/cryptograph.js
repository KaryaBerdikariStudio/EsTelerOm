// cryptograph.js
const fs     = require('fs');
const crypto = require('crypto');



// Load PEM files from the same folder:
const espPubPem    = fs.readFileSync(__dirname + '/esp32_public_key.pem',    'utf8');
const nodePrivPem  = fs.readFileSync(__dirname + '/nodejs_private_key.pem', 'utf8');
const nodePubPem   = fs.readFileSync(__dirname + '/nodejs_public_key.pem',  'utf8');


function encryptIntoEsp32(text) {
  const buffer = Buffer.from(text, 'utf8');
  return crypto.publicEncrypt(
    {
      key:     espPubPem,
      padding: crypto.constants.RSA_PKCS1_PADDING
    },
    buffer
  ).toString('base64');
}

// In your cryptograph.js
function encryptIntoDB(text) {
  const buffer = Buffer.from(text, 'utf8');
  return crypto.publicEncrypt(
    {
      key:     nodePubPem,
      padding: crypto.constants.RSA_PKCS1_PADDING
    },
    buffer
  ).toString('base64');
}

function decrypt(base64) {
  const buffer = Buffer.from(base64, 'base64');
  return crypto.privateDecrypt(
    {
      key:     nodePrivPem,
      padding: crypto.constants.RSA_PKCS1_PADDING
    },
    buffer
  ).toString('utf8');
}


module.exports = {
  /** Encrypt UTF‑8 text with the ESP32’s public key, using PKCS#1 v1.5 padding. */
  encryptIntoEsp32,
  /** Encrypt UTF‑8 text for database using Node’s own public key (optional) */
  encryptIntoDB,

  /** Decrypt base64‑encoded ciphertext using Node’s private key */
  decrypt
};
