from fastapi import FastAPI
import uvicorn
import sqlite3
from datetime import datetime
from DatabaseInit import initialize_database  # Import database setup function

app = FastAPI()

# 📌 Run database setup before server starts
initialize_database()

# Function to connect to database
def get_db_connection():
    conn = sqlite3.connect("game_database.db")
    conn.row_factory = sqlite3.Row  # Allows dictionary-like access
    return conn

# 📌 Route: Get all players' scores
@app.get("/scores")
def get_scores():
    conn = get_db_connection()
    cursor = conn.cursor()
    cursor.execute("SELECT * FROM Scores ORDER BY Scores DESC")
    scores = cursor.fetchall()
    conn.close()
    return {"scores": [dict(row) for row in scores]}

# 📌 Route: Add new score
@app.post("/scores/add")
def add_score(name: str, score: int):
    conn = get_db_connection()
    cursor = conn.cursor()
    
    date_now = datetime.now().strftime("%Y-%m-%d %H:%M:%S")  # Get current timestamp
    cursor.execute("INSERT INTO Scores (Name, Scores, Date) VALUES (?, ?, ?)", (name, score, date_now))
    
    conn.commit()
    conn.close()
    return {"message": "Score added successfully!", "name": name, "score": score, "date": date_now}

# 📌 Route: Get RFID card by UID
@app.get("/rfid/{uid}")
def get_rfid(uid: str):
    conn = get_db_connection()
    cursor = conn.cursor()
    cursor.execute("SELECT * FROM RFID_Card WHERE UID = ?", (uid,))
    card = cursor.fetchone()
    
    if card:
        conn.close()
        return dict(card)
    else:
        # If no RFID card is found, read the last character from the database
        cursor.execute("SELECT Huruf FROM RFID_Card ORDER BY ROWID DESC LIMIT 1")
        last_huruf_row = cursor.fetchone()
        
        if last_huruf_row:
            last_huruf = last_huruf_row["Huruf"]
            if last_huruf == "Z":
                next_huruf = "A"  # Wrap around to 'A' after 'Z'
            else:
                next_huruf = chr(ord(last_huruf) + 1)  # Increment normally
        else:
            next_huruf = "A"  # Start with 'A' if the table is empty
        
        # Close the current connection before calling add_rfid
        conn.close()
        
        # Call the add_rfid function to add the new RFID card
        return add_rfid(uid, next_huruf)

# 📌 Route: Add new RFID card (Only if UID does not exist)
@app.post("/rfid/add")
def add_rfid(uid: str, huruf: str):
    conn = get_db_connection()
    cursor = conn.cursor()

    # Check if the UID already exists
    cursor.execute("SELECT * FROM RFID_Card WHERE UID = ?", (uid,))
    existing_card = cursor.fetchone()

    if existing_card:
        conn.close()
        return {"message": "UID already exists!", "UID": uid, "Huruf": existing_card["Huruf"]}

    # Insert new UID if not found
    cursor.execute("INSERT INTO RFID_Card (UID, Huruf) VALUES (?, ?)", (uid, huruf))
    conn.commit()
    conn.close()
    return {"message": "RFID card added successfully!", "UID": uid, "Huruf": huruf}

# 📌 Route: Get all words (Kata)
@app.get("/words")
def get_words():
    conn = get_db_connection()
    cursor = conn.cursor()
    cursor.execute("SELECT * FROM Kata")
    words = cursor.fetchall()
    conn.close()
    return {"words": [dict(row) for row in words]}

# 📌 Start the FastAPI Server
if __name__ == "__main__":
    uvicorn.run(app, host="0.0.0.0", port=8000)
