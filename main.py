from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from typing import Dict, Any

app = FastAPI()

# S�ownik przechowuj�cy dane przesy�ane przez urz�dzenia HoloLens
hololens_data = {}

# S�ownik przechowuj�cy zarejestrowane typy wiadomo�ci
message_types = {}


# Model danych przesy�anych przez urz�dzenia HoloLens
class HololensData(BaseModel):
    device_id: str  # ID urz�dzenia HoloLens
    sensor_data: Dict[str, Any]  # Dane sensoryczne w formacie klucz-warto��


# Model reprezentuj�cy typ wiadomo�ci
class MessageType(BaseModel):
    name: str  # Nazwa typu wiadomo�ci
    structure: Dict[str, str]  # Struktura wiadomo�ci (klucz i typ danych)


# Endpoint do odbierania danych od urz�dze� HoloLens
@app.post("/hololens/data")
async def receive_hololens_data(data: HololensData):
    # Dodanie danych do odpowiedniego urz�dzenia
    if data.device_id not in hololens_data:
        hololens_data[data.device_id] = []
    hololens_data[data.device_id].append(data.sensor_data)
    return {"status": "Data received successfully"}


# Endpoint do pobierania danych zarejestrowanych dla danego urz�dzenia
@app.get("/hololens/data/{device_id}")
async def get_hololens_data(device_id: str):
    if device_id not in hololens_data:
        raise HTTPException(status_code=404, detail="Device data not found")
    return hololens_data[device_id]


# Endpoint do rejestrowania nowych typ�w wiadomo�ci
@app.post("/message-types")
async def add_message_type(message_type: MessageType):
    if message_type.name in message_types:
        raise HTTPException(status_code=400, detail="Message type already exists")
    message_types[message_type.name] = message_type.structure
    return {"status": f"Message type '{message_type.name}' added successfully"}


# Endpoint do pobierania wszystkich zarejestrowanych typ�w wiadomo�ci
@app.get("/message-types")
async def get_message_types():
    return message_types


# Endpoint g��wny - sprawdzenie, czy serwer dzia�a
@app.get("/")
async def root():
    return {"message": "HoloLens Communication Server is running"}
