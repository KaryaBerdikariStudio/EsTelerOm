const fs = require("fs");
const path = require("path");
const sqlite3 = require("sqlite3").verbose();

// Define the path where you want your database file to be created
const dbPath = path.join(__dirname, "game_database.db");

// Check if the file exists; if not, create an empty file (optional, since sqlite3 creates it automatically)
if (!fs.existsSync(dbPath)) {
  fs.writeFileSync(dbPath, "");
  console.log("Database file created at:", dbPath);
}

// Connect to the SQLite database (this will create the file if it doesn't exist)
const db = new sqlite3.Database(dbPath, (err) => {
  if (err) console.error("❌ Database connection error:", err.message); 
  else console.log("✅ Connected to SQLite database at:", dbPath);
});

// Function to initialize the database and create tables
function initializeDatabase() {
  db.serialize(() => {
    // Scores Table
    db.run(`CREATE TABLE IF NOT EXISTS Scores (
      id INTEGER PRIMARY KEY AUTOINCREMENT,
      name TEXT NOT NULL,
      score INTEGER DEFAULT 0,
      date TIMESTAMP DEFAULT CURRENT_TIMESTAMP
    )`);

    // RFID_Card
    db.run(`CREATE TABLE IF NOT EXISTS RFID_Card (
      uid TEXT PRIMARY KEY,
      huruf TEXT NOT NULL
    )`);

    // Clientele
    db.run(`CREATE TABLE IF NOT EXISTS Clientele(
      ip TEXT NOT NULL,
      type TEXT NOT NULL,
      nama TEXT NOT NULL PRIMARY KEY,
      status TEXT NOT NULL  
    )`);

    // RegisteredPlayer
    db.run(`CREATE TABLE IF NOT EXISTS RegisteredPlayer(
      namaDevice TEXT PRIMARY KEY,
      namaPlayer TEXT NOT NULL,
      RGB TEXT NOT NULL
    )`);

    // Input_Logs
    db.run(`CREATE TABLE IF NOT EXISTS Input_Logs(
      type TEXT NOT NULL,
      nama TEXT NOT NULL,
      huruf TEXT NOT NULL,
      benarSalah TEXT NOT NULL,
      date TIMESTAMP DEFAULT CURRENT_TIMESTAMP   
    )`);

    console.log("✅ Database and tables initialized successfully!");
  });
}


// Run initialization when this file is executed directly
if (require.main === module) {
  initializeDatabase();
}

// Export the db connection and initialization function for use in other files
module.exports = { db, initializeDatabase };
