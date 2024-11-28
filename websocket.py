import asyncio
import json
from websockets.asyncio.server import serve

#Ten plik implementuje prosty serwer WebSocket, kt�ry odbiera i odsy�a wiadomo�ci w formacie JSON.

async def echo(websocket):#Nas�uchuje wiadomo�ci od klient�w WebSocket. Odbiera wiadomo�� JSON, drukuje jej zawarto�� i odsy�a j� z powrotem do klienta.
    async for message in websocket:
        data = json.loads(message)
        print(f"Received JSON message: {data}")
        await websocket.send(json.dumps(data))

async def main():
    async with serve(echo, "localhost", 8765):
        await asyncio.get_running_loop().create_future()

asyncio.run(main())