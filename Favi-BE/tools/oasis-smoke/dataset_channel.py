"""OASIS channel protocol with future-based replies instead of100ms polling."""
import asyncio
import uuid


class DatasetChannel:
    def __init__(self):
        self.receive_queue = asyncio.Queue()
        self.pending = {}

    async def receive_from(self):
        return await self.receive_queue.get()

    async def write_to_receive_queue(self, action_info):
        message_id = str(uuid.uuid4())
        if getattr(action_info[-1], "value", action_info[-1]) != "exit":
            self.pending[message_id] = asyncio.get_running_loop().create_future()
        await self.receive_queue.put((message_id, action_info))
        return message_id

    async def send_to(self, message):
        future = self.pending.get(message[0])
        if future is None or future.done():
            raise RuntimeError("Unknown or duplicate OASIS reply")
        future.set_result(message)

    async def read_from_send_queue(self, message_id):
        try:
            return await asyncio.wait_for(self.pending[message_id], timeout=45)
        finally:
            self.pending.pop(message_id, None)
