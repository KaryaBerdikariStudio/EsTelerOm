import sqlite3

db = sqlite3.connect("game_data.db")
cursor = db.cursor()

# Fetch and print all data from each table
print("\n🔹 Player Table:")
for row in cursor.execute("SELECT * FROM Player"):
    print(row)

print("\n🔹 RFID_Card Table:")
for row in cursor.execute("SELECT * FROM RFID_Card"):
    print(row)

print("\n🔹 Kata Table:")
for row in cursor.execute("SELECT * FROM Kata"):
    print(row)

db.close()
