import sqlite3

# 📌 Function to create tables if they don't exist
def initialize_database():
    conn = sqlite3.connect("game_database.db")
    cursor = conn.cursor()

    # Create Scores Table
    cursor.execute("""
    CREATE TABLE IF NOT EXISTS Scores (
        ID INTEGER PRIMARY KEY AUTOINCREMENT,
        Name TEXT NOT NULL,
        Scores INTEGER DEFAULT 0,
        Date TEXT NOT NULL
    )
    """)

    # Create RFID_Card Table
    cursor.execute("""
    CREATE TABLE IF NOT EXISTS RFID_Card (
        UID TEXT PRIMARY KEY,
        Huruf TEXT NOT NULL
    )
    """)

    # Create Kata Table (No Primary Key)
    cursor.execute("""
    CREATE TABLE IF NOT EXISTS Kata (
        Huruf TEXT NOT NULL,
        Bahasa_Daerah TEXT NOT NULL,
        Terjemahan TEXT NOT NULL,
        IsVisualAssetReady INTEGER DEFAULT 0, 
        IsBahasaDaerahAudioReady INTEGER DEFAULT 0,
        IsTerjemahanAudioReady INTEGER DEFAULT 0
    )
    """)

    conn.commit()
    conn.close()
    print("✅ Database and tables initialized successfully!")

# Run this script only when executed directly
if __name__ == "__main__":
    initialize_database()
