import asyncio
import json
from websockets.asyncio.server import serve

#Ten plik implementuje prosty serwer WebSocket, który odbiera i odsy³a wiadomoœci w formacie JSON.

async def echo(websocket):#Nas³uchuje wiadomoœci od klientów WebSocket. Odbiera wiadomoœæ JSON, drukuje jej zawartoœæ i odsy³a j¹ z powrotem do klienta.
    async for message in websocket:
        data = json.loads(message)
        print(f"Received JSON message: {data}")
        await websocket.send(json.dumps(data))

async def main():
    async with serve(echo, "localhost", 8765):
        await asyncio.get_running_loop().create_future()

asyncio.run(main())