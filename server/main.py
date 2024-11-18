from fastapi import FastAPI, WebSocket, WebSocketDisconnect
from fastapi.middleware.cors import CORSMiddleware
from packet_manager import PacketManager

app = FastAPI()

# CORS (for now)
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"],
)

packet_manager = PacketManager()


@app.websocket("/ws/hololens")
async def websocket_endpoint(websocket: WebSocket):
    await websocket.accept()
    print("WebSocket connection established!")

    try:
        while True:
            # Receive data from the Holo
            data = await websocket.receive_text()
            packet_manager.process_packet(data)

    except WebSocketDisconnect:
        print(f"Connection closed!")
    except Exception as e:
        print(f"WebSocket error: {e}")
