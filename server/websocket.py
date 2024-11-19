import asyncio
import json
from websockets.asyncio.server import serve

async def echo(websocket):
    async for message in websocket:
        data = json.loads(message)
        print(f"Received JSON message: {data}")
        await websocket.send(json.dumps(data))

async def main():
    async with serve(echo, "localhost", 8765):
        await asyncio.get_running_loop().create_future()

asyncio.run(main())