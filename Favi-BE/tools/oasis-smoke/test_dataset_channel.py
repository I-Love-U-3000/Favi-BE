import asyncio
import unittest
from dataset_channel import DatasetChannel


class DatasetChannelTests(unittest.IsolatedAsyncioTestCase):
    async def test_reply_before_wait_is_not_lost(self):
        channel = DatasetChannel()
        key = await channel.write_to_receive_queue((0, {}, "like_post"))
        self.assertEqual((await channel.receive_from())[0], key)
        await channel.send_to((key, {"success": True}))
        self.assertEqual(await channel.read_from_send_queue(key), (key, {"success": True}))
        self.assertEqual(channel.pending, {})

    async def test_out_of_order_replies_reach_correct_waiters(self):
        channel = DatasetChannel()
        keys = [await channel.write_to_receive_queue((i, {}, "follow")) for i in range(20)]
        waiters = [asyncio.create_task(channel.read_from_send_queue(key)) for key in keys]
        for index in reversed(range(20)):
            await channel.send_to((keys[index], index))
        self.assertEqual([m[1] for m in await asyncio.gather(*waiters)], list(range(20)))
        self.assertFalse(channel.pending)

    async def test_cancelled_request_is_cleaned_up(self):
        channel = DatasetChannel()
        key = await channel.write_to_receive_queue((0, {}, "follow"))
        task = asyncio.create_task(channel.read_from_send_queue(key))
        await asyncio.sleep(0)
        task.cancel()
        with self.assertRaises(asyncio.CancelledError):
            await task
        self.assertFalse(channel.pending)

    async def test_exit_needs_no_reply_and_duplicate_reply_fails(self):
        channel = DatasetChannel()
        await channel.write_to_receive_queue((None, None, "exit"))
        self.assertFalse(channel.pending)
        key = await channel.write_to_receive_queue((0, {}, "follow"))
        await channel.send_to((key, True))
        with self.assertRaises(RuntimeError):
            await channel.send_to((key, True))


if __name__ == "__main__":
    unittest.main()
