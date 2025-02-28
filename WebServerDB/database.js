const sqlite3 = require("sqlite3").verbose();

// 📌 Connect to SQLite database
const db = new sqlite3.Database("game_database.db", (err) => {
    if (err) console.error("❌ Database connection error:", err.message);
    else console.log("✅ Connected to SQLite database.");
});

// 📌 Function to initialize the database
function initializeDatabase() {
    // Initialize database tables if they don't exist
    db.serialize(() => {
        // Scores Table: Stores player's name, score, and timestamp.
        db.run(`CREATE TABLE IF NOT EXISTS Scores (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        name TEXT NOT NULL,
        score INTEGER DEFAULT 0,
        date TEXT NOT NULL
        )`);
    
        // RFID_Card Table: Maps RFID UID to a letter.
        db.run(`CREATE TABLE IF NOT EXISTS RFID_Card (
        uid TEXT PRIMARY KEY,
        huruf TEXT NOT NULL
        )`);
    
        // Kamus Table: Contains words and asset readiness flags.
        db.run(`CREATE TABLE IF NOT EXISTS Kamus (
        huruf TEXT NOT NULL,
        bahasa_daerah TEXT NOT NULL,
        terjemahan TEXT NOT NULL,
        isVisualAssetReady INTEGER DEFAULT 0,
        isBahasaDaerahAudioReady INTEGER DEFAULT 0,
        isTerjemahanAudioReady INTEGER DEFAULT 0
        )`);
    
        // InputLog Table: Logs each RFID input with IP address and timestamp.
        db.run(`CREATE TABLE IF NOT EXISTS InputLog (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        ip_address TEXT NOT NULL,
        uid TEXT NOT NULL,
        timestamp DATETIME DEFAULT CURRENT_TIMESTAMP
        )`);
    
        console.log("✅ Database and tables initialized successfully!");
    });
}

// 📌 Run initialization when this file is executed directly
if (require.main === module) {
    initializeDatabase();
}



// Export db and init function for use in other files
module.exports = { db, initializeDatabase };
