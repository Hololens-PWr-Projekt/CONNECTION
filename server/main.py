from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from typing import Dict

app = FastAPI()

hololens_data = {}


class HololensData(BaseModel):
    device_id: str  # Hololens ID
    sensor_data: Dict


@app.post("/hololens/data")
async def receive_hololens_data(data: HololensData):
    hololens_data[data.device_id] = data.sensor_data
    return {"status": "Data received successfully"}


@app.get("/hololens/data/{device_id}")
async def get_hololens_data(device_id: str):
    if device_id not in hololens_data:
        raise HTTPException(status_code=404, detail="Device data not found")
    return hololens_data[device_id]


@app.get("/")
async def root():
    return {"message": "HoloLens Communication Server is running"}
