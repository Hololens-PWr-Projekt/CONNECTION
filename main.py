from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from typing import Dict, Any

app = FastAPI()

# Słownik przechowujący dane przesyłane przez urządzenia HoloLens
hololens_data = {}

# Słownik przechowujący zarejestrowane typy wiadomości
message_types = {}


# Model danych przesyłanych przez urządzenia HoloLens
class HololensData(BaseModel):
    device_id: str  # ID urządzenia HoloLens
    sensor_data: Dict[str, Any]  # Dane sensoryczne w formacie klucz-wartość


# Model reprezentujący typ wiadomości
class MessageType(BaseModel):
    name: str  # Nazwa typu wiadomości
    structure: Dict[str, str]  # Struktura wiadomości (klucz i typ danych)


# Endpoint do odbierania danych od urządzeń HoloLens
@app.post("/hololens/data")
async def receive_hololens_data(data: HololensData):
    # Dodanie danych do odpowiedniego urządzenia
    if data.device_id not in hololens_data:
        hololens_data[data.device_id] = []
    hololens_data[data.device_id].append(data.sensor_data)
    return {"status": "Data received successfully"}


# Endpoint do pobierania danych zarejestrowanych dla danego urządzenia
@app.get("/hololens/data/{device_id}")
async def get_hololens_data(device_id: str):
    if device_id not in hololens_data:
        raise HTTPException(status_code=404, detail="Device data not found")
    return hololens_data[device_id]


# Endpoint do rejestrowania nowych typów wiadomości
@app.post("/message-types")
async def add_message_type(message_type: MessageType):
    if message_type.name in message_types:
        raise HTTPException(status_code=400, detail="Message type already exists")
    message_types[message_type.name] = message_type.structure
    return {"status": f"Message type '{message_type.name}' added successfully"}


# Endpoint do pobierania wszystkich zarejestrowanych typów wiadomości
@app.get("/message-types")
async def get_message_types():
    return message_types


# Endpoint główny - sprawdzenie, czy serwer działa
@app.get("/")
async def root():
    return {"message": "HoloLens Communication Server is running"}
